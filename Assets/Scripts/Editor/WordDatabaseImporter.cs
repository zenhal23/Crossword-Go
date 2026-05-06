using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrosswordGo
{
    // JSON format expected:
    // { "entries": [ { "word": "CASTLE", "clues": [ { "text": "Medieval fortress", "clueType": 0 } ] }, ... ] }
    public static class WordDatabaseImporter
    {
        [MenuItem("Tools/Crossword GO/Import Word Database from JSON")]
        public static void ImportFromJson()
        {
            string jsonPath = EditorUtility.OpenFilePanel("Select Word Database JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(jsonPath)) return;

            string dbPath = EditorUtility.SaveFilePanelInProject(
                "Save Word Database", "WordDatabase", "asset", "", "Assets/Data");
            if (string.IsNullOrEmpty(dbPath)) return;

            string json = File.ReadAllText(jsonPath);
            var raw = JsonUtility.FromJson<WordDatabaseJson>(json);
            if (raw?.entries == null)
            {
                EditorUtility.DisplayDialog("Import Failed", "Could not parse JSON. Check format.", "OK");
                return;
            }

            string dir = Path.GetDirectoryName(dbPath);
            if (!AssetDatabase.IsValidFolder(dir))
                Directory.CreateDirectory(dir);

            var db = AssetDatabase.LoadAssetAtPath<WordDatabase>(dbPath)
                     ?? ScriptableObject.CreateInstance<WordDatabase>();

            db.entries.Clear();
            foreach (var raw_entry in raw.entries)
            {
                var entry = new WordEntry { word = raw_entry.word.ToUpperInvariant() };
                foreach (var rc in raw_entry.clues)
                    entry.clues.Add(new WordClue { text = rc.text, clueType = (WordClueType)rc.clueType });
                db.entries.Add(entry);
            }

            if (!AssetDatabase.Contains(db))
                AssetDatabase.CreateAsset(db, dbPath);

            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Import Complete",
                $"Imported {db.entries.Count} words into {dbPath}", "OK");
            Selection.activeObject = db;
        }

        // ── JSON-serializable mirror types ──────────────────────────────────

        [Serializable]
        private class WordDatabaseJson
        {
            public List<WordEntryJson> entries;
        }

        [Serializable]
        private class WordEntryJson
        {
            public string word;
            public List<WordClueJson> clues;
        }

        [Serializable]
        private class WordClueJson
        {
            public string text;
            public int clueType; // maps to WordClueType enum value
        }
    }
}
