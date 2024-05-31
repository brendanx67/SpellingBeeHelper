using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpellingBeeHelper
{
    internal class Trie
    {
        private bool _isWord;
        private Trie[] _edges;

        public Trie()
        {
            _edges = new Trie[26];
        }

        public Trie(IList<string> words)
            : this()
        {
            AddWords(words);
        }

        public Trie(params string[] wordListPaths)
            : this()
        {
            AddWords(wordListPaths);
        }

        private int ToIndex(char c)
        {
            return char.ToLowerInvariant(c) - 'a';
        }

        public void AddWords(params string[] wordListPaths)
        {
            foreach (var wordListPath in wordListPaths)
            {
                var wordList = File.ReadAllLines(wordListPath)
                    .Where(l => l.Length > 0 && l.All(char.IsAsciiLetter))
                    .Select(l => l.ToLower()).Distinct().ToList();
                AddWords(wordList);
            }
        }
        public void AddWords(IList<string> words)
        {
            foreach (string word in words)
            {
                if (word.Length > 0)
                {
                    AddWord(word);
                }
            }
        }

        private void AddWord(string word)
        {
            if (word.Length == 0)
            {
                _isWord = true;
                return;
            }
            int edge = ToIndex(word[0]);
            var nextNode = _edges[edge];
            if (nextNode == null)
            {
                nextNode = _edges[edge] = new Trie();
            }
            string remainder = word.Substring(1);
            nextNode.AddWord(remainder);
        }

        private bool IsWord(string word)
        {
            if (word.Length == 0)
            {
                return _isWord;
            }
            int edge = ToIndex(word[0]);
            var nextNode = _edges[edge];
            if (nextNode == null)
                return false;
            string remainder = word.Substring(1);
            return nextNode.IsWord(remainder);
        }

        public IList<string> FindWords(string chars, string prefix = "")
        {
            var result = new List<string>();
            if (_isWord)
            {
                result.Add(prefix);
            }
            for (int i = 0; i < chars.Length; i++)
            {
                int edge = ToIndex(chars[i]);
                var nextNode = _edges[edge];
                if (nextNode != null)
                    result.AddRange(nextNode.FindWords(chars, prefix + chars[i]));
            }
            return result;
        }

        public IList<string> FindBeeWords(string beeChars, char? letter = null, int? length = null)
        {
            var anagrams = FindWords(beeChars)
                .Where(w => w.Length > 3 && w.Contains(beeChars[0]))
                .Order().ToList();
            if (letter.HasValue)
                anagrams = anagrams.Where(w => w[0] == letter.Value).ToList();
            if (length.HasValue)
                anagrams = anagrams.Where(w => w.Length == length.Value).ToList();
            return anagrams;
        }
    }
}
