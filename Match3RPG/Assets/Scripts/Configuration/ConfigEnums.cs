namespace Match3RPG.Configuration
{
    public enum ItemType
    {
        Consumable = 0,
        Equipment = 1,
        Material = 2,
        Quest = 3
    }

    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    public enum EquipmentSlot
    {
        None = 0,
        Weapon = 1,
        Head = 2,
        Body = 3,
        Accessory = 4,
        Gloves = 5,
        Feet = 6
    }

    public enum EquipmentGrade
    {
        PhapKhi = 0,
        BaoKhi = 1,
        LinhKhi = 2,
        DaoKhi = 3,
        TienKhi = 4,
        ThanKhi = 5,
        ThanhKhi = 6,
        DeBinh = 7
    }

    public enum SkillType
    {
        Active = 0,
        Passive = 1,
        Triggered = 2
    }

    public enum SkillTargetType
    {
        Self = 0,
        SingleAlly = 1,
        AllAllies = 2,
        SingleEnemy = 3,
        AllEnemies = 4,
        SelfAndBoard = 5
    }

    public enum SkillTriggerType
    {
        Manual = 0,
        BattleStart = 1,
        TurnStart = 2,
        OnAttack = 3,
        OnDamageTaken = 4,
        OnMatch = 5
    }
}
