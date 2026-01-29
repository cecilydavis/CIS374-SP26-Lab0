/// <summary>
/// Interface for a word set that supports prefix/range queries
/// and lexicographic neighbors (Next/Prev).
/// </summary>
public interface IWordSet
{
    bool Add(string word);
    bool Remove(string word);
    bool Contains(string word);

    /// <summary>Next word strictly greater than <paramref name="word"/>; null if none.</summary>
    string? Next(string word);

    /// <summary>Previous word strictly less than <paramref name="word"/>; null if none.</summary>
    string? Prev(string word);

    /// <summary>Up to k words that start with <paramref name="prefix"/>, sorted, not including the prefix; all words if prefix=""</summary>
    IEnumerable<string> Prefix(string prefix, int k);

    /// <summary>Up to k words in the inclusive lexigraphic range [lo, hi], sorted.</summary>
    IEnumerable<string> Range(string lo, string hi, int k);

    int Count{get;}

}