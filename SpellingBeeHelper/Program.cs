// See https://aka.ms/new-console-template for more information
using SpellingBeeHelper;
using System.Runtime.CompilerServices;

internal class Program
{
    // Possible dictionaries to search
    private static string dictionaryDir = @"C:\Users\brend\source\repos\SpellingBeeHelper\SpellingBeeHelper";
    private static string dictionaryPath = Path.Combine(dictionaryDir, "usa2.txt");    // small
    private static string seenPath = Path.Combine(dictionaryDir, "seen.txt");    // seen but not in dictionaries
    private static string notWordsPath = Path.Combine(dictionaryDir, "notwords.txt");    // seen but not in dictionaries
    private static string dictionaryLargePath = Path.Combine(dictionaryDir, "en_US-large.txt"); // medium
    private static string dictionaryHugePath = Path.Combine(dictionaryDir, "words_alpha.txt"); // large
    private static string dictionaryScrabblePath = Path.Combine(dictionaryDir, "scrabble-dictionary.txt"); // large
    private static string dictionaryWebstersPath = Path.Combine(dictionaryDir, "WebstersEnglishDictionary.txt");

    private static void Main(string[] args)
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
        var trie = new Trie(dictionaryPath, seenPath);
        var anagrams = trie.FindBeeWords(beeChars);
        Console.WriteLine();
        var notWords = File.ReadLines(notWordsPath);
        ShowChoices(notWordsPath, anagrams.Except(notWords).ToArray());

        // Find larger set with more misses, adding found words to seen.txt
        var trieLarge = new Trie(dictionaryLargePath);
        var anagramsLarge = trieLarge.FindBeeWords(beeChars);
        var anagramsDiff = anagramsLarge.Except(anagrams).Except(notWords).ToArray();
        ShowChoices(seenPath, anagramsDiff, notWordsPath);

        // Search HUGE set of words by start character and length
        // adding found words to seen.txt
        var trieHuge = new Trie(dictionaryHugePath,
            dictionaryLargePath, dictionaryPath, dictionaryScrabblePath, dictionaryWebstersPath);
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
            ShowChoices(seenPath, anagramsHuge);
        }

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

    private static void ShowChoices(string seenPath, string[] anagrams, string notPath = null)
    {
        int line = 1;
        foreach (var anagram in anagrams)
        {
            Console.Write(line++ + ". ");
            Console.WriteLine(anagram);
        }
        for (; ;)
        {
            Console.Write("Seen (comma separated)? ");
            var seenWords = Console.ReadLine();
            if (string.IsNullOrEmpty(seenWords))
            {
                WriteNotSeenWords(notPath, anagrams, []);
                return;
            }

            using var seenWriter = new StreamWriter(seenPath, true);
            try
            {
                var seenWordIndexes = seenWords.Split(',').Select(w => int.Parse(w.Trim()) - 1).ToArray();
                foreach (var i in seenWordIndexes)
                {
                    seenWriter.WriteLine(anagrams[i]);
                }
                WriteNotSeenWords(notPath, anagrams, seenWordIndexes);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    private static void WriteNotSeenWords(string notPath, string[] anagrams, int[] seenWordIndexes)
    {
        if (notPath != null)
        {
            using var notWriter = new StreamWriter(notPath, true);
            for (int i = 0; i < anagrams.Length; i++)
            {
                if (!seenWordIndexes.Contains(i))
                    notWriter.WriteLine(anagrams[i]);
            }
        }
    }
}