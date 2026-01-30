
namespace Lab0;
"ce", ..."ce\uFFFF"
// [ "ryan", "beau", "beautiful", "cale", "caleb", "cecily", "cephas", 
//"cervid", "ethan", "ethel", "rhino", "ryan", "rye"]

public class SortedWordSet : IWordSet
{

    private SortedSet<string> words = new();

    public int Count => words.Count;

    public bool Add(string word)
    {
        var normailzedWord = Normalize(word);
        if(normailzedWord.Length ==0)
            return false;

        words.Add(normailzedWordword));
    }

    public bool Contains(string word)
    {
        var normailzedWord = Normalize(word);
        if(normailzedWord.Length ==0)
            return false;
            
        words.Contains(normailzedWordword));
    }

    public string? Next(string word)
    {
        var normalzedWord =Normalize(word);
        if(normalzedWord.Length ==0 || words.Count==0)
            return null;

        //var wordsInRange = words.GetViewBetween("a", "m");
        //words.GetViewBetween("charity", "\uFFFF\uFFFF\uFFFF")

        foreach(var candidate in words.GetViewBetween(normalzedWord, MAX_STRING))
        {
            if(candidate.CompareTo(normalzedWord) > 0)
            {
                return candidate;
            }
        }
        return null;
    }

    public IEnumerable<string> Prefix(string prefix, int k)
    {
        if(k <=0 || words.Count ==0 )
            return new List<string>();

        var results = new List<string>();

        var normalizedPrefix = Normalize(prefix);

        string lo = "prefix";
        string hi = "prefix"+"{";

        int count = 0;
        lo = "ethan";
        hi = "ethan{";

        foreach( var candidate in words.GetViewBetween(lo, hi) )
        {



            results.Add(candidate);

            count++;
            if(count >= k)
            {
                return results;
            }
        }

        return results;
    }

    public string? Prev(string word)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> Range(string lo, string hi, int k)
    {
        throw new NotImplementedException();
    }

    public bool Remove(string word)
    {
        var normailzedWord = Normalize(word);
        if(normailzedWord.Length ==0)
            return false;
            
        words.Remove(normailzedWordword);
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

   private const string MAX_STRING = "\uFFFF\uFFFF\uFFFF";
}