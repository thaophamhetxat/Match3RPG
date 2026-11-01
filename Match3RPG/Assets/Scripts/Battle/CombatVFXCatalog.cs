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

        public void Configure(CombatVFXType[] types, Sprite[] sprites)
        {
            int count = Mathf.Min(types != null ? types.Length : 0,
                sprites != null ? sprites.Length : 0);
            entries = new Entry[count];
            for (int index = 0; index < count; index++)
                entries[index] = new Entry { type = types[index], sprite = sprites[index] };
        }
    }
}
