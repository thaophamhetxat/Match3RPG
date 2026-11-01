using System.Collections.Generic;

namespace Match3RPG.Persistence
{
    public sealed class RuntimeLootDrop
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public bool FirstClearOnly { get; }

        public RuntimeLootDrop(string itemId, int quantity, bool firstClearOnly)
        {
            ItemId = itemId;
            Quantity = quantity;
            FirstClearOnly = firstClearOnly;
        }
    }

    public sealed class RuntimeLootResult
    {
        readonly IReadOnlyList<RuntimeLootDrop> drops;

        public string LootTableId { get; }
        public IReadOnlyList<RuntimeLootDrop> Drops => drops;

        public RuntimeLootResult(string lootTableId, IEnumerable<RuntimeLootDrop> rolledDrops)
        {
            LootTableId = lootTableId ?? string.Empty;
            drops = new List<RuntimeLootDrop>(rolledDrops ?? new RuntimeLootDrop[0])
                .AsReadOnly();
        }
    }

    public sealed class RewardPackage
    {
        readonly IReadOnlyList<RuntimeLootDrop> itemDrops;

        public string StageId { get; }
        public int Gold { get; }
        public int Experience { get; }
        public bool IsFirstClear { get; }
        public IReadOnlyList<RuntimeLootDrop> ItemDrops => itemDrops;

        public RewardPackage(string stageId, int gold, int experience, bool isFirstClear,
            IEnumerable<RuntimeLootDrop> drops)
        {
            StageId = stageId;
            Gold = gold;
            Experience = experience;
            IsFirstClear = isFirstClear;
            itemDrops = new List<RuntimeLootDrop>(drops ?? new RuntimeLootDrop[0]).AsReadOnly();
        }
    }
}
