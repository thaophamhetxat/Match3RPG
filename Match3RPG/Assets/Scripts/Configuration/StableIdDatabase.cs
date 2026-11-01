using System;
using System.Collections.Generic;
using UnityEngine;

namespace Match3RPG.Configuration
{
    public abstract class StableIdDatabase<T> : ScriptableObject where T : UnityEngine.Object
    {
        readonly Dictionary<string, T> definitionsById =
            new Dictionary<string, T>(StringComparer.Ordinal);
        bool isInitialized;

        public int Count
        {
            get
            {
                EnsureInitialized();
                return definitionsById.Count;
            }
        }

        protected abstract IEnumerable<T> EnumerateDefinitions();
        protected abstract string GetStableId(T definition);

        protected virtual void OnEnable()
        {
            RebuildCache();
        }

        protected virtual void OnValidate()
        {
            RebuildCache();
        }

        public T GetById(string stableId)
        {
            return TryGetById(stableId, out T definition) ? definition : null;
        }

        public bool TryGetById(string stableId, out T definition)
        {
            EnsureInitialized();
            if (!string.IsNullOrEmpty(stableId))
                return definitionsById.TryGetValue(stableId, out definition);

            definition = null;
            return false;
        }

        protected void RebuildCache()
        {
            definitionsById.Clear();
            foreach (T definition in EnumerateDefinitions())
            {
                if (definition == null)
                    continue;

                string stableId = GetStableId(definition);
                if (!Match3RPG.Core.StableIdRules.IsValid(stableId))
                {
                    Debug.LogError($"Invalid or missing stable ID in {name}: {definition.name}.", this);
                    continue;
                }

                if (!definitionsById.TryAdd(stableId, definition))
                    Debug.LogError($"Duplicate stable ID '{stableId}' in {name}.", this);
            }
            isInitialized = true;
        }

        void EnsureInitialized()
        {
            if (!isInitialized)
                RebuildCache();
        }
    }
}
