using System.IO;
using UnityEditor;
using UnityEngine;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Shared procedural UI sprite bakery (Assets/Art/UI): rounded-rect panels
    /// with 9-slice borders, vertical gradients, soft glow discs. Used by the
    /// greybox builder (hint card) and the shell scene builder (menus).
    /// </summary>
    public static class UiSpriteGen
    {
        public const string UiRoot = "Assets/Art/UI";

        /// <summary>Rounded-rect sprite; 9-slice border defaults to radius+6.</summary>
        public static Sprite RoundedRect(string name, int side, int radius, Color32 fill, int borderPx = 0)
        {
            var px = RoundedRectPixels(side, radius, fill);
            int border = borderPx > 0 ? borderPx : radius + 6;
            return Write(name, px, side, side, new Vector4(border, border, border, border));
        }

        public static Color32[] RoundedRectPixels(int side, int radius, Color32 fill)
        {
            var px = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = Mathf.Max(Mathf.Max(radius - x, x - (side - 1 - radius)), 0);
                    float dy = Mathf.Max(Mathf.Max(radius - y, y - (side - 1 - radius)), 0);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * side + x] = new Color32(fill.r, fill.g, fill.b, (byte)(fill.a * a));
                }
            return px;
        }

        /// <summary>Vertical gradient (bottom color at y=0, top color at y=h-1).</summary>
        public static Sprite GradientV(string name, int w, int h, Color32 bottom, Color32 top)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                var c = Color32.Lerp(bottom, top, (float)y / (h - 1));
                for (int x = 0; x < w; x++) px[y * w + x] = c;
            }
            return Write(name, px, w, h, Vector4.zero);
        }

        /// <summary>Soft radial glow (white, quadratic falloff) — particle dots, vignettes.</summary>
        public static Sprite SoftDisc(string name, int side)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, r = side * 0.48f;
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    px[y * side + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            return Write(name, px, side, side, Vector4.zero);
        }

        // ============================================================ luxury set

        static float SdfRoundRect(float x, float y, int w, int h, float r)
        {
            float qx = Mathf.Abs(x - (w - 1) * 0.5f) - ((w - 1) * 0.5f - r);
            float qy = Mathf.Abs(y - (h - 1) * 0.5f) - ((h - 1) * 0.5f - r);
            float ax = Mathf.Max(qx, 0f), ay = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static Color GoldGradient(float t) // 0 = dark bronze, 1 = light gold
        {
            var dark = new Color(0.42f, 0.27f, 0.09f);
            var mid = new Color(0.85f, 0.62f, 0.22f);
            var lite = new Color(1.00f, 0.88f, 0.52f);
            return t < 0.5f ? Color.Lerp(dark, mid, t * 2f) : Color.Lerp(mid, lite, (t - 0.5f) * 2f);
        }

        static Color32 Over(Color32 dst, Color src) // src-over alpha blend
        {
            float a = Mathf.Clamp01(src.a);
            float da = dst.a / 255f;
            float outA = a + da * (1f - a);
            if (outA <= 0f) return new Color32(0, 0, 0, 0);
            byte Mix(byte d, float s) => (byte)(Mathf.Clamp01((s * a + (d / 255f) * da * (1f - a)) / outA) * 255f);
            return new Color32(Mix(dst.r, src.r), Mix(dst.g, src.g), Mix(dst.b, src.b), (byte)(outA * 255f));
        }

        /// <summary>Round gem button: glow halo, gold rim, glassy face, inner shadow. ~128px.</summary>
        public static Sprite GemButton(string name, int side, Color baseColor)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, R = side * 0.42f;
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / R; // 1 = rim edge
                    var col = new Color32(0, 0, 0, 0);

                    if (d > 1f) // warm halo
                    {
                        float halo = Mathf.Clamp01(1f - (d - 1f) / 0.22f);
                        halo *= halo;
                        col = Over(col, new Color(1f, 0.72f, 0.34f, halo * 0.42f));
                    }
                    else
                    {
                        // gold rim (0.90..1.00) with diagonal light
                        if (d >= 0.90f)
                        {
                            float t = Mathf.Clamp01(0.5f + 0.5f * (dy - dx) / (R * 1.2f));
                            var gold = GoldGradient(t);
                            float edge = d >= 0.985f ? Mathf.Clamp01((1f - d) / 0.015f) : 1f;
                            col = Over(col, new Color(gold.r, gold.g, gold.b, edge));
                        }
                        if (d < 0.90f)
                        {
                            // gem face: radial falloff + top glass highlight + bottom shadow
                            float face = Mathf.Clamp01(d / 0.90f);
                            var c0 = baseColor * 1.30f; c0.a = 1f;
                            var c1 = baseColor * 0.52f; c1.a = 1f;
                            var faceC = Color.Lerp(c0, c1, Mathf.SmoothStep(0f, 1f, face));
                            float aa = Mathf.Clamp01((0.90f - d) / 0.02f);
                            col = Over(col, new Color(faceC.r, faceC.g, faceC.b, aa));

                            float high = Mathf.Clamp01(1f - face * 1.6f) * Mathf.Clamp01(dy / (R * 0.55f));
                            col = Over(col, new Color(1f, 1f, 1f, high * 0.30f));

                            float shade = Mathf.Clamp01((face - 0.62f) / 0.3f) * Mathf.Clamp01(-dy / (R * 0.4f));
                            col = Over(col, new Color(0f, 0f, 0f, shade * 0.28f));
                        }
                        // dark hairline between rim and face
                        if (d >= 0.885f && d < 0.90f) col = Over(col, new Color(0f, 0f, 0f, 0.5f));
                    }
                    px[y * side + x] = col;
                }
            return Write(name, px, side, side, Vector4.zero);
        }

        /// <summary>Ornate joystick base: faint dark disc, gold ring, diagonal tick marks.</summary>
        public static Sprite OrnateRing(string name, int side)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, R = side * 0.46f;
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float d = dist / R;
                    var col = new Color32(0, 0, 0, 0);

                    if (d < 1f) col = Over(col, new Color(0.03f, 0.045f, 0.09f, 0.34f * Mathf.Clamp01((1f - d) / 0.05f)));
                    if (d >= 0.90f && d <= 1f) // gold ring
                    {
                        float t = Mathf.Clamp01(0.5f + 0.5f * (dy - dx) / (R * 1.2f));
                        var gold = GoldGradient(t);
                        float ring = Mathf.Clamp01(Mathf.Min(d - 0.90f, 1f - d) / 0.018f);
                        col = Over(col, new Color(gold.r, gold.g, gold.b, ring * 0.95f));
                    }
                    if (Mathf.Abs(d - 0.855f) < 0.008f) col = Over(col, new Color(1f, 1f, 1f, 0.10f)); // hairline
                    if (d >= 1f && d < 1.10f) // outer glow
                    {
                        float halo = 1f - (d - 1f) / 0.10f;
                        col = Over(col, new Color(1f, 0.72f, 0.34f, halo * halo * 0.22f));
                    }
                    // 4 diagonal gold ticks
                    if (d > 0.70f && d < 0.80f)
                    {
                        float ang = Mathf.Atan2(dy, dx);
                        float nearest = Mathf.Round(ang / (Mathf.PI / 2f) + 0.5f) * (Mathf.PI / 2f) - 0.5f * (Mathf.PI / 2f);
                        float dd = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, nearest * Mathf.Rad2Deg));
                        if (dd < 3.2f) col = Over(col, new Color(0.95f, 0.78f, 0.42f, 0.75f));
                    }
                    px[y * side + x] = col;
                }
            return Write(name, px, side, side, Vector4.zero);
        }

        /// <summary>Gem joystick knob: amber core, gold rim, top highlight.</summary>
        public static Sprite GemKnob(string name, int side)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, R = side * 0.44f;
            var amber = new Color(0.95f, 0.55f, 0.16f);
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / R;
                    var col = new Color32(0, 0, 0, 0);
                    if (d > 1f)
                    {
                        float halo = Mathf.Clamp01(1f - (d - 1f) / 0.25f);
                        col = Over(col, new Color(1f, 0.7f, 0.3f, halo * halo * 0.5f));
                    }
                    else
                    {
                        if (d >= 0.86f)
                        {
                            float t = Mathf.Clamp01(0.5f + 0.5f * (dy - dx) / (R * 1.2f));
                            var gold = GoldGradient(t);
                            col = Over(col, new Color(gold.r, gold.g, gold.b, Mathf.Clamp01((1f - d) / 0.02f + 0.6f)));
                        }
                        if (d < 0.86f)
                        {
                            float face = d / 0.86f;
                            var faceC = Color.Lerp(new Color(1f, 0.85f, 0.45f), amber * 0.7f, Mathf.SmoothStep(0f, 1f, face));
                            col = Over(col, new Color(faceC.r, faceC.g, faceC.b, Mathf.Clamp01((0.86f - d) / 0.03f)));
                            float hx = dx + R * 0.28f, hy = dy - R * 0.30f;
                            float hd = Mathf.Sqrt(hx * hx + hy * hy) / (R * 0.34f);
                            if (hd < 1f) col = Over(col, new Color(1f, 1f, 1f, (1f - hd) * (1f - hd) * 0.75f));
                        }
                    }
                    px[y * side + x] = col;
                }
            return Write(name, px, side, side, Vector4.zero);
        }

        /// <summary>Dark navy panel with a thin gold frame (9-slice).</summary>
        public static Sprite GoldPanel(string name, int side, int radius, Color32 fill)
        {
            var px = new Color32[side * side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float sd = SdfRoundRect(x, y, side, side, radius);
                    var col = new Color32(0, 0, 0, 0);
                    if (sd <= 0f)
                    {
                        float aa = Mathf.Clamp01(-sd);
                        col = Over(col, new Color(fill.r / 255f, fill.g / 255f, fill.b / 255f, fill.a / 255f * aa));
                        // gold frame ~2px, inset 3px
                        float fs = Mathf.Abs(sd + 4f) - 1.6f;
                        if (fs < 0f)
                        {
                            float t = Mathf.Clamp01((float)y / side * 0.7f + 0.15f);
                            var gold = GoldGradient(t);
                            col = Over(col, new Color(gold.r, gold.g, gold.b, Mathf.Clamp01(-fs) * 0.9f));
                        }
                    }
                    px[y * side + x] = col;
                }
            int b = radius + 8;
            return Write(name, px, side, side, new Vector4(b, b, b, b));
        }

        /// <summary>White glyph sprites (tinted by Image.color): flame / roll swirl / talk bubble.</summary>
        public static Sprite Icon(string name, int side, string kind)
        {
            var px = new Color32[side * side];
            float h = side;
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float a = 0f;
                    if (kind == "flame")
                    {
                        // bold solid flame: wide lower body sweeping to a flicked tip
                        float ny = y / h;                    // 0 bottom .. 1 top
                        float nx = (x - h * 0.5f) / h;       // -0.5..0.5
                        float sway = 0.11f * Mathf.Sin(ny * 2.6f) * ny;   // tip flicks sideways
                        float body = Mathf.Pow(Mathf.Clamp01(1f - ny), 0.62f) * Mathf.Clamp01(ny * 3.4f);
                        float halfW = 0.31f * body;
                        float d = Mathf.Abs(nx - sway) - halfW;
                        a = Mathf.Max(a, Mathf.Clamp01(0.5f - d * h * 0.5f));
                    }
                    else if (kind == "roll")
                    {
                        // bold circular arrow: thick arc + fat arrowhead at the clockwise end
                        float nx = (x - h * 0.5f) / h, ny = (y - h * 0.52f) / h;
                        float r = Mathf.Sqrt(nx * nx + ny * ny);
                        float ang = Mathf.Atan2(ny, nx) * Mathf.Rad2Deg; // -180..180, 0=east CCW
                        // gap centered at -45° (lower right), 70° wide
                        float gapDist = Mathf.Abs(Mathf.DeltaAngle(-45f, ang));
                        float radial = 0.085f - Mathf.Abs(r - 0.30f);
                        if (radial > 0f && gapDist > 35f)
                        {
                            float cap = Mathf.Clamp01((gapDist - 35f) * 0.08f * h); // soft arc ends
                            a = Mathf.Max(a, Mathf.Clamp01(Mathf.Min(radial, cap * 0.02f) * h * 0.5f));
                        }
                        // arrowhead at the -80° arc end, pointing along clockwise tangent
                        float te = -80f * Mathf.Deg2Rad;
                        var E = new Vector2(0.5f * h + Mathf.Cos(te) * 0.30f * h, 0.52f * h + Mathf.Sin(te) * 0.30f * h);
                        var T = new Vector2(Mathf.Sin(te), -Mathf.Cos(te));        // clockwise tangent
                        var P = new Vector2(Mathf.Cos(te), Mathf.Sin(te));         // radial (perp to T)
                        Vector2 tip = E + T * 0.15f * h;
                        Vector2 b1 = E - T * 0.02f * h + P * 0.12f * h;
                        Vector2 b2 = E - T * 0.02f * h - P * 0.12f * h;
                        var p0 = new Vector2(x, y);
                        float s1 = (tip.x - b1.x) * (p0.y - b1.y) - (tip.y - b1.y) * (p0.x - b1.x);
                        float s2 = (b1.x - b2.x) * (p0.y - b2.y) - (b1.y - b2.y) * (p0.x - b2.x);
                        float s3 = (b2.x - tip.x) * (p0.y - tip.y) - (b2.y - tip.y) * (p0.x - tip.x);
                        bool pos = (s1 >= 0f && s2 >= 0f && s3 >= 0f);
                        bool neg = (s1 <= 0f && s2 <= 0f && s3 <= 0f);
                        if (pos || neg) a = Mathf.Max(a, 0.97f);
                    }
                    else if (kind == "talk")
                    {
                        float sd = SdfRoundRect(x, y - (int)(h * 0.14f), side, (int)(h * 0.62f), h * 0.16f);
                        if (sd < 0f) a = 0.96f;
                        // tail
                        if (x > h * 0.28f && x < h * 0.46f)
                        {
                            float t = (x - h * 0.28f) / (h * 0.18f);
                            float top = h * 0.24f, bot = Mathf.Lerp(h * 0.16f, h * 0.02f, t);
                            if (y > bot && y < top) a = 0.96f;
                        }
                        // three dot holes
                        for (int i = 0; i < 3; i++)
                        {
                            float ddx = x - h * (0.32f + i * 0.18f), ddy = y - (h * 0.14f + h * 0.31f);
                            if (ddx * ddx + ddy * ddy < h * h * 0.004f) a = 0f;
                        }
                    }
                    px[y * side + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            return Write(name, px, side, side, Vector4.zero);
        }

        public static Sprite Write(string name, Color32[] px, int w, int h, Vector4 border)
        {
            EnsureFolder(UiRoot);
            string path = $"{UiRoot}/{name}.png";
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            if (border != Vector4.zero) ti.spriteBorder = border;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
