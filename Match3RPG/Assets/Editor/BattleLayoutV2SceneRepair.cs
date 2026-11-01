using Match3RPG.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Match3RPG.Editor
{
    public static class BattleLayoutV2SceneRepair
    {
        const string LayoutAssetPath = "Assets/Art/Battle/Layouts/BattleLayout_V2/BattleLayout_V2.png";
        const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Match3RPG/Repair/Battle Layout V2")]
        public static void RepairBattleLayoutV2SceneReference()
        {
            Sprite layoutSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LayoutAssetPath);
            if (layoutSprite == null)
            {
                Debug.LogError($"[BattleLayoutRepair] Could not load Sprite from {LayoutAssetPath}.");
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(GameScenePath);
            bool openedHere = !scene.IsValid() || !scene.isLoaded;
            if (openedHere)
                scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);

            bool changed = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (GameBootstrap bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
            {
                SerializedObject serialized = new SerializedObject(bootstrap);
                SerializedProperty spriteProperty = serialized.FindProperty("battleLayoutV2Sprite");
                if (spriteProperty == null)
                {
                    Debug.LogError("[BattleLayoutRepair] GameBootstrap.battleLayoutV2Sprite property was not found on the active bootstrap.", bootstrap);
                    continue;
                }

                if (spriteProperty.objectReferenceValue != layoutSprite)
                {
                    spriteProperty.objectReferenceValue = layoutSprite;
                    serialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(bootstrap);
                    EditorUtility.SetDirty(bootstrap.gameObject);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[BattleLayoutRepair] BattleLayout_V2 assigned to GameBootstrap in {GameScenePath}.");
            }
            else
            {
                Debug.Log("[BattleLayoutRepair] BattleLayout_V2 reference already assigned on the active GameBootstrap.");
            }

            if (openedHere)
                EditorSceneManager.CloseScene(scene, true);
        }
    }
}
