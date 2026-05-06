using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrosswordGo
{
    [CreateAssetMenu(menuName = "CrosswordGo/Word Database", fileName = "WordDatabase")]
    public class WordDatabase : ScriptableObject
    {
        public List<WordEntry> entries = new List<WordEntry>();

        public string[] GetWordsOfLength(int length) =>
            entries
                .Where(e => !string.IsNullOrEmpty(e.word) && e.word.Length == length)
                .Select(e => e.word.ToUpperInvariant())
                .Distinct()
                .ToArray();

        public WordEntry GetEntry(string word)
        {
            string upper = word.ToUpperInvariant();
            return entries.FirstOrDefault(e =>
                string.Equals(e.word, upper, System.StringComparison.OrdinalIgnoreCase));
        }
    }
}
