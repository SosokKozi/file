using System.IO;
using AshenCanvas.Game.Boot;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AshenCanvas.EditorTools
{
    /// <summary>Меню «Ashen Canvas» в редакторе: сцена запуска, сохранения, каталог ассетов.</summary>
    public static class ProjectMenu
    {
        const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        [MenuItem("Ashen Canvas/Создать сцену Boot")]
        public static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            new GameObject("GameRoot").AddComponent<GameRoot>();
            Directory.CreateDirectory(Path.GetDirectoryName(BootScenePath));
            EditorSceneManager.SaveScene(scene, BootScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootScenePath, true) };
            Debug.Log("Сцена Boot создана и добавлена в сборку: " + BootScenePath);
        }

        [MenuItem("Ashen Canvas/Удалить сохранение")]
        public static void DeleteSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "ashen_canvas_save.json");
            if (File.Exists(path)) File.Delete(path);
            Debug.Log("Сохранение удалено: " + path);
        }

        [MenuItem("Ashen Canvas/Открыть папку сохранений")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        [MenuItem("Ashen Canvas/Каталог ассетов: что уже заменено")]
        public static void ReportAssets()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_Project/Art/asset_catalog.json");
            if (json == null) { Debug.LogWarning("Нет Assets/_Project/Art/asset_catalog.json"); return; }
            var catalog = JsonUtility.FromJson<AssetCatalog>(json.text);
            int real = 0;
            foreach (var a in catalog.assets)
            {
                bool has = Game.Art.AssetProvider.HasRealModel(a.id);
                if (has) real++;
                Debug.Log((has ? "[модель]   " : "[болванка] ") + a.id + " — " + a.name);
            }
            Debug.Log("Настоящих моделей: " + real + " из " + catalog.assets.Length);
        }

        [System.Serializable]
        class AssetCatalog { public AssetEntry[] assets = new AssetEntry[0]; }

        [System.Serializable]
        class AssetEntry { public string id, name, prompt, source; }
    }
}
