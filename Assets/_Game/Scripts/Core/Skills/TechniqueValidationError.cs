namespace Game.Core.Skills
{
    public enum TechniqueValidationError
    {
        None = 0,
        DefinitionMissing = 1,
        LockedMastery = 2,
        WrongDiscipline = 3,
        Cooldown = 4,
        CombatBusy = 5,
        NoWeaponEquipped = 6,
        TargetDead = 7
    }
}
