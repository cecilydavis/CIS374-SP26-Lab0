namespace Lab0;
// [ "ryan", "beau", "caleb", "rye", 
// "beautiful", "cale", "cephas", "rhino", "cervid", "cecily"
// "ethan" , "ethel"]

/// <summary>
/// WordSet implementation using HashSet. 
/// Exact lookups are fast, but ordered/prefix operations scan and sort.
/// </summary>
public sealed class HashWordSet : IWordSet
{
    private HashSet<string> words = new();

    public int Count => words.Count;

    public bool Add(string word)
    {
        var normalizedWord = Normalize(word);
        if (normalizedWord.Length == 0)
            return false;

        return words.Add(word);
    }

    public bool Contains(string word)
    {
        var normalizedWord = Normalize(word);
        if (normalizedWord.Length == 0)
            return false;

        return words.Contains(word);
    }

    public bool Remove(string word)
    {
        var normalizedWord = Normalize(word);
        if (normalizedWord.Length == 0)
            return false;

        return words.Remove(word);
    }

    /// TODO
    public string? Prev(string word)
    {
        var normalizedWord = Normalize(word);
        if (normalizedWord.Length == 0)
            return null;

        string? best = null;

    // look for a better best
    foreach (var w in words)
    {
        // best < w && w < word
        if (w.CompareTo(word) < 0
            && (best is null || best.CompareTo(w) < 0))
        {
            best = w;
        }
    }

    return best;
    }

    public string? Next(string word)
    {
        var normalizedWord = Normalize(word);
        if (normalizedWord.Length == 0)
            return null;

        string? best = null;

        // look for a better best
        foreach (var w in words)
        {
            // word < w && w < best
            if (word.CompareTo(w) < 0
                && (best is null || w.CompareTo(best) < 0))
            {
                best = w;
            }
        }

        return best;
    }

    public IEnumerable<string> Prefix(string prefix, int k)
    {
        var normalizedPrefix = Normalize(prefix);

        var results = new List<string>();

        foreach (var word in words)
        {
            if (word.StartsWith(normalizedPrefix))
            {
                results.Add(word);
            }
        }

        results.Sort();

        return results.Slice(0, Math.Min(k, results.Count));
    }

    /// TODO
    public IEnumerable<string> Range(string lo, string hi, int k)
    {
        var normalizedLo = Normalize(lo);
        var normalizedHi = Normalize(hi);

        var results = new List<string>();

    foreach (var word in words)
    {
        if (normalizedLo.CompareTo(word) <= 0
            && word.CompareTo(normalizedHi) <= 0)
        {
            results.Add(word);
        }
    }

    results.Sort();

    return results.Slice(0, Math.Min(k, results.Count));
    }
    /// <summary>
    /// Normalize a word by trimming whitespace and converting to lowercase.
    /// </summary>
    /// <param name="word">The word to normalize.</param>
    /// <returns>The normalized word.</returns>
    private string Normalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return string.Empty;

        // Trim and lowercase 
        return word.Trim().ToLowerInvariant();
    }
}
