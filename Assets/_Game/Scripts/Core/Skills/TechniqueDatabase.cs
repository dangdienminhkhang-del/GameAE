using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Skills
{
    /// <summary>
    /// Tập hợp WeaponTechniqueDefinition tĩnh — tìm kiếm theo ID hoặc cặp (Discipline, InputSlot).
    /// Thêm Technique mới ở Mốc 2 chỉ cần thêm asset + gán vào đây. Core/PvE không đổi.
    /// </summary>
    [CreateAssetMenu(fileName = "TechniqueDatabase", menuName = "Game/Core/Skills/Technique Database")]
    public class TechniqueDatabase : ScriptableObject
    {
        [SerializeField] private List<WeaponTechniqueDefinition> techniques = new List<WeaponTechniqueDefinition>();

        private readonly Dictionary<string, WeaponTechniqueDefinition> _byId = new Dictionary<string, WeaponTechniqueDefinition>();
        private bool _indexed;

        public IReadOnlyList<WeaponTechniqueDefinition> All => techniques;

        private void EnsureIndexed()
        {
            if (_indexed) return;
            _byId.Clear();
            if (techniques != null)
            {
                for (int i = 0; i < techniques.Count; i++)
                {
                    WeaponTechniqueDefinition def = techniques[i];
                    if (def == null || string.IsNullOrWhiteSpace(def.Id)) continue;
                    _byId[def.Id] = def;
                }
            }
            _indexed = true;
        }

        public WeaponTechniqueDefinition FindById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            EnsureIndexed();
            return _byId.TryGetValue(id, out WeaponTechniqueDefinition def) ? def : null;
        }

        public WeaponTechniqueDefinition FindBySlot(WeaponDiscipline discipline, TechniqueInputSlot slot)
        {
            if (discipline == WeaponDiscipline.None || slot == TechniqueInputSlot.None) return null;
            EnsureIndexed();

            WeaponTechniqueDefinition best = null;
            int bestMastery = -1;

            if (techniques != null)
            {
                for (int i = 0; i < techniques.Count; i++)
                {
                    WeaponTechniqueDefinition def = techniques[i];
                    if (def == null) continue;
                    if (def.WeaponDiscipline != discipline || def.InputSlot != slot) continue;
                    if (def.MasteryRequired > bestMastery)
                    {
                        bestMastery = def.MasteryRequired;
                        best = def;
                    }
                }
            }
            return best;
        }

        public List<WeaponTechniqueDefinition> ListByDiscipline(WeaponDiscipline discipline)
        {
            var result = new List<WeaponTechniqueDefinition>();
            if (discipline == WeaponDiscipline.None) return result;
            EnsureIndexed();

            if (techniques != null)
            {
                for (int i = 0; i < techniques.Count; i++)
                {
                    WeaponTechniqueDefinition def = techniques[i];
                    if (def != null && def.WeaponDiscipline == discipline) result.Add(def);
                }
            }
            return result;
        }
    }
}
