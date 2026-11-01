using Match3RPG.Core;
using UnityEngine;

namespace Match3RPG.Configuration
{
    public abstract class StableIdConfig : ScriptableObject
    {
        [SerializeField] string stableId;

        public string StableId => stableId;
        public bool HasValidStableId => StableIdRules.IsValid(stableId);

        protected void SetStableId(string value)
        {
            stableId = value;
        }
    }
}
