using System;
using UnityEngine;

namespace AshenCanvas.Game.World
{
    /// <summary>То, с чем говорят или что используют: NPC, порталы, мольберт путей.</summary>
    public sealed class Interactable : MonoBehaviour
    {
        public string Label;
        public float Radius = 3.2f;
        public Action OnUse;

        Transform body, swirl;

        void Start()
        {
            var model = transform.Find("Model");
            if (model == null) return;
            body = Art.AssetProvider.FindDeep(model, "Body");
            swirl = Art.AssetProvider.FindDeep(model, "Swirl");
        }

        void Update()
        {
            // Портал крутится, NPC дышат — мир живой.
            if (swirl != null) swirl.Rotate(0f, 140f * Time.deltaTime, 0f, Space.Self);
            if (body != null) body.localScale = new Vector3(1f, 1f + Mathf.Sin(Time.time * 2f + transform.position.x) * 0.02f, 1f);
        }
    }
}
