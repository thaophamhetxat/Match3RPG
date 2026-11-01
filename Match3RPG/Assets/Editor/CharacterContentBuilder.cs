#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Match3RPG.Battle;
using Match3RPG.Campaign;
using Match3RPG.Campaign.Data;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;

namespace Match3RPG.Editor
{
    public static class CharacterContentBuilder
    {
        const string AnimationOutput = "Assets/Animations";
        const string HeroDataFolder = "Assets/Resources/Characters/Heroes";
        const string BossDataFolder = "Assets/Resources/Campaign/Bosses";
        const string Hero2AnimationRoot = "Assets/Art/Characters/Hero2/Animations";
        const float IdleFps = 8f;
        const float ActionFps = 12f;
        static readonly BattleCharacterAnimState[] HeroBattleStates =
        {
            BattleCharacterAnimState.Idle,
            BattleCharacterAnimState.Attack,
            BattleCharacterAnimState.SpecAttack,
            BattleCharacterAnimState.Skill,
            BattleCharacterAnimState.Hit,
            BattleCharacterAnimState.Dodge,
            BattleCharacterAnimState.Rising,
            BattleCharacterAnimState.Death
        };
        static readonly BattleCharacterAnimState[] RequiredHeroStates =
        {
            BattleCharacterAnimState.Idle,
            BattleCharacterAnimState.Attack,
            BattleCharacterAnimState.Death
        };

        static readonly SheetDefinition[] BossSheets =
        {
            new SheetDefinition("Boss01", BattleCharacterAnimState.Idle,
                "Assets/Art/Bosses/Region01/boss01/Idle/boss1-idle-trimmed.png", 5, 4, 20, IdleFps, true),
            new SheetDefinition("Boss01", BattleCharacterAnimState.Attack,
                "Assets/Art/Bosses/Region01/boss01/Attack/Armored dragon's violet fireball attack-1.png", 5, 4, 17, ActionFps),
            new SheetDefinition("Boss01", BattleCharacterAnimState.SpecAttack,
                "Assets/Art/Bosses/Region01/boss01/SpecAttack/Armored Dragon’s Violet Orb Attack.png", 5, 4, 17, ActionFps),
            new SheetDefinition("Boss01", BattleCharacterAnimState.Death,
                "Assets/Art/Bosses/Region01/boss01/Death/Armored violet dragon defeat animation-3.png", 5, 4, 17, ActionFps),

            new SheetDefinition("Boss02", BattleCharacterAnimState.Idle,
                "Assets/Art/Bosses/Region01/boss02/Idle/Feathered dragon boss idle sprite sheet-1.png", 5, 3, 15, IdleFps, true),
            new SheetDefinition("Boss02", BattleCharacterAnimState.Attack,
                "Assets/Art/Bosses/Region01/boss02/Attack/Purple Dragon Attack Sprite Sheet-2.png", 5, 3, 15, ActionFps),
            new SheetDefinition("Boss02", BattleCharacterAnimState.SpecAttack,
                "Assets/Art/Bosses/Region01/boss02/SpecAttack/Violet Dragon Ultimate Attack Animation-3.png", 5, 3, 15, ActionFps),
            new SheetDefinition("Boss02", BattleCharacterAnimState.Death,
                "Assets/Art/Bosses/Region01/boss02/Death/Feathered Dragon Collapse Animation Sheet-4.png", 5, 3, 15, ActionFps)
        };

