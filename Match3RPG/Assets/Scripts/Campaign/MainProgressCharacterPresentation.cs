using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Match3RPG.Campaign
{
    [CreateAssetMenu(menuName = "Match3RPG/Main Progress/Character Presentation")]
    public sealed class MainProgressCharacterPresentation : ScriptableObject
    {
        const string StaticCharacterAssetPath = "Assets/Art/MainProgress/Image/Seated cultivator with flowing robes.png";

        [SerializeField] Sprite progressBackground;
        [SerializeField] Sprite staticCharacterSprite;
        [SerializeField, Min(.1f)] float baseMeditationScale = 1.1f;
        [SerializeField, Range(1f, 1.5f)] float meditationScaleMultiplier = 1.28f;
        [SerializeField] Vector2 mainProgressCharacterOffset = new Vector2(0f, -18f);
        [SerializeField] Vector2 lotusAnchor = new Vector2(.5f, .38f);

        public Sprite ProgressBackground => progressBackground;
        public Sprite MainProgressCharacterSprite => staticCharacterSprite != null ? staticCharacterSprite : ResolveStaticCharacterSprite();
        public float MainProgressCharacterScale => Mathf.Max(.1f, baseMeditationScale * meditationScaleMultiplier);
        public Vector2 MainProgressCharacterOffset => mainProgressCharacterOffset;
        public Vector2 LotusAnchor => lotusAnchor;

        public static MainProgressCharacterPresentation LoadDefault()
        {
            MainProgressCharacterPresentation loaded = Resources.Load<MainProgressCharacterPresentation>("MainProgress/MainProgressCharacterPresentation");
            if (loaded != null)
                return loaded;

            MainProgressCharacterPresentation fallback = CreateInstance<MainProgressCharacterPresentation>();
            fallback.progressBackground = Resources.Load<Sprite>("MainProgress/Progress/Blank Cultivation HUD Progress Bar");
            fallback.staticCharacterSprite = ResolveStaticCharacterSprite();
            fallback.baseMeditationScale = 1.1f;
            fallback.meditationScaleMultiplier = 1.28f;
            fallback.mainProgressCharacterOffset = new Vector2(0f, -140f);
            fallback.lotusAnchor = new Vector2(0.60f, 0.31f);

            if (fallback.staticCharacterSprite == null)
                Debug.LogWarning("MainProgress static character sprite was not resolved from Assets/Art/MainProgress/Image. Ensure the source asset exists at the required path.");

            return fallback;
        }

        static Sprite ResolveStaticCharacterSprite()
        {
#if UNITY_EDITOR
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StaticCharacterAssetPath);
            if (sprite != null)
                return sprite;
#endif

            return Resources.Load<Sprite>("MainProgress/Image/Seated cultivator with flowing robes");
        }
    }
}
