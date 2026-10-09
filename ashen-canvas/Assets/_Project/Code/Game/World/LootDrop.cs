using AshenCanvas.Game.Art;
using AshenCanvas.Sim.Items;
using UnityEngine;

namespace AshenCanvas.Game.World
{
    /// <summary>Выпавший лут: подпрыгивает при появлении, подписан цветом редкости.</summary>
    public sealed class LootDrop : MonoBehaviour
    {
        public Drop Drop;
        Vector3 from, to;
        float t;
        Transform model;

        public string Label
        {
            get
            {
                switch (Drop.Kind)
                {
                    case DropKind.Gold: return Drop.Amount + " золота";
                    case DropKind.Potion: return "Зелье здоровья";
                    default: return Drop.Item.name;
                }
            }
        }

        public Color LabelColor => Drop.Kind == DropKind.Gold ? Mats.Hex(0xFFC928)
            : Drop.Kind == DropKind.Potion ? Mats.Hex(0xFF7A8A)
            : Mats.Hex(Item.RarityColor(Drop.Item.Rarity));

        public static LootDrop Spawn(Drop d, Vector3 origin, Vector3 landing, Transform parent)
        {
            string asset = d.Kind == DropKind.Gold ? "loot_gold" : d.Kind == DropKind.Potion ? "loot_potion" : d.Item.Base.AssetId;
            var go = new GameObject("Loot");
            go.transform.SetParent(parent, false);
            var drop = go.AddComponent<LootDrop>();
            drop.Drop = d;
            drop.from = origin;
            drop.to = landing;
            drop.model = AssetProvider.Spawn(asset, go.transform).transform;
            go.transform.position = origin;

            if (d.Kind == DropKind.Item && d.Item.Rarity != Rarity.Normal)
            {
                var beam = PlaceholderFactory.P(go.transform, PrimitiveType.Cylinder, new Vector3(0, 2.5f, 0), new Vector3(0.12f, 2.5f, 0.12f), Color.white);
                beam.GetComponent<Renderer>().sharedMaterial = Mats.Unlit(Mats.Hex(Item.RarityColor(d.Item.Rarity), 0.55f));
                beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return drop;
        }

        void Update()
        {
            if (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.deltaTime * 2.2f);
                var p = Vector3.Lerp(from, to, t);
                p.y = Mathf.Sin(t * Mathf.PI) * 1.8f;
                transform.position = p;
            }
            if (model != null) model.Rotate(0f, 90f * Time.deltaTime, 0f);
        }
    }
}