        [MenuItem("Tools/Characters/Build Character Content")]
        public static void Build()
        {
            EnsureFolder("Assets", "Animations");
            EnsureFolder(AnimationOutput, "Characters");
            EnsureFolder(AnimationOutput, "Bosses");
            EnsureFolder("Assets/Resources", "Characters");
            EnsureFolder("Assets/Resources/Characters", "Heroes");
            EnsureFolder("Assets/Resources/Campaign", "Bosses");

            Dictionary<string, Dictionary<BattleCharacterAnimState, Sprite[]>> characterFrames =
                new Dictionary<string, Dictionary<BattleCharacterAnimState, Sprite[]>>();
            Dictionary<BattleCharacterAnimState, int> heroFrameBodyHeights =
                new Dictionary<BattleCharacterAnimState, int>();
            foreach (SheetDefinition sourceSheet in DiscoverHero2Sheets(heroFrameBodyHeights).Concat(BossSheets))
            {
                SheetDefinition sheet = sourceSheet.State == BattleCharacterAnimState.Idle
                    ? FilterVisibleIdleFrames(sourceSheet)
                    : sourceSheet;
                if (!File.Exists(ToAbsolutePath(sheet.Path)))
                {
                    if (sheet.State == BattleCharacterAnimState.Idle
                        || sheet.State == BattleCharacterAnimState.Attack)
                        throw new FileNotFoundException(
                            $"Required {sheet.CharacterId} {sheet.State} sheet is missing.", sheet.Path);
                    Debug.LogWarning($"Optional {sheet.CharacterId} {sheet.State} sheet is unavailable: {sheet.Path}");
                    continue;
                }

                Sprite[] frames;
                try
                {
                    frames = ImportSheet(sheet);
                }
                catch (Exception exception) when (sheet.CharacterId == "Hero2"
                    && !RequiredHeroStates.Contains(sheet.State))
                {
                    Debug.LogWarning($"Hero2 optional {sheet.State} spritesheet skipped: "
                        + exception.Message);
                    continue;
                }
                if (!characterFrames.TryGetValue(sheet.CharacterId,
                    out Dictionary<BattleCharacterAnimState, Sprite[]> states))
                {
                    states = new Dictionary<BattleCharacterAnimState, Sprite[]>();
                    characterFrames.Add(sheet.CharacterId, states);
                }
                states[sheet.State] = frames;
            }

            foreach (string characterId in new[] { "Hero2", "Boss01", "Boss02" })
            {
                if (!characterFrames.TryGetValue(characterId,
                    out Dictionary<BattleCharacterAnimState, Sprite[]> states)
                    || !states.TryGetValue(BattleCharacterAnimState.Idle, out Sprite[] idleFrames)
                    || !states.TryGetValue(BattleCharacterAnimState.Attack, out Sprite[] attackFrames))
                    throw new InvalidOperationException(
                        $"{characterId} requires valid Idle and Attack animation sheets.");

                if (characterId == "Hero2")
                    foreach (BattleCharacterAnimState requiredState in RequiredHeroStates)
                        if (!states.TryGetValue(requiredState, out Sprite[] requiredFrames)
                            || requiredFrames.Length == 0)
                            throw new InvalidOperationException(
                                $"Hero2 requires valid {requiredState} frames before Battle animation can be generated.");

                CharacterAnimationSet animations = BuildAnimationSet(characterId, states);
                if (characterId == "Hero2")
                    BuildHeroData(characterId, animations, states, heroFrameBodyHeights);
                else
                    BuildBossData(characterId, animations, idleFrames[0]);
                Debug.Log($"Built {characterId}: {idleFrames.Length} Idle and "
                    + $"{attackFrames.Length} Attack frames; generated animation set "
                    + $"{AssetDatabase.GetAssetPath(animations)}.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static IEnumerable<SheetDefinition> DiscoverHero2Sheets(
            Dictionary<BattleCharacterAnimState, int> heroFrameBodyHeights)
        {
            string absoluteRoot = ToAbsolutePath(Hero2AnimationRoot);
            if (!Directory.Exists(absoluteRoot))
                throw new DirectoryNotFoundException(
                    $"Hero2 animation source folder is missing: {Hero2AnimationRoot}");

            foreach (BattleCharacterAnimState state in HeroBattleStates)
            {
                string folder = Path.Combine(absoluteRoot, state.ToString());
                string[] files = Directory.Exists(folder)
                    ? Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray()
                    : Array.Empty<string>();
                bool required = RequiredHeroStates.Contains(state);
                if (files.Length == 0)
                {
                    string message = $"Hero2 {state} has no source spritesheet in "
                        + $"{Hero2AnimationRoot}/{state}; "
                        + (required ? "this state is required." : "optional state skipped.");
                    if (required)
                        throw new FileNotFoundException(message,
                            $"{Hero2AnimationRoot}/{state}");
                    Debug.LogWarning(message);
                    continue;
                }
                if (files.Length > 1)
                {
                    string message = $"Hero2 {state} has multiple source spritesheets; "
                        + $"expected one direct PNG under {Hero2AnimationRoot}/{state}: "
                        + string.Join(", ", files.Select(Path.GetFileName));
                    if (required)
                        throw new InvalidOperationException(message);
                    Debug.LogWarning(message + "; optional state skipped.");
                    continue;
                }

                string assetPath = files[0].Replace('\\', '/');
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
                int columns;
                int rows;
                int width;
                int height;
                int frameCount;
                int firstFrameBodyHeight;
                try
                {
                    (columns, rows, width, height, frameCount, firstFrameBodyHeight)
                        = InspectSheet(assetPath);
                    if (frameCount == 0)
                    {
                        string message = $"Hero2 {state} spritesheet has no valid frames: {assetPath}";
                        if (required)
                            throw new InvalidDataException(message);
                        Debug.LogWarning(message + "; optional state skipped.");
                        continue;
                    }
                }
                catch (Exception exception) when (!required)
                {
                    Debug.LogWarning($"Hero2 optional {state} sheet skipped: {exception.Message}");
                    continue;
                }

                float frameRate = state == BattleCharacterAnimState.Idle
                    ? IdleFps : ActionFps;
                Debug.Log($"[HeroSheet] State={state}; Source={assetPath}; "
                    + $"Dimensions={width}x{height}; Grid={columns}x{rows}; "
                    + $"Cell={width / (float)columns:F2}x{height / (float)rows:F2}; "
                    + $"ValidFrames={frameCount}; "
                    + $"FirstFrameVisibleHeight={firstFrameBodyHeight}px.");
                if (assetPath.StartsWith(Hero2AnimationRoot, StringComparison.Ordinal))
                    heroFrameBodyHeights[state] = firstFrameBodyHeight;
                yield return new SheetDefinition("Hero2", state, assetPath,
                    columns, rows, frameCount, frameRate,
                    state == BattleCharacterAnimState.Idle);
            }
        }

        static (int columns, int rows, int width, int height, int frameCount,
            int firstFrameBodyHeight) InspectSheet(string assetPath)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture,
                    File.ReadAllBytes(ToAbsolutePath(assetPath)), false))
                    throw new InvalidDataException($"Could not decode Hero2 spritesheet: {assetPath}");

