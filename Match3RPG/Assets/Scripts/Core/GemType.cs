namespace Match3RPG.Core
{
    public enum GemType
    {
        Attack = 0,
        Mana = 1,
        ExpDrain = 2,
        Rage = 3,
        Health = 4,
        Shield = 5,
        Soul = 6,
        Rainbow = 7,
        Lightning = 8,
        Bagua = 9
    }

    public static class GemTypeMigration
    {
        public static bool TryMigrateLegacy(int legacyValue, out GemType gemType)
        {
            switch (legacyValue)
            {
                case 0: gemType = GemType.Attack; return true;
                case 1: gemType = GemType.Mana; return true;
                case 2: gemType = GemType.ExpDrain; return true;
                case 3: gemType = GemType.Rage; return true;
                case 4: gemType = GemType.Health; return true;
                case 5: gemType = GemType.Rage; return true;
                case 6: gemType = GemType.Soul; return true;
                case 7: gemType = GemType.Rainbow; return true;
                default:
                    gemType = default;
                    return false;
            }
        }
    }
}
