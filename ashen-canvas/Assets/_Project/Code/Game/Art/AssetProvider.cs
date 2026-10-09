using UnityEngine;

namespace AshenCanvas.Game.Art
{
    /// <summary>
    /// Единая точка получения моделей по id из каталога ассетов.
    /// Если в Resources/Models лежит модель с таким именем (glb из Blender или Tripo) — берём её,
    /// иначе собираем болванку из примитивов. Код игры не знает, откуда пришла модель.
    /// Договорённость для моделей: корень у ног, лицом по +Z, рост героини ≈ 1.8 м.
    /// </summary>
    public static class AssetProvider
    {
        public const string ModelsFolder = "Models/";

        public static GameObject Spawn(string assetId, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(ModelsFolder + assetId);
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent);
                go.name = assetId;
                foreach (var c in go.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            }
            else
            {
                go = PlaceholderFactory.Build(assetId);
                go.transform.SetParent(parent, false);
            }
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>Поиск узла по имени на любой глубине: у моделей из glb узлы вложены глубже, чем у болванок.</summary>
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var t = FindDeep(root.GetChild(i), name);
                if (t != null) return t;
            }
            return null;
        }

        public static bool HasRealModel(string assetId) => Resources.Load<GameObject>(ModelsFolder + assetId) != null;
    }
}
