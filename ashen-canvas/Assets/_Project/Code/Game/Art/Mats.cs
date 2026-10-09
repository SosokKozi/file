using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AshenCanvas.Game.Art
{
    /// <summary>Цвета и материалы, созданные кодом. Работает и в URP, и во встроенном конвейере.</summary>
    public static class Mats
    {
        static Shader lit, unlit;
        static readonly Dictionary<int, Material> LitCache = new Dictionary<int, Material>();
        static readonly Dictionary<int, Material> UnlitCache = new Dictionary<int, Material>();
        static Texture2D circle;

        /// <summary>Яркие «краски» для брызг и взрывов.</summary>
        public static readonly Color[] Paint =
        {
            Hex(0xFF3B4A), Hex(0xFF8A2A), Hex(0xFFC928), Hex(0x6BE36B), Hex(0x2FB8FF), Hex(0x8A3CFF), Hex(0xFF5FA2),
        };

        public static Color Hex(uint rgb, float a = 1f)
            => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        public static Color RandomPaint() => Paint[Random.Range(0, Paint.Length)]; // косметика: Random допустим

        static Shader LitShader
        {
            get
            {
                if (lit != null) return lit;
                if (GraphicsSettings.currentRenderPipeline != null)
                {
                    lit = Shader.Find("Universal Render Pipeline/Simple Lit");
                    if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
                }
                if (lit == null) lit = Shader.Find("Standard");
                return lit;
            }
        }

        static Shader UnlitShader
        {
            get
            {
                if (unlit == null) unlit = Shader.Find("Sprites/Default");
                return unlit;
            }
        }

        static int Key(Color c)
        {
            Color32 k = c;
            return (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        }

        /// <summary>Общий матовый материал цвета. Не менять — он общий.</summary>
        public static Material Lit(Color c)
        {
            int key = Key(c);
            if (LitCache.TryGetValue(key, out var m) && m != null) return m;
            m = NewLit(c);
            LitCache[key] = m;
            return m;
        }

        /// <summary>Свой экземпляр матового материала — для частей, меняющих цвет.</summary>
        public static Material NewLit(Color c)
        {
            var m = new Material(LitShader) { color = c };
            m.SetFloat("_Smoothness", 0.05f);
            m.SetFloat("_Glossiness", 0.05f);
            return m;
        }

        /// <summary>Общий неосвещённый материал (свечение, лучи лута).</summary>
        public static Material Unlit(Color c)
        {
            int key = Key(c);
            if (UnlitCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(UnlitShader) { color = c };
            UnlitCache[key] = m;
            return m;
        }

        /// <summary>Свой неосвещённый прозрачный материал — для затухающих эффектов.</summary>
        public static Material NewUnlit(Color c, bool round = false)
        {
            var m = new Material(UnlitShader) { color = c };
            if (round) m.mainTexture = Circle;
            return m;
        }

        /// <summary>Мягкий круг: капли, кляксы, круги предупреждений.</summary>
        public static Texture2D Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 64;
                circle = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((1f - d) * 10f);
                        px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                    }
                circle.SetPixels32(px);
                circle.Apply();
                return circle;
            }
        }
    }
}
