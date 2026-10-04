using Game.Core.Items;

namespace Game.Core.Skills
{
    /// <summary>
    /// Map WeaponType (enum ở Items, không modify được vì không owned) sang WeaponDiscipline.
    /// TechniqueController (PvE) đọc equipment WeaponType rồi convert qua đây.
    /// Nếu Discipline không thuộc Mốc 1 → trả về None (Technique bị khóa).
    /// </summary>
    public static class WeaponTypeToDisciplineConverter
    {
        public static WeaponDiscipline Convert(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                case WeaponType.Rapier:
                case WeaponType.TwoHandedSword:
                    return WeaponDiscipline.Blade;

                case WeaponType.Dagger:
                    return WeaponDiscipline.Knives;

                case WeaponType.TwoHandedAxe:
                    return WeaponDiscipline.Cleaver;

                case WeaponType.Mace:
                    return WeaponDiscipline.Maul;

                default:
                    return WeaponDiscipline.None;
            }
        }
    }
}
