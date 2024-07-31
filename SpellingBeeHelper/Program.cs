namespace SpellingBeeHelper;

internal class Program
{
    // Possible dictionaries to search
    private const string DictionaryDir = @"C:\Users\brend\source\repos\SpellingBeeHelper\SpellingBeeHelper";
    private static readonly string DictionaryPath = Path.Combine(DictionaryDir, "usa2.txt");    // small
    private static readonly string SeenPath = Path.Combine(DictionaryDir, "seen.txt");    // seen but not in dictionaries
    private static readonly string NotWordsPath = Path.Combine(DictionaryDir, "notwords.txt");    // seen but not in dictionaries
    private static readonly string DictionaryLargePath = Path.Combine(DictionaryDir, "en_US-large.txt"); // medium
    private static readonly string DictionaryHugePath = Path.Combine(DictionaryDir, "words_alpha.txt"); // large
    private static readonly string DictionaryScrabblePath = Path.Combine(DictionaryDir, "scrabble-dictionary.txt"); // large
    private static readonly string DictionaryWebstersPath = Path.Combine(DictionaryDir, "WebstersEnglishDictionary.txt");

    public static void Main(string[] args)
    {
        do
        {
            PlayGame();
        }
        while (Again());
    }

    private static bool Again()
    {
        string response;
        do
        {
            Console.Write("Play Again (Y/N)? ");
            string? inputChars = Console.ReadLine();
            response = inputChars != null ? inputChars.Trim().ToUpper() : string.Empty;
            if (Equals(response, "Y"))
                return true;
        }
        while (!Equals(response, "N"));

        return false;
    }

    private static void PlayGame()
    {
        // Ask for the set of letters allowed
        string? beeChars = null;
        while (beeChars == null)
        {
            Console.WriteLine("Letters (7 char with center first): ");
            string? inputChars = Console.ReadLine();
            if (inputChars == null)
                continue;
            inputChars = inputChars.ToLowerInvariant();
            if (inputChars.Distinct().Count() == 7 && inputChars.All(c => 'a' <= c && c <= 'z'))
                beeChars = inputChars;
        }

        // Find base set of words usually achieving "genius" level
        ShowBaseWords(beeChars, out var anagrams, out var notWords);

        // Find larger set with more misses, adding found words to seen.txt
        var trieLarge = new Trie(DictionaryLargePath);
        var anagramsLarge = trieLarge.FindBeeWords(beeChars);
        var anagramsDiff = anagramsLarge.Except(anagrams).Except(notWords).ToArray();
        ShowChoices(SeenPath, anagramsDiff, NotWordsPath);

        ShowBaseWords(beeChars, out anagrams, out notWords);

        // Search HUGE set of words by start character and length
        // adding found words to seen.txt
        var trieHuge = new Trie(DictionaryHugePath,
            DictionaryLargePath, DictionaryPath, DictionaryScrabblePath, DictionaryWebstersPath);
        char? searchLetter;
        int? searchLength;
        for (; ;)
        {
            Console.WriteLine();
            searchLetter = GetSearchLetter();
            searchLength = GetSearchLength();
            if (!searchLetter.HasValue && !searchLength.HasValue)
                break;

            var anagramsHuge = trieHuge.FindBeeWords(beeChars, searchLetter, searchLength)
                .Except(notWords)
                // .Except(new[] { anagrams, anagramsLarge}.SelectMany(a => a)) - missing words
                .ToArray();
            ShowChoices(SeenPath, anagramsHuge);
        }

        ShowBaseWords(beeChars, out _, out _);

        static char? GetSearchLetter()
        {
            for (; ; )
            {
                Console.Write("Letter: ");
                string? letterText = Console.ReadLine();
                if (string.IsNullOrEmpty(letterText))
                    return null;
                if (letterText.Length == 1)
                {
                    var ch = char.ToLowerInvariant(letterText[0]);
                    if (char.IsAsciiLetterLower(ch))
                        return ch;
                }
                Console.WriteLine("Error: please enter a letter between a and z.");
            }
        }

        static int? GetSearchLength()
        {
            for (; ; )
            {
                Console.Write("Length: ");
                string? lengthText = Console.ReadLine();
                if (string.IsNullOrEmpty(lengthText))
                    return null;
                int searchLength;
                if (int.TryParse(lengthText, out searchLength))
                    return searchLength;
            }
        }
    }

    private static void ShowBaseWords(string beeChars, out IList<string> anagrams, out IList<string> notWords)
    {
        for (; ; )
        {
            var trie = new Trie(DictionaryPath, SeenPath);
            anagrams = trie.FindBeeWords(beeChars);
            Console.WriteLine();
            notWords = File.ReadLines(NotWordsPath).ToArray();
            if (!ShowChoices(NotWordsPath, anagrams.Except(notWords).ToArray(), null, beeChars))
                break;
        }
    }

    private static bool ShowChoices(string seenPath, string[] anagrams, string? notPath = null, string? letters = null)
    {
        int score = 0;
        int limit = anagrams.Length/2;
        if (anagrams.Length % 2 == 1)
            limit++;
        for (int i = 0; i < limit; i++)
        {
            string anagram = anagrams[i];
            Console.Write(GetNumberText(i) + anagram);
            score += GetScore(anagram, letters);
            if (i + limit < anagrams.Length)
            {
                Console.Write("                           ".Substring(anagram.Length));
                anagram = anagrams[i + limit];
                Console.Write(GetNumberText(i + limit) + anagram);
                score += GetScore(anagram, letters);
            }
            Console.WriteLine();
        }
        if (letters != null)
            Console.WriteLine("Score: " + score);

        for (; ;)
        {
            Console.Write("Seen (comma separated)? ");
            var seenWords = Console.ReadLine();
            if (string.IsNullOrEmpty(seenWords))
            {
                return WriteNotSeenWords(notPath, anagrams, Array.Empty<int>());
            }

            using var seenWriter = new StreamWriter(seenPath, true);
            try
            {
                bool seenWritten = false;

                var seenWordIndexes = seenWords.Split(',').Select(w => int.Parse(w.Trim()) - 1).ToArray();
                foreach (var i in seenWordIndexes)
                {
                    seenWriter.WriteLine(anagrams[i]);
                    seenWritten = true;
                }
                bool notseenWritten = WriteNotSeenWords(notPath, anagrams, seenWordIndexes);
                return seenWritten || notseenWritten;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    private static string GetNumberText(int i)
    {
        int lineNum = i + 1;
        var lineText = lineNum.ToString();
        if (lineText.Length == 1)
            lineText = " " + lineText;
        return lineText + ". ";
    }

    private static int GetScore(string anagram, string? letters)
    {
        if (anagram.Length < 5)
            return 1;
        int score = anagram.Length;
        if (letters != null && letters.All(anagram.Contains))
            score += 7;
        return score;
    }

    private static bool WriteNotSeenWords(string? notPath, string[] anagrams, int[] seenWordIndexes)
    {
        bool wordsWritten = false;
        if (notPath != null)
        {
            using var notWriter = new StreamWriter(notPath, true);
            for (int i = 0; i < anagrams.Length; i++)
            {
                if (!seenWordIndexes.Contains(i))
                {
                    wordsWritten = true;
                    notWriter.WriteLine(anagrams[i]);
                }
            }
        }
        return wordsWritten;
    }
}