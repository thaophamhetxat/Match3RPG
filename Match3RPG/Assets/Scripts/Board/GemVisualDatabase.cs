using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Board
{
    public sealed class GemVisualDatabase
    {
        readonly Sprite[] baseSprites;
        readonly Sprite[] specialSprites;

        public Sprite RainbowActiveSprite { get; }

        public GemVisualDatabase(Sprite[] bases, Sprite[] specials, Sprite rainbowActiveSprite)
        {
            baseSprites = bases ?? new Sprite[0];
            specialSprites = specials ?? new Sprite[0];
            RainbowActiveSprite = rainbowActiveSprite;
        }

        public Sprite GetSprite(GemType type, GemSpecialType specialType)
        {
            if (specialType == GemSpecialType.Lightning)
                return GetBaseSprite(GemType.Lightning);
            if (specialType == GemSpecialType.Bagua)
                return GetBaseSprite(GemType.Bagua);

            if (specialType == GemSpecialType.Rainbow || type == GemType.Rainbow)
            {
                if (RainbowActiveSprite != null)
                    return RainbowActiveSprite;
                return GetBaseSprite(GemType.Rainbow);
            }

            if (specialType == GemSpecialType.Line
                || specialType == GemSpecialType.AreaBomb)
                return GetSpecialSprite(type);

            return GetBaseSprite(type);
        }

        public Sprite GetBaseSprite(GemType type)
        {
            switch (type)
            {
                case GemType.Attack: return GetSpriteAt(baseSprites, 0);
                case GemType.Mana: return GetSpriteAt(baseSprites, 1);
                case GemType.ExpDrain: return GetSpriteAt(baseSprites, 2);
                case GemType.Rage: return GetSpriteAt(baseSprites, 3);
                case GemType.Health: return GetSpriteAt(baseSprites, 4);
                case GemType.Shield: return GetSpriteAt(baseSprites, 5);
                case GemType.Soul: return GetSpriteAt(baseSprites, 6);
                case GemType.Rainbow: return GetSpriteAt(baseSprites, 7);
                case GemType.Lightning: return GetSpriteAt(baseSprites, 8);
                case GemType.Bagua: return GetSpriteAt(baseSprites, 9);
                default: return null;
            }
        }

        public Sprite GetSpecialSprite(GemType gemType)
        {
            switch (gemType)
            {
                case GemType.Attack: return GetSpriteAt(specialSprites, 0);
                case GemType.Mana: return GetSpriteAt(specialSprites, 1);
                case GemType.ExpDrain: return GetSpriteAt(specialSprites, 2);
                case GemType.Rage: return GetSpriteAt(specialSprites, 3);
                case GemType.Health: return GetSpriteAt(specialSprites, 4);
                case GemType.Shield: return GetSpriteAt(specialSprites, 5);
                case GemType.Soul: return GetSpriteAt(specialSprites, 6);
                default: return null;
            }
        }

        public bool HasSpecialSprite(GemType type, GemSpecialType specialType)
        {
            if (specialType == GemSpecialType.Line
                || specialType == GemSpecialType.AreaBomb)
                return GetSpecialSprite(type) != null;
            return true;
        }

        public static string ExpectedVisualMapping(GemType type, GemSpecialType specialType)
        {
            if (specialType == GemSpecialType.Line || specialType == GemSpecialType.AreaBomb)
                return ExpectedSpecialMapping(type);
            if (specialType == GemSpecialType.Lightning)
                return ExpectedBaseMapping(GemType.Lightning);
            if (specialType == GemSpecialType.Bagua)
                return ExpectedBaseMapping(GemType.Bagua);
            if (type == GemType.Rainbow || specialType == GemSpecialType.Rainbow)
                return "Assets/Art/Gems/V2/Special/Base_08_Spiral.png";
            return ExpectedBaseMapping(type);
        }

        public static string ExpectedSpecialMapping(GemType type)
        {
            switch (type)
            {
                case GemType.Attack: return "Assets/Art/Gems/V2/Special/Base_01_Sword_special.png";
                case GemType.Mana: return "Assets/Art/Gems/V2/Special/Base_02_YinYang_special.png";
                case GemType.ExpDrain: return "Assets/Art/Gems/V2/Special/Base_03_Star_special.png";
                case GemType.Rage: return "Assets/Art/Gems/V2/Special/Base_04_GoldIngot_special.png";
                case GemType.Health: return "Assets/Art/Gems/V2/Special/Base_05_Heart_special.png";
                case GemType.Shield: return "Assets/Art/Gems/V2/Special/Base_06_Fire_special.png";
                case GemType.Soul: return "Assets/Art/Gems/V2/Special/Base_07_Diamond_special.png";
                case GemType.Lightning: return "Assets/Art/Gems/V2/Other/Base_09_PurpleLightning.png";
                case GemType.Bagua: return "Assets/Art/Gems/V2/Other/Base_10_YinYang_Black.png";
                default: return "Assets/Art/Gems/V2/<unknown gem sprite>";
            }
        }

        public static string ExpectedBaseMapping(GemType type)
        {
            switch (type)
            {
                case GemType.Attack: return "Assets/Art/Gems/V2/Base/Base_01_Sword.png";
                case GemType.Mana: return "Assets/Art/Gems/V2/Base/Base_02_YinYang.png";
                case GemType.ExpDrain: return "Assets/Art/Gems/V2/Base/Base_03_Star.png";
                case GemType.Rage: return "Assets/Art/Gems/V2/Base/Base_04_GoldIngot.png";
                case GemType.Health: return "Assets/Art/Gems/V2/Base/Base_05_Heart.png";
                case GemType.Shield: return "Assets/Art/Gems/V2/Base/Base_06_Fire.png";
                case GemType.Soul: return "Assets/Art/Gems/V2/Base/Base_07_Diamond.png";
                case GemType.Rainbow: return "Assets/Art/Gems/V2/Base/Base_08_Spiral.png";
                case GemType.Lightning: return "Assets/Art/Gems/V2/Other/Base_09_PurpleLightning.png";
                case GemType.Bagua: return "Assets/Art/Gems/V2/Other/Base_10_YinYang_Black.png";
                default: return "Assets/Art/Gems/V2/<unknown gem sprite>";
            }
        }

        static Sprite GetSpriteAt(Sprite[] sprites, int index)
        {
            return index >= 0 && index < sprites.Length ? sprites[index] : null;
        }
    }
}