                const int columns = 5;
                int rows = Mathf.RoundToInt(columns * texture.height / (float)texture.width);
                float cellAspect = (texture.width / (float)columns)
                    / (texture.height / (float)rows);
                if (rows <= 0 || Mathf.Abs(cellAspect - 1f) > .02f)
                    throw new InvalidDataException(
                        $"Hero2 sheet frame cells are not approximately square: "
                        + $"{texture.width}x{texture.height} in a {columns}x{rows} grid.");

                Color32[] pixels = texture.GetPixels32();
                int[] occupiedPixelCounts = new int[columns * rows];
                int firstFrameMinY = texture.height;
                int firstFrameMaxY = -1;
                for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
                for (int column = 0; column < columns; column++)
                {
                    int xMin = column * texture.width / columns;
                    int xMax = (column + 1) * texture.width / columns;
                    int rowFromBottom = rows - rowFromTop - 1;
                    int yMin = rowFromBottom * texture.height / rows;
                    int yMax = (rowFromBottom + 1) * texture.height / rows;
                    int count = 0;
                    for (int y = yMin; y < yMax; y++)
                    for (int x = xMin; x < xMax; x++)
                        if (pixels[y * texture.width + x].a > 8)
                        {
                            count++;
                            if (rowFromTop == 0 && column == 0)
                            {
                                firstFrameMinY = Mathf.Min(firstFrameMinY, y);
                                firstFrameMaxY = Mathf.Max(firstFrameMaxY, y);
                            }
                        }
                    occupiedPixelCounts[rowFromTop * columns + column] = count;
                }

                int maximumCellPixels = occupiedPixelCounts.Max();
                int minimumContentPixels = Mathf.Max(128,
                    Mathf.RoundToInt(maximumCellPixels * .15f));
                int lastValidFrame = Array.FindLastIndex(occupiedPixelCounts,
                    count => count >= minimumContentPixels);
                for (int index = 0; index <= lastValidFrame; index++)
                    if (occupiedPixelCounts[index] < minimumContentPixels)
                        throw new InvalidDataException(
                            $"Hero2 sheet contains a blank frame cell before frame {lastValidFrame}: "
                            + $"{assetPath}");

