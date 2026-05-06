using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CrosswordGo
{
    public class WordDatabaseTests
    {
        private static WordDatabase MakeDb(params (string word, string clueText)[] entries)
        {
            var db = ScriptableObject.CreateInstance<WordDatabase>();
            foreach (var (word, clueText) in entries)
                db.entries.Add(new WordEntry
                {
                    word = word.ToUpperInvariant(),
                    clues = new List<WordClue>
                    {
                        new WordClue { text = clueText, clueType = WordClueType.Text }
                    }
                });
            return db;
        }

        [Test]
        public void get_words_of_length_returns_only_matching_words()
        {
            var db = MakeDb(("CASTLE", "Medieval fort"), ("ABSOLUTE", "Total"), ("WONDER", "Marvel"));
            var words = db.GetWordsOfLength(6);
            CollectionAssert.Contains(words, "CASTLE");
            CollectionAssert.Contains(words, "WONDER");
            CollectionAssert.DoesNotContain(words, "ABSOLUTE");
        }

        [Test]
        public void get_words_of_length_returns_uppercase()
        {
            var db = MakeDb(("castle", "Medieval fort"));
            var words = db.GetWordsOfLength(6);
            Assert.AreEqual("CASTLE", words[0]);
        }

        [Test]
        public void get_words_of_length_deduplicates()
        {
            var db = MakeDb(("CASTLE", "Clue A"), ("CASTLE", "Clue B"));
            var words = db.GetWordsOfLength(6);
            Assert.AreEqual(1, words.Length);
        }

        [Test]
        public void get_entry_finds_by_word()
        {
            var db = MakeDb(("CASTLE", "Medieval fort"));
            var entry = db.GetEntry("castle");
            Assert.IsNotNull(entry);
            Assert.AreEqual("CASTLE", entry.word);
            Assert.AreEqual("Medieval fort", entry.clues[0].text);
        }

        [Test]
        public void get_entry_returns_null_for_missing_word()
        {
            var db = MakeDb(("CASTLE", "Medieval fort"));
            Assert.IsNull(db.GetEntry("DRAGON"));
        }

        [Test]
        public void generator_uses_word_database_words()
        {
            var db = MakeDb(
                ("CASTLE", "c1"), ("BRIGHT", "c2"), ("SIMPLE", "c3"), ("WONDER", "c4"),
                ("FINGER", "c5"), ("TURTLE", "c6"), ("GROUND", "c7"), ("PLANET", "c8"),
                ("FROZEN", "c9"), ("FLOWER", "c10"), ("FOREST", "c11"), ("FAMILY", "c12")
            );
            var slots = LevelGenerator.Generate(Difficulty.Easy, new System.Random(1), db);
            var dbWords = new System.Collections.Generic.HashSet<string>(db.GetWordsOfLength(6));
            foreach (var slot in slots)
                Assert.IsTrue(dbWords.Contains(slot.answer), $"{slot.answer} not from database");
        }
    }
}
