using UnityEngine;

namespace CrosswordGo
{
    [CreateAssetMenu(menuName = "CrosswordGo/Level Library", fileName = "LevelLibrary")]
    public class LevelLibrary : ScriptableObject
    {
        public LevelData[] levels;

        // Wraps around: index 5 with 5 levels returns levels[0]
        public LevelData GetLevel(int index)
        {
            if (levels == null || levels.Length == 0) return null;
            return levels[index % levels.Length];
        }

        // 1-based display number that never resets (level 6 of 5 shows "6", not "1")
        public int DisplayNumber(int index) => index + 1;
    }
}
