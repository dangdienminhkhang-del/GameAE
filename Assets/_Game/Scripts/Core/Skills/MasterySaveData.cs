using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Save key và DTO cho mastery. Key riêng để không sửa SaveKeys ở Core/World (không owned).
    /// Mastery range 0-100 theo docs/07 §3.
    /// </summary>
    public static class MasterySaveKeys
    {
        public const string Mastery = "mastery";
    }

    [Serializable]
    public class MasteryEntry
    {
        public string disciplineId;
        public int value;

        public MasteryEntry() { }

        public MasteryEntry(WeaponDiscipline d, int v)
        {
            disciplineId = d.ToString();
            value = v;
        }

        public WeaponDiscipline Discipline
        {
            get
            {
                if (Enum.TryParse(disciplineId, out WeaponDiscipline result)) return result;
                return WeaponDiscipline.None;
            }
        }
    }

    [Serializable]
    public class MasterySaveData
    {
        public List<MasteryEntry> entries = new List<MasteryEntry>();

        public int GetValue(WeaponDiscipline discipline)
        {
            if (discipline == WeaponDiscipline.None) return 0;
            string id = discipline.ToString();
            for (int i = 0; i < entries.Count; i++)
            {
                MasteryEntry e = entries[i];
                if (e != null && e.disciplineId == id) return Mathf.Clamp(e.value, 0, 100);
            }
            return 0;
        }

        public void SetValue(WeaponDiscipline discipline, int value)
        {
            if (discipline == WeaponDiscipline.None) return;
            int clamped = Mathf.Clamp(value, 0, 100);
            string id = discipline.ToString();
            for (int i = 0; i < entries.Count; i++)
            {
                MasteryEntry e = entries[i];
                if (e != null && e.disciplineId == id) { e.value = clamped; return; }
            }
            entries.Add(new MasteryEntry(discipline, clamped));
        }

        public Dictionary<WeaponDiscipline, int> ToDictionary()
        {
            var result = new Dictionary<WeaponDiscipline, int>();
            for (int i = 0; i < entries.Count; i++)
            {
                MasteryEntry e = entries[i];
                if (e == null) continue;
                WeaponDiscipline d = e.Discipline;
                if (d == WeaponDiscipline.None) continue;
                result[d] = Mathf.Clamp(e.value, 0, 100);
            }
            return result;
        }

        public static MasterySaveData FromDictionary(Dictionary<WeaponDiscipline, int> dict)
        {
            var data = new MasterySaveData();
            if (dict == null) return data;
            foreach (var pair in dict)
            {
                if (pair.Key == WeaponDiscipline.None) continue;
                data.entries.Add(new MasteryEntry(pair.Key, Mathf.Clamp(pair.Value, 0, 100)));
            }
            return data;
        }
    }
}
