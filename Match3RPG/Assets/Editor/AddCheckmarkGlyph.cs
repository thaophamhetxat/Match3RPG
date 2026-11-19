using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Match3RPG.Editor
{
    public static class AddCheckmarkGlyph
    {
        const string MainFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string FallbackFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/CheckmarkFallback SDF.asset";
        const string SourceFontFilePath = @"C:\Windows\Fonts\seguisym.ttf";
        const uint CheckmarkUnicode = 0x2713;
        const int PointSize = 86;
        const int AtlasPadding = 9;
        const int AtlasSize = 256;

        [MenuItem("Tools/Match3RPG/Add Checkmark Glyph (U+2713) to LiberationSans SDF")]
        public static void Run()
        {
            TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontAssetPath);
            if (mainFont == null)
            {
                Debug.LogError($"[AddCheckmarkGlyph] Main font asset not found at '{MainFontAssetPath}'.");
                return;
            }

            if (!File.Exists(SourceFontFilePath))
            {
                Debug.LogError($"[AddCheckmarkGlyph] Source font file not found at '{SourceFontFilePath}'.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath) != null)
                AssetDatabase.DeleteAsset(FallbackFontAssetPath);

            TMP_FontAsset fallbackFont = TMP_FontAsset.CreateFontAsset(
                SourceFontFilePath, 0, PointSize, AtlasPadding, GlyphRenderMode.SDFAA, AtlasSize, AtlasSize);
            if (fallbackFont == null)
            {
                Debug.LogError($"[AddCheckmarkGlyph] Failed to create TMP_FontAsset from '{SourceFontFilePath}'.");
                return;
            }

            bool added = fallbackFont.TryAddCharacters(new[] { CheckmarkUnicode }, out uint[] missingUnicodes);
            if (!added || !fallbackFont.HasCharacter((int)CheckmarkUnicode))
            {
                string missing = missingUnicodes != null && missingUnicodes.Length > 0
                    ? string.Join(", ", missingUnicodes)
                    : "none";
                Debug.LogError(
                    $"[AddCheckmarkGlyph] Failed to bake U+2713 from '{SourceFontFilePath}'. added={added}, missing=[{missing}].");
                Object.DestroyImmediate(fallbackFont);
                return;
            }

            // Convert to pure Static atlas so no external .ttf file is referenced or bundled.
            fallbackFont.atlasPopulationMode = AtlasPopulationMode.Static;
            fallbackFont.name = "CheckmarkFallback SDF";

            Texture2D atlasTexture = fallbackFont.atlasTextures != null && fallbackFont.atlasTextures.Length > 0
                ? fallbackFont.atlasTextures[0]
                : null;
            if (atlasTexture == null || fallbackFont.material == null)
            {
                Debug.LogError("[AddCheckmarkGlyph] Generated fallback font asset is missing atlas texture or material.");
                Object.DestroyImmediate(fallbackFont);
                return;
            }

            atlasTexture.name = "CheckmarkFallback SDF Atlas";
            atlasTexture.Apply(false, false);
            fallbackFont.material.name = "CheckmarkFallback SDF Material";

            AssetDatabase.CreateAsset(fallbackFont, FallbackFontAssetPath);
            AssetDatabase.AddObjectToAsset(atlasTexture, fallbackFont);
            AssetDatabase.AddObjectToAsset(fallbackFont.material, fallbackFont);

            SerializedObject fallbackSo = new SerializedObject(fallbackFont);
            ClearStringProperty(fallbackSo, "m_SourceFontFilePath");
            ClearStringProperty(fallbackSo, "m_SourceFontFileGUID");
            ClearObjectProperty(fallbackSo, "m_SourceFontFile");
            ClearObjectProperty(fallbackSo, "m_SourceFontFile_EditorRef");
            SerializedProperty clearDynamicProp = fallbackSo.FindProperty("m_ClearDynamicDataOnBuild");
            if (clearDynamicProp != null)
                clearDynamicProp.boolValue = false;
            fallbackSo.ApplyModifiedPropertiesWithoutUndo();

            if (mainFont.fallbackFontAssetTable == null)
                mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            mainFont.fallbackFontAssetTable.RemoveAll(item => item == null);
            if (!mainFont.fallbackFontAssetTable.Contains(fallbackFont))
                mainFont.fallbackFontAssetTable.Add(fallbackFont);

            fallbackFont.ReadFontAssetDefinition();
            mainFont.ReadFontAssetDefinition();

            EditorUtility.SetDirty(fallbackFont);
            EditorUtility.SetDirty(mainFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            bool fallbackHas = fallbackFont.HasCharacter((int)CheckmarkUnicode);
            bool mainHasWithFallbacks = mainFont.HasCharacter((char)CheckmarkUnicode, searchFallbacks: true);
            Debug.Log(
                $"[AddCheckmarkGlyph] Success: fallback HasCharacter(0x2713)={fallbackHas}, main HasCharacter(0x2713, searchFallbacks:true)={mainHasWithFallbacks}, asset='{FallbackFontAssetPath}'.");
        }

        static void ClearStringProperty(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
                prop.stringValue = string.Empty;
        }

        static void ClearObjectProperty(SerializedObject serializedObject, string propertyName)
        {
            SerializedProperty prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
                prop.objectReferenceValue = null;
        }
    }
}
