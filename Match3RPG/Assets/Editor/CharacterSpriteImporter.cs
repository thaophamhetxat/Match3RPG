#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Match3RPG.Battle;
using Match3RPG.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Editor
{
    [InitializeOnLoad]
    public sealed class CharacterSpriteImporter : AssetPostprocessor
    {
        const string CharacterFolder = "Assets/Art/Characters/";
        const string BossFolder = "Assets/Art/Bosses/";
        const string GameScenePath = "Assets/Scenes/Game.unity";
        const string Hero2AnimationRoot = "Assets/Art/Characters/Hero2/Animations/";
        const string BossAnimationRoot = "Assets/Art/Bosses/Region01/";
        const string BossSpritePath = "Assets/Art/Bosses/Boss01/boss1_Idle.png";
        const string ReimportPreferencePrefix = "Match3RPG.CharacterSpriteImporter.Reimported.";
        static readonly string[] Hero2BattleFolders =
        {
            "Idle", "Attack", "SpecAttack", "Skill", "Hit", "Dodge", "Rising", "Death"
        };
        static readonly string[] BossBattleFolders =
        {
            "Idle", "Attack", "SpecAttack", "Death"
        };

        static CharacterSpriteImporter()
        {
            string gemPreferenceKey = "Match3RPG.GemV2Importer.Imported."
                + Application.dataPath.Replace('\\', '/');
            if (!EditorPrefs.GetBool(gemPreferenceKey, false))
            {
                EditorPrefs.SetBool(gemPreferenceKey, true);
                EditorApplication.delayCall += ReimportExistingGemV2Sprites;
            }
            EditorApplication.delayCall += AssignSceneGemV2Sprites;

            string preferenceKey = ReimportPreferencePrefix + Application.dataPath.Replace('\\', '/');
            if (EditorPrefs.GetBool(preferenceKey, false)) return;
            EditorPrefs.SetBool(preferenceKey, true);
            EditorApplication.delayCall += ReimportExistingCharacterSprites;
        }

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (IsHero2BattleSheet(path))
            {
                ConfigureSpriteSheetImporter((TextureImporter)assetImporter);
                return;
            }
            if (IsBossBattleSheet(path))
            {
                ConfigureSpriteSheetImporter((TextureImporter)assetImporter);
                return;
            }

            if (path.StartsWith("Assets/Art/Gems/V2/", StringComparison.OrdinalIgnoreCase)
                && path.IndexOf("/Reference/", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    ConfigureSpriteImporter((TextureImporter)assetImporter);
                    TextureImporter gemImporter = (TextureImporter)assetImporter;
                    TextureImporterSettings gemSettings = new TextureImporterSettings();
                    gemImporter.ReadTextureSettings(gemSettings);
                    gemSettings.spriteAlignment = (int)SpriteAlignment.Center;
                    gemSettings.spriteMeshType = SpriteMeshType.FullRect;
                    gemImporter.SetTextureSettings(gemSettings);
                }
                return;
            }

            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || (!path.StartsWith(CharacterFolder, StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith(BossFolder, StringComparison.OrdinalIgnoreCase)))
                return;

            ConfigureSpriteImporter((TextureImporter)assetImporter);
        }

        static void ConfigureSpriteImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.compressionQuality = 100;
            SetUncompressedPlatform(importer, "Standalone");
            SetUncompressedPlatform(importer, "Android");
            SetUncompressedPlatform(importer, "WebGL");
        }

        static void ConfigureSpriteSheetImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.compressionQuality = 100;
            importer.maxTextureSize = 4096;
            SetUncompressedPlatform(importer, "Standalone", 4096);
            SetUncompressedPlatform(importer, "Android", 4096);
            SetUncompressedPlatform(importer, "WebGL", 4096);
        }

        static bool IsHero2BattleSheet(string path)
        {
            if (!path.StartsWith(Hero2AnimationRoot, StringComparison.OrdinalIgnoreCase)
                || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;

            string relativePath = path.Substring(Hero2AnimationRoot.Length);
            int separator = relativePath.IndexOf('/');
            if (separator <= 0)
                return false;

            string folder = relativePath.Substring(0, separator);
            return Array.IndexOf(Hero2BattleFolders, folder) >= 0;
        }

        static bool IsBossBattleSheet(string path)
        {
            if (!path.StartsWith(BossAnimationRoot, StringComparison.OrdinalIgnoreCase)
                || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;

            string[] segments = path.Substring(BossAnimationRoot.Length).Split('/');
            return segments.Length == 3
                && (string.Equals(segments[0], "boss01", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(segments[0], "boss02", StringComparison.OrdinalIgnoreCase))
                && Array.IndexOf(BossBattleFolders, segments[1]) >= 0;
        }

        [MenuItem("Tools/Match3RPG/Reimport Character Sprites")]
        public static void ReimportExistingCharacterSprites()
        {
            string absoluteHero2Root = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                Hero2AnimationRoot.Replace('/', Path.DirectorySeparatorChar));
            foreach (string folder in Hero2BattleFolders)
            {
                string sourceFolder = Path.Combine(absoluteHero2Root, folder);
                if (!Directory.Exists(sourceFolder))
                    continue;
                foreach (string file in Directory.GetFiles(sourceFolder, "*.png",
                    SearchOption.TopDirectoryOnly))
                {
                    string assetPath = file.Replace('\\', '/');
                    assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
            ImportCharacterSprite(BossSpritePath);
            CharacterContentBuilder.Build();
            ReimportExistingGemV2Sprites();
            AssignSceneBossSprite();
        }

        [MenuItem("Tools/Match3RPG/Reimport Gem V2 Sprites")]
        public static void ReimportExistingGemV2Sprites()
        {
            string root = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "Assets/Art/Gems/V2");
            if (!Directory.Exists(root))
            {
                Debug.LogWarning($"Gem V2 asset folder was not found: {root}");
                return;
            }

            foreach (string file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                if (path.IndexOf("/Reference/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                string assetPath = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError($"Gem V2 texture has no TextureImporter: {assetPath}");
                    continue;
                }
                if (!HasRequiredGemV2Settings(importer))
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
            AssignSceneGemV2Sprites();
            ValidateGemV2Imports(root);
            ValidateSceneGemV2References();
        }

        static void ImportCharacterSprite(string path)
        {
            string absolutePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, path);
            if (!File.Exists(absolutePath))
            {
                Debug.LogWarning($"Character sprite not found for automatic import: {path}");
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static void SetUncompressedPlatform(TextureImporter importer, string platformName,
            int maxTextureSize = 0)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            settings.name = platformName;
            settings.overridden = true;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.crunchedCompression = false;
            settings.compressionQuality = 100;
            if (maxTextureSize > 0)
                settings.maxTextureSize = maxTextureSize;
            importer.SetPlatformTextureSettings(settings);
        }

        static void AssignSceneBossSprite()
        {
            Sprite boss = AssetDatabase.LoadAssetAtPath<Sprite>(BossSpritePath);
            if (boss == null) Debug.LogWarning($"Boss sprite did not load as a Sprite: {BossSpritePath}");
            if (boss == null) return;

            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (GameBootstrap bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
            {
                SerializedObject serialized = new SerializedObject(bootstrap);
                changed |= Assign(serialized.FindProperty("bossIdleSprite"), boss);
                if (!changed) continue;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bootstrap);
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Character sprites assigned to GameBootstrap in Game.unity.");
            }

            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }

        static void AssignSceneGemV2Sprites()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= ResumeGemV2SpriteAssignment;
                EditorApplication.playModeStateChanged += ResumeGemV2SpriteAssignment;
                return;
            }

            Sprite[] baseSprites = LoadGemBaseSprites();
            Sprite[] specialSprites = LoadNumberedSprites("Special", "Base_", 7, true);
            Sprite rainbowActive = LoadNumberedSprite("Special", "Base_08_", false);
            Sprite selection = LoadExactSprite("Effects/Special_Selection.png", true);
            Sprite horizontalBeam = LoadActivationSprite("HorizontalBeam");
            Sprite verticalBeam = LoadActivationSprite("VerticalBeam");
            Sprite purpleBurst = LoadActivationSprite("PurpleBurst");
            Sprite prismaticBurst = LoadActivationSprite("PrismaticBurst");

            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
            bool sceneWasDirty = scene.isDirty;

            bool changed = false;
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            foreach (GameBootstrap bootstrap in rootObject.GetComponentsInChildren<GameBootstrap>(true))
            {
                SerializedObject serialized = new SerializedObject(bootstrap);
                bool bootstrapChanged = AssignArray(serialized.FindProperty("gemV2BaseSprites"), baseSprites)
                    | AssignArray(serialized.FindProperty("gemV2SpecialSprites"), specialSprites);
                bootstrapChanged |= Assign(serialized.FindProperty("rainbowActiveSprite"), rainbowActive);
                bootstrapChanged |= Assign(serialized.FindProperty("selectionOverlaySprite"), selection);
                bootstrapChanged |= Assign(serialized.FindProperty("horizontalBeamSprite"), horizontalBeam);
                bootstrapChanged |= Assign(serialized.FindProperty("verticalBeamSprite"), verticalBeam);
                bootstrapChanged |= Assign(serialized.FindProperty("purpleBurstSprite"), purpleBurst);
                bootstrapChanged |= Assign(serialized.FindProperty("prismaticBurstSprite"), prismaticBurst);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (bootstrapChanged)
                {
                    EditorUtility.SetDirty(bootstrap);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!sceneWasDirty || openedHere)
                    EditorSceneManager.SaveScene(scene);
                Debug.Log("Gem V2 base, shared special, rainbow, selection, and activation sprites assigned to Game.unity.");
            }
            else
                Debug.Log("Gem V2 Sprite references verified in Game.unity.");
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }

        static void ResumeGemV2SpriteAssignment(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.playModeStateChanged -= ResumeGemV2SpriteAssignment;
            EditorApplication.delayCall += AssignSceneGemV2Sprites;
        }

        static Sprite[] LoadNumberedSprites(string folder, string filePrefix,
            int spriteCount, bool required)
        {
            Sprite[] sprites = new Sprite[spriteCount];
            for (int index = 0; index < sprites.Length; index++)
            {
                sprites[index] = LoadNumberedSprite(folder,
                    $"{filePrefix}{index + 1:00}_", required);
            }
            return sprites;
        }

        static Sprite[] LoadGemBaseSprites()
        {
            Sprite[] sprites = new Sprite[10];
            Sprite[] ordinarySprites = LoadNumberedSprites("Base", "Base_", 8, true);
            Array.Copy(ordinarySprites, sprites, ordinarySprites.Length);
            sprites[8] = LoadNumberedSprite("Other", "Base_09_", true);
            sprites[9] = LoadNumberedSprite("Other", "Base_10_", true);
            return sprites;
        }

        static Sprite LoadNumberedSprite(string folder, string prefix, bool required)
        {
            string assetFolder = $"Assets/Art/Gems/V2/{folder}";
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { assetFolder });
            foreach (string guid in paths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }
            if (required)
                Debug.LogError($"Required Gem V2 sprite is missing: {assetFolder}/{prefix}*.png");
            return null;
        }

        static Sprite LoadExactSprite(string relativePath, bool required)
        {
            string path = $"Assets/Art/Gems/V2/{relativePath}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                if (required) Debug.LogError($"Required Gem V2 sprite is missing: {path}");
            }
            return sprite;
        }

        static Sprite LoadActivationSprite(string concept)
        {
            string folder = "Assets/Art/Gems/V2/ActivationVFX";
            string prefix = $"GemV2_ActivationVFX_{concept}.";
            string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string guid in paths)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                    return sprite;
            }
            Debug.LogError($"Required Gem V2 activation sprite is missing: {folder}/GemV2_ActivationVFX_{concept}.png");
            return null;
        }

        static bool AssignArray(SerializedProperty property, Sprite[] sprites)
        {
            if (property == null) return false;
            bool changed = property.arraySize != sprites.Length;
            property.arraySize = sprites.Length;
            for (int index = 0; index < sprites.Length; index++)
                changed |= Assign(property.GetArrayElementAtIndex(index), sprites[index]);
            return changed;
        }

        static bool Assign(SerializedProperty property, Sprite sprite)
        {
            if (property == null || property.objectReferenceValue == sprite) return false;
            property.objectReferenceValue = sprite;
            return true;
        }

        static void ValidateGemV2Imports(string root)
        {
            bool valid = true;
            int validatedCount = 0;
            foreach (string file in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                string normalizedPath = file.Replace('\\', '/');
                if (normalizedPath.IndexOf("/Reference/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                string assetPath = "Assets" + normalizedPath.Substring(Application.dataPath.Length);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                bool importedCorrectly = importer != null && sprite != null
                    && HasRequiredGemV2Settings(importer);
                if (!importedCorrectly)
                {
                    valid = false;
                    Debug.LogError($"Gem V2 import validation failed: {assetPath}", importer);
                    continue;
                }
                validatedCount++;
            }

            if (valid)
                Debug.Log($"Validated {validatedCount} Gem V2 PNGs outside Reference.");
        }

        static bool HasRequiredGemV2Settings(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single
                && importer.alphaIsTransparency
                && importer.filterMode == FilterMode.Bilinear
                && importer.wrapMode == TextureWrapMode.Clamp
                && !importer.mipmapEnabled
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && !importer.crunchedCompression
                && importer.compressionQuality == 100
                && settings.spriteAlignment == (int)SpriteAlignment.Center
                && settings.spriteMeshType == SpriteMeshType.FullRect
                && HasUncompressedPlatformSettings(importer, "Standalone")
                && HasUncompressedPlatformSettings(importer, "Android")
                && HasUncompressedPlatformSettings(importer, "WebGL");
        }

        static bool HasUncompressedPlatformSettings(TextureImporter importer, string platformName)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            return settings.overridden
                && settings.textureCompression == TextureImporterCompression.Uncompressed
                && !settings.crunchedCompression
                && settings.compressionQuality == 100;
        }

        static void ValidateSceneGemV2References()
        {
            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

            Sprite[] bases = LoadGemBaseSprites();
            Sprite[] specials = LoadNumberedSprites("Special", "Base_", 7, true);
            bool foundBootstrap = false;
            bool valid = true;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (GameBootstrap bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
            {
                foundBootstrap = true;
                SerializedObject serialized = new SerializedObject(bootstrap);
                valid &= VerifyArray(serialized.FindProperty("gemV2BaseSprites"), bases, bootstrap);
                valid &= VerifyArray(serialized.FindProperty("gemV2SpecialSprites"), specials, bootstrap);
                valid &= VerifyReference(serialized, "rainbowActiveSprite",
                    LoadNumberedSprite("Special", "Base_08_", false), bootstrap);
                valid &= VerifyReference(serialized, "selectionOverlaySprite",
                    LoadExactSprite("Effects/Special_Selection.png", true), bootstrap);
                valid &= VerifyReference(serialized, "horizontalBeamSprite",
                    LoadActivationSprite("HorizontalBeam"), bootstrap);
                valid &= VerifyReference(serialized, "verticalBeamSprite",
                    LoadActivationSprite("VerticalBeam"), bootstrap);
                valid &= VerifyReference(serialized, "purpleBurstSprite",
                    LoadActivationSprite("PurpleBurst"), bootstrap);
                valid &= VerifyReference(serialized, "prismaticBurstSprite",
                    LoadActivationSprite("PrismaticBurst"), bootstrap);
            }

            if (!foundBootstrap)
            {
                valid = false;
                Debug.LogError($"No GameBootstrap found in {GameScenePath}.");
            }
            if (valid)
                Debug.Log("All required Gem V2 sprite references resolve correctly in Game.unity.");
            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }

        static bool VerifyArray(SerializedProperty property, Sprite[] expected, UnityEngine.Object context)
        {
            if (property == null || property.arraySize != expected.Length)
            {
                Debug.LogError($"Gem V2 scene Sprite array is missing or has the wrong size on {context.name}.", context);
                return false;
            }
            for (int index = 0; index < expected.Length; index++)
            {
                if (expected[index] != null
                    && property.GetArrayElementAtIndex(index).objectReferenceValue == expected[index])
                    continue;
                Debug.LogError($"Gem V2 scene Sprite reference {property.name}[{index}] does not resolve on {context.name}.", context);
                return false;
            }
            return true;
        }

        static bool VerifyReference(SerializedObject serialized, string propertyName,
            Sprite expected, UnityEngine.Object context)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (expected != null && property != null && property.objectReferenceValue == expected)
                return true;
            Debug.LogError($"Gem V2 scene Sprite reference '{propertyName}' does not resolve on {context.name}.", context);
            return false;
        }
    }
}
#endif