using System;
using UnityEngine;

namespace Match3RPG.Battle
{
    [CreateAssetMenu(menuName = "Match3 RPG/Battle/Combat VFX Catalog")]
    public sealed class CombatVFXCatalog : ScriptableObject
    {
        [Serializable]
        struct Entry
        {
            public CombatVFXType type;
            public Sprite sprite;
            public Sprite[] frames;
        }

        [SerializeField] Entry[] entries = Array.Empty<Entry>();

        public Sprite GetSprite(CombatVFXType type)
        {
            if (entries == null)
                return null;
            foreach (Entry entry in entries)
                if (entry.type == type)
                    return entry.sprite;
            return null;
        }

        public Sprite[] GetFrames(CombatVFXType type)
        {
            if (entries == null)
                return null;
            foreach (Entry entry in entries)
                if (entry.type == type)
                    return entry.frames;
            return null;
        }

        public void Configure(CombatVFXType[] types, Sprite[] sprites)
        {
            Configure(types, sprites, null);
        }

        public void Configure(CombatVFXType[] types, Sprite[] sprites, Sprite[][] frames)
        {
            int count = Mathf.Min(types != null ? types.Length : 0,
                sprites != null ? sprites.Length : 0);
            entries = new Entry[count];
            for (int index = 0; index < count; index++)
                entries[index] = new Entry
                {
                    type = types[index],
                    sprite = sprites[index],
                    frames = frames != null && index < frames.Length ? frames[index] : null
                };
        }
    }
}
