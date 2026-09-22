using System;
using System.IO;
using UnityEngine;

namespace LittleEmber.Core
{
    /// <summary>Serializable save slot payload (JSON on disk).</summary>
    [Serializable]
    public class SaveData
    {
        public string heroName = "Pip";
        public int cloakColor;                    // 0..4 (HeroColorSwap index)
        public int maxHearts = 3;
        public int hearts = 3;
        public int embers;                        // XP / currency
        public int level = 1;
        public string sceneName = "Emberholt_Village";
        public float posX, posY;
        public int playSeconds;
        public string savedAt = "";
    }

    /// <summary>
    /// 3-slot JSON save system in persistentDataPath. Corrupt/unreadable files
    /// are treated as "no save" (try/catch integrity guard) instead of crashing.
    /// </summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        public static SaveData Current { get; private set; }
        public static int CurrentSlot { get; private set; } = -1;
        public static bool HasActiveGame => Current != null;

        static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

        public static bool HasSave(int slot) => Peek(slot) != null;

        /// <summary>Read a slot without making it active. Null when missing/corrupt.</summary>
        public static SaveData Peek(int slot)
        {
            try
            {
                string p = PathFor(slot);
                if (!File.Exists(p)) return null;
                var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(p));
                if (d == null || string.IsNullOrEmpty(d.heroName)) return null;
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Slot {slot} unreadable: {e.Message}");
                return null;
            }
        }

        /// <summary>First free slot, otherwise the oldest save (overwrite).</summary>
        public static int PickSlotForNewGame()
        {
            int oldest = 0;
            DateTime oldestT = DateTime.MaxValue;
            for (int i = 0; i < SlotCount; i++)
            {
                var d = Peek(i);
                if (d == null) return i;
                if (!DateTime.TryParse(d.savedAt, out DateTime t)) return i; // corrupt -> reuse
                if (t < oldestT) { oldestT = t; oldest = i; }
            }
            return oldest;
        }

        public static int LatestSlot()
        {
            int best = -1;
            DateTime bestT = DateTime.MinValue;
            for (int i = 0; i < SlotCount; i++)
            {
                var d = Peek(i);
                if (d == null) continue;
                if (!DateTime.TryParse(d.savedAt, out DateTime t)) { if (best < 0) best = i; continue; }
                if (t > bestT) { bestT = t; best = i; }
            }
            return best;
        }

        public static void NewGame(int slot, string heroName)
        {
            CurrentSlot = slot;
            Current = new SaveData
            {
                heroName = string.IsNullOrWhiteSpace(heroName) ? "Pip" : heroName.Trim()
            };
            Save();
        }

        public static bool Load(int slot)
        {
            var d = Peek(slot);
            if (d == null) return false;
            Current = d;
            CurrentSlot = slot;
            return true;
        }

        public static void Save()
        {
            if (Current == null || CurrentSlot < 0) return;
            Current.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            try
            {
                File.WriteAllText(PathFor(CurrentSlot), JsonUtility.ToJson(Current, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Write failed: {e.Message}");
            }
        }

        public static void Delete(int slot)
        {
            try { if (File.Exists(PathFor(slot))) File.Delete(PathFor(slot)); }
            catch (Exception e) { Debug.LogWarning($"[Save] Delete failed: {e.Message}"); }
        }

        public static void DeleteAll()
        {
            for (int i = 0; i < SlotCount; i++) Delete(i);
            Current = null;
            CurrentSlot = -1;
        }
    }
}