                int firstFrameHeight = firstFrameMaxY >= firstFrameMinY
                    ? firstFrameMaxY - firstFrameMinY + 1
                    : 0;
                return (columns, rows, texture.width, texture.height,
                    lastValidFrame + 1, firstFrameHeight);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static Sprite[] ImportSheet(SheetDefinition sheet)
        {
            TextureImporter importer = AssetImporter.GetAtPath(sheet.Path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Texture importer is unavailable: {sheet.Path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();

            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, SpriteRect> existing = provider.GetSpriteRects()
                .ToDictionary(sprite => sprite.name, sprite => sprite);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            SpriteRect[] rectangles = new SpriteRect[sheet.FrameCount];
            for (int index = 0; index < sheet.FrameCount; index++)
            {
                int cellIndex = sheet.FrameCellIndices != null
                    ? sheet.FrameCellIndices[index]
                    : index;
                int column = cellIndex % sheet.Columns;
                int row = cellIndex / sheet.Columns;
                float x0 = column * width / (float)sheet.Columns;
                float x1 = (column + 1) * width / (float)sheet.Columns;
                float y0 = height - (row + 1) * height / (float)sheet.Rows;
                float y1 = height - row * height / (float)sheet.Rows;
                string spriteName = GetFrameName(sheet.CharacterId, sheet.State, index);
                rectangles[index] = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(x0, y0, x1 - x0, y1 - y0),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(.5f, 0f),
                    spriteID = existing.TryGetValue(spriteName, out SpriteRect old)
                        ? old.spriteID
                        : GUID.Generate()
                };
            }

            provider.SetSpriteRects(rectangles);
            provider.Apply();
            importer.SaveAndReimport();

            Dictionary<string, Sprite> imported = AssetDatabase.LoadAllAssetsAtPath(sheet.Path)
                .OfType<Sprite>().ToDictionary(sprite => sprite.name, sprite => sprite);
            Sprite[] frames = new Sprite[sheet.FrameCount];
            for (int index = 0; index < frames.Length; index++)
            {
                string frameName = GetFrameName(sheet.CharacterId, sheet.State, index);
                if (!imported.TryGetValue(frameName, out frames[index]))
                    throw new InvalidOperationException(
                        $"Imported animation frame is missing: {sheet.Path} -> {frameName}");
            }
            return frames;
        }

        static CharacterAnimationSet BuildAnimationSet(string characterId,
            Dictionary<BattleCharacterAnimState, Sprite[]> framesByState)
        {
            string assetFolder = characterId.StartsWith("Boss", StringComparison.Ordinal)
                ? $"{AnimationOutput}/Bosses/Region01/{characterId}"
                : $"{AnimationOutput}/Characters/{characterId}";
            EnsureAssetFolder(assetFolder);

            Dictionary<BattleCharacterAnimState, AnimationClip> clips =
                new Dictionary<BattleCharacterAnimState, AnimationClip>();
            foreach (KeyValuePair<BattleCharacterAnimState, Sprite[]> pair in framesByState)
            {
                string path = $"{assetFolder}/{characterId}_{pair.Key}.anim";
                clips[pair.Key] = CreateClip(path, characterId, pair.Key, pair.Value,
                    pair.Key == BattleCharacterAnimState.Idle ? IdleFps : ActionFps,
                    pair.Key == BattleCharacterAnimState.Idle);
                if (pair.Key == BattleCharacterAnimState.Idle)
                {
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(
                        clips[pair.Key],
                        EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"));
                    if (keys.Length == 0 || keys.Any(key => key.value == null))
                        throw new InvalidOperationException(
                            $"{characterId} Idle clip contains a missing Sprite keyframe.");
                    Debug.Log($"[IdleAnimation] Character={characterId}; Frames={keys.Length}; "
                        + $"FPS={clips[pair.Key].frameRate}; Duration={clips[pair.Key].length:F3}s; "
                        + $"FinalKey={keys[keys.Length - 1].time:F3}s; "
                        + $"Loop={AnimationUtility.GetAnimationClipSettings(clips[pair.Key]).loopTime}.");
                }
                if (characterId == "Hero2")
                    Debug.Log($"[HeroAnimationClip] State={pair.Key}; Clip={path}; "
                        + $"Frames={string.Join(", ", pair.Value.Select(frame => frame.name))}");
            }

            AnimatorController controller = CreateController(
                $"{assetFolder}/{characterId}.controller", clips);
            CharacterAnimationSet set = LoadOrCreate<CharacterAnimationSet>(
                $"{assetFolder}/{characterId}AnimationSet.asset");
            set.Configure(characterId, controller,
                GetClip(clips, BattleCharacterAnimState.Idle),
                GetClip(clips, BattleCharacterAnimState.Attack),
                GetClip(clips, BattleCharacterAnimState.SpecAttack),
                GetClip(clips, BattleCharacterAnimState.Skill),
                GetClip(clips, BattleCharacterAnimState.Hit),
                GetClip(clips, BattleCharacterAnimState.Dodge),
                GetClip(clips, BattleCharacterAnimState.Rising),
                GetClip(clips, BattleCharacterAnimState.Death),
                framesByState[BattleCharacterAnimState.Idle],
                framesByState[BattleCharacterAnimState.Attack],
                GetFrameCount(framesByState, BattleCharacterAnimState.SpecAttack),
                GetFrameCount(framesByState, BattleCharacterAnimState.Skill),
                GetFrameCount(framesByState, BattleCharacterAnimState.Hit),
                GetFrameCount(framesByState, BattleCharacterAnimState.Dodge),
                GetFrameCount(framesByState, BattleCharacterAnimState.Rising),
                GetFrameCount(framesByState, BattleCharacterAnimState.Death));
            EditorUtility.SetDirty(set);
            return set;
        }

        static int GetFrameCount(
            Dictionary<BattleCharacterAnimState, Sprite[]> framesByState,
            BattleCharacterAnimState state)
        {
            return framesByState.TryGetValue(state, out Sprite[] frames)
                ? frames.Length : 0;
        }

        static SheetDefinition FilterVisibleIdleFrames(SheetDefinition sheet)
        {
            string path = ToAbsolutePath(sheet.Path);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                    throw new InvalidDataException($"Could not decode Idle spritesheet: {sheet.Path}");

                Color32[] pixels = texture.GetPixels32();
                int[] counts = new int[sheet.Columns * sheet.Rows];
                for (int index = 0; index < counts.Length; index++)
                {
                    int column = index % sheet.Columns;
                    int rowFromTop = index / sheet.Columns;
                    int rowFromBottom = sheet.Rows - rowFromTop - 1;
                    int xMin = column * texture.width / sheet.Columns;
                    int xMax = (column + 1) * texture.width / sheet.Columns;
                    int yMin = rowFromBottom * texture.height / sheet.Rows;
                    int yMax = (rowFromBottom + 1) * texture.height / sheet.Rows;
                    for (int y = yMin; y < yMax; y++)
                    for (int x = xMin; x < xMax; x++)
                        if (pixels[y * texture.width + x].a > 8)
                            counts[index]++;
                }

                int minimumPixels = Mathf.Max(128,
                    Mathf.RoundToInt(counts.Max() * .15f));
                int[] validCells = Enumerable.Range(0, counts.Length)
                    .Where(index => counts[index] >= minimumPixels).ToArray();
                if (validCells.Length == 0)
                    throw new InvalidDataException(
                        $"{sheet.CharacterId} Idle sheet has no visible frames: {sheet.Path}");

                Debug.Log($"[IdleFrames] Character={sheet.CharacterId}; "
                    + $"GridCells={counts.Length}; ValidFrames={validCells.Length}; "
                    + $"ExcludedCells={counts.Length - validCells.Length}; "
                    + $"Source={sheet.Path}.");
                return new SheetDefinition(sheet.CharacterId, sheet.State, sheet.Path,
                    sheet.Columns, sheet.Rows, validCells.Length, sheet.FrameRate,
                    sheet.Loop, validCells);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static void BuildHeroData(string characterId, CharacterAnimationSet animations,
            Dictionary<BattleCharacterAnimState, Sprite[]> framesByState,
            Dictionary<BattleCharacterAnimState, int> firstFrameBodyHeights)
        {
            Sprite fallback = framesByState[BattleCharacterAnimState.Idle][0];
            Sprite preview = framesByState[BattleCharacterAnimState.Idle][0];
            if (!firstFrameBodyHeights.TryGetValue(BattleCharacterAnimState.Idle,
                    out int idleBodyHeight)
                || !firstFrameBodyHeights.TryGetValue(BattleCharacterAnimState.Attack,
                    out int attackBodyHeight)
                || idleBodyHeight <= 0 || attackBodyHeight <= 0)
                throw new InvalidOperationException(
                    "Hero2 Idle and Attack first-frame body bounds could not be measured.");
            float idleNormalization = attackBodyHeight / (float)idleBodyHeight;
            HeroData data = LoadOrCreate<HeroData>(
                $"{HeroDataFolder}/Hero2.asset");
            data.Configure(PlayerHeroSelectionService.HeroId, "Tạ Diệm",
                preview, preview, fallback, animations, 500, 20, 100, 100,
                1f, Vector2.zero, CharacterFacing.Right, idleNormalization);
            EditorUtility.SetDirty(data);
            Debug.Log($"[HeroBodySize] IdleFrameBodyHeight={idleBodyHeight}px; "
                + $"AttackFrameBodyHeight={attackBodyHeight}px; "
                + $"IdleNormalization={idleNormalization:F4}; "
                + "Attack is the reference.");
        }

        static void BuildBossData(string characterId, CharacterAnimationSet animations,
            Sprite idlePreview)
        {
            string id = characterId == "Boss01" ? "region_01_boss_01" : "region_01_boss_02";
            string path = $"{BossDataFolder}/{id}.asset";
            BossData boss = AssetDatabase.LoadAssetAtPath<BossData>(path);
            if (boss == null)
                throw new InvalidOperationException($"Campaign BossData asset is missing: {path}");
            boss.SetAnimationSet(animations);
            SerializedObject serialized = new SerializedObject(boss);
            serialized.FindProperty("portrait").objectReferenceValue = idlePreview;
            serialized.FindProperty("sourceFacing").enumValueIndex = (int)CharacterFacing.Right;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boss);
        }

        static AnimationClip CreateClip(string path, string characterId,
            BattleCharacterAnimState state, Sprite[] frames, float frameRate, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.name = $"{characterId}_{state}";
            clip.frameRate = frameRate;
            clip.ClearCurves();
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
            for (int index = 0; index < frames.Length; index++)
                keys[index] = new ObjectReferenceKeyframe
                {
                    time = index / frameRate,
                    value = frames[index]
                };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite"), keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimatorController CreateController(string path,
            Dictionary<BattleCharacterAnimState, AnimationClip> clips)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null)
                AssetDatabase.DeleteAsset(path);
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            AnimatorState attack = machine.AddState("Attack");
            machine.defaultState = idle;
            idle.motion = clips[BattleCharacterAnimState.Idle];
            attack.motion = clips[BattleCharacterAnimState.Attack];

            AnimatorStateTransition toAttack = idle.AddTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            AnimatorStateTransition attackToIdle = attack.AddTransition(idle);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime = 1f;
            attackToIdle.duration = 0f;

            foreach (KeyValuePair<BattleCharacterAnimState, AnimationClip> pair in clips)
            {
                if (pair.Key == BattleCharacterAnimState.Idle
                    || pair.Key == BattleCharacterAnimState.Attack
                    || pair.Key == BattleCharacterAnimState.Death)
                    continue;
                AnimatorState state = machine.AddState(pair.Key.ToString());
                state.motion = pair.Value;
                AnimatorStateTransition toIdle = state.AddTransition(idle);
                toIdle.hasExitTime = true;
                toIdle.exitTime = 1f;
                toIdle.duration = 0f;
            }

            if (clips.TryGetValue(BattleCharacterAnimState.Death, out AnimationClip deathClip))
            {
                AnimatorState death = machine.AddState("Death");
                death.motion = deathClip;
                AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(death);
                deathTransition.hasExitTime = false;
                deathTransition.duration = 0f;
                deathTransition.canTransitionToSelf = false;
                deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Death");
            }

            EditorUtility.SetDirty(controller);
            return controller;
        }

        static AnimationClip GetClip(Dictionary<BattleCharacterAnimState, AnimationClip> clips,
            BattleCharacterAnimState state)
        {
            return clips.TryGetValue(state, out AnimationClip clip) ? clip : null;
        }

        static string GetFrameName(string characterId, BattleCharacterAnimState state, int index)
        {
            return $"{characterId}_{state}_{index:D2}";
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureAssetFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folder);
        }

        static string ToAbsolutePath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        sealed class SheetDefinition
        {
            public readonly string CharacterId;
            public readonly BattleCharacterAnimState State;
            public readonly string Path;
            public readonly int Columns;
            public readonly int Rows;
            public readonly int FrameCount;
            public readonly float FrameRate;
            public readonly bool Loop;
            public readonly int[] FrameCellIndices;

            public SheetDefinition(string characterId, BattleCharacterAnimState state,
                string path, int columns, int rows, int frameCount, float frameRate,
                bool loop = false, int[] frameCellIndices = null)
            {
                CharacterId = characterId;
                State = state;
                Path = path;
                Columns = columns;
                Rows = rows;
                FrameCount = frameCount;
                FrameRate = frameRate;
                Loop = loop;
                FrameCellIndices = frameCellIndices;
            }
        }
    }

}
#endif
