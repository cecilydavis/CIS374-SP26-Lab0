using System.Diagnostics;

namespace Lab0;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: dotnet run -c Release -- <path-to-words.txt> [iterations] [k]");
            return;
        }

        string path = args[0];
        int iterations = args.Length >= 2 && int.TryParse(args[1], out var it) ? it : 50_000;
        int k = args.Length >= 3 && int.TryParse(args[2], out var kk) ? kk : 10;

        var words = LoadWords(path);
        Console.WriteLine($"Loaded {words.Count:N0} raw lines.");

        // Build both structures with the same normalized words
        IWordSet hash = new HashWordSet();
        IWordSet sorted = new SortedWordSet();

        foreach (var w in words)
        {
            var t = Normalize(w);
            if (t.Length == 0) continue;
            hash.Add(t);
            sorted.Add(t);
        }

        Console.WriteLine($"HashSet count:  {hash.Count:N0}");
        Console.WriteLine($"SortedSet count:{sorted.Count:N0}");
        Console.WriteLine($"Iterations: {iterations:N0}, k={k}");

        // Create deterministic workloads from the word universe
        var universe = sorted.Range("a", "\uFFFF", int.MaxValue).ToList(); // all words (already sorted)
        if (universe.Count == 0)
        {
            Console.WriteLine("No words loaded after normalization.");
            return;
        }

        var containsQueries = BuildContainsWorkload(universe, iterations);
        var prefixQueries = BuildPrefixWorkload(universe, iterations);
        var rangeQueries = BuildRangeWorkload(universe, iterations);
        var neighborQueries = BuildNeighborWorkload(universe, iterations);

        // Warm up JIT + caches
        WarmUp(hash, sorted, containsQueries, prefixQueries, rangeQueries, neighborQueries, k);

        Console.WriteLine();
        Console.WriteLine("=== CONTAINS (membership) ===");
        Bench("HashSet.Contains", () => RunContains(hash, containsQueries));
        Bench("SortedSet.Contains", () => RunContains(sorted, containsQueries));

        Console.WriteLine();
        Console.WriteLine("=== PREFIX (autocomplete) ===");
        Bench("HashSet.Prefix", () => RunPrefix(hash, prefixQueries, k));
        Bench("SortedSet.Prefix", () => RunPrefix(sorted, prefixQueries, k));

        Console.WriteLine();
        Console.WriteLine("=== RANGE (lex range) ===");
        Bench("HashSet.Range", () => RunRange(hash, rangeQueries, k));
        Bench("SortedSet.Range", () => RunRange(sorted, rangeQueries, k));

        Console.WriteLine();
        Console.WriteLine("=== NEXT/PREV (neighbors) ===");
        Bench("HashSet.Next/Prev", () => RunNeighbors(hash, neighborQueries));
        Bench("SortedSet.Next/Prev", () => RunNeighbors(sorted, neighborQueries));

        Console.WriteLine();
        Console.WriteLine("Done.");
    }

    // --- workloads ---

    private static string[] BuildContainsWorkload(List<string> universe, int iterations)
    {
        // Mix of present and absent queries
        var queries = new string[iterations];
        int n = universe.Count;

        for (int i = 0; i < iterations; i++)
        {
            if (i % 2 == 0)
            {
                // present
                queries[i] = universe[(i * 9973) % n];
            }
            else
            {
                // absent: tweak a present word
                var w = universe[(i * 9973) % n];
                queries[i] = w + "x"; // likely absent
            }
        }

        return queries;
    }

    private static string[] BuildPrefixWorkload(List<string> universe, int iterations)
    {
        // Prefix lengths 1..4 across the sorted list
        var prefixes = new string[iterations];
        int n = universe.Count;

        for (int i = 0; i < iterations; i++)
        {
            var w = universe[(i * 7919) % n];
            int len = 1 + (i % 4);
            if (w.Length < len) len = w.Length;
            prefixes[i] = w.Substring(0, len);
        }

        return prefixes;
    }

    private static (string lo, string hi)[] BuildRangeWorkload(List<string> universe, int iterations)
    {
        // Create ranges by selecting two words and using them as bounds (ensures non-empty ranges often)
        var ranges = new (string lo, string hi)[iterations];
        int n = universe.Count;

        for (int i = 0; i < iterations; i++)
        {
            int a = (i * 3571) % n;
            int b = (i * 3571 + 12345) % n;

            var lo = universe[Math.Min(a, b)];
            var hi = universe[Math.Max(a, b)];

            // Tighten to a smaller lexical neighborhood sometimes
            if (i % 3 == 0 && lo.Length >= 2)
            {
                lo = lo.Substring(0, 2);
                hi = hi.Substring(0, 2) + "{";
            }

            ranges[i] = (lo, hi);
        }

        return ranges;
    }

    private static string[] BuildNeighborWorkload(List<string> universe, int iterations)
    {
        // Use a mix of existing words and near-misses
        var queries = new string[iterations];
        int n = universe.Count;

        for (int i = 0; i < iterations; i++)
        {
            var w = universe[(i * 104729) % n];
            queries[i] = (i % 5 == 0) ? w + "a" : w; // some not present, still meaningful for Next/Prev
        }

        return queries;
    }

    // --- runners (return checksum so work can't be optimized away) ---

    private static long RunContains(IWordSet dict, string[] queries)
    {
        long sum = 0;
        foreach (var q in queries)
            sum += dict.Contains(q) ? 1 : 0;
        return sum;
    }

    private static long RunPrefix(IWordSet dict, string[] prefixes, int k)
    {
        long sum = 0;
        foreach (var pre in prefixes)
        {
            foreach (var w in dict.Prefix(pre, k))
                sum += w.Length;
        }
        return sum;
    }

    private static long RunRange(IWordSet dict, (string lo, string hi)[] ranges, int k)
    {
        long sum = 0;
        foreach (var (lo, hi) in ranges)
        {
            foreach (var w in dict.Range(lo, hi, k))
                sum += w.Length;
        }
        return sum;
    }

    private static long RunNeighbors(IWordSet dict, string[] queries)
    {
        long sum = 0;
        foreach (var q in queries)
        {
            var n = dict.Next(q);
            var p = dict.Prev(q);
            if (n != null) sum += n.Length;
            if (p != null) sum += p.Length;
        }
        return sum;
    }

    // --- benchmark helpers ---

    private static void WarmUp(
        IWordSet hash,
        IWordSet sorted,
        string[] containsQueries,
        string[] prefixQueries,
        (string lo, string hi)[] rangeQueries,
        string[] neighborQueries,
        int k)
    {
        int warm = Math.Min(5000, containsQueries.Length);

        _ = RunContains(hash, containsQueries.Take(warm).ToArray());
        _ = RunContains(sorted, containsQueries.Take(warm).ToArray());

        _ = RunPrefix(hash, prefixQueries.Take(warm).ToArray(), k);
        _ = RunPrefix(sorted, prefixQueries.Take(warm).ToArray(), k);

        _ = RunRange(hash, rangeQueries.Take(warm).ToArray(), k);
        _ = RunRange(sorted, rangeQueries.Take(warm).ToArray(), k);

        _ = RunNeighbors(hash, neighborQueries.Take(warm).ToArray());
        _ = RunNeighbors(sorted, neighborQueries.Take(warm).ToArray());
    }

    private static void Bench(string name, Func<long> action)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long beforeAlloc = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();

        long checksum = action();

        sw.Stop();
        long afterAlloc = GC.GetAllocatedBytesForCurrentThread();
        long alloc = afterAlloc - beforeAlloc;

        Console.WriteLine($"{name}");
        Console.WriteLine($"  Time: {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"  Allocated (thread): {alloc:N0} bytes");
        Console.WriteLine($"  Checksum: {checksum:N0}");
    }

    private static List<string> LoadWords(string path)
    {
        var list = new List<string>(capacity: 1_000_000);
        foreach (var line in File.ReadLines(path))
        {
            var w = line.Trim();
            if (w.Length > 0) list.Add(w);
        }
        return list;
    }

    private static string Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToLowerInvariant();
}
