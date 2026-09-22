using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using LittleEmber.CameraWork;
using LittleEmber.Combat;
using LittleEmber.Player;
using LittleEmber.UI;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Emberholt_Greybox.unity programmatically:
    /// portrait camera rig, baked-texture ground (tilemaps misbehave in batchmode
    /// editor scripts here, so grass+paths are painted into one sprite),
    /// walls, pack behavior prefabs (chest / push-block / tall grass / torches),
    /// training dummies, Pip with lantern Light2D, and the portrait touch HUD.
    /// Generated textures are greybox placeholders — real village art is M1.
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Emberholt_Greybox.unity";
        const string ArtRoot = "Assets/Art/Greybox";
        const string UiRoot = "Assets/Art/UI";
        const string AnimatorPath = "Assets/Art/Animations/Hero/Pip.controller";
        const string PackRoot = "Assets/Gif/Super_Retro_Collection/Resources";

        // map extents in world units (1 unit = 1 tile = 16 px)
        static readonly Vector2 MapMin = new Vector2(-18, -28);
        static readonly Vector2 MapMax = new Vector2(18, 28);

        [MenuItem("LittleEmber/M0/2 - Build Emberholt Greybox Scene")]
        public static void Build()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (controller == null)
            {
                Debug.LogError("[Greybox] Pip.controller missing — run 'LittleEmber/M0/1 - Generate Hero Animations' first.");
                return;
            }

            // ---------- generated sprites ----------
            var groundSprite = MakeSprite("gb_ground", GroundPixels(), 36 * 16, 56 * 16, 16);
            var blockSprite = MakeSprite("gb_block", BlockPixels(), 16, 16, 16);
            var ringSprite = MakeUiSprite("ui_ring", RingPixels(256, 12));
            var knobSprite = MakeUiSprite("ui_knob", DiscPixels(128));
            var buttonSprite = MakeUiSprite("ui_button", ButtonPixels(256));
            var heartFull = MakeUiSprite("ui_heart_full", HeartPixels(64, new Color32(0xE8, 0x4A, 0x4A, 255)));
            var heartEmpty = MakeUiSprite("ui_heart_empty", HeartPixels(64, new Color32(0x44, 0x4A, 0x58, 255)));
            if (groundSprite == null || blockSprite == null)
            {
                Debug.LogError("[Greybox] Generated sprites failed to load — aborting scene build.");
                return;
            }

            // ---------- scene ----------
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f; // 18u vertical @ PPU16 — portrait Zelda framing
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
            camGo.transform.position = new Vector3(0, -20, -10);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.useBounds = true;
            follow.minBounds = MapMin;
            follow.maxBounds = MapMax;
            cam.GetUniversalAdditionalCameraData(); // ensure URP camera component

            // global light (URP 2D)
            var globalLightGo = new GameObject("Global Light 2D");
            var globalLight = globalLightGo.AddComponent<Light2D>();
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.color = new Color(1f, 0.96f, 0.90f);
            globalLight.intensity = 0.92f;

            // ground: one baked sprite (36x56 units), grass + curved paths painted in
            var groundGo = new GameObject("Ground");
            groundGo.transform.position = new Vector3((MapMin.x + MapMax.x) * 0.5f, (MapMin.y + MapMax.y) * 0.5f, 0);
            var groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.sprite = groundSprite;
            groundSr.sortingOrder = -10;

            // border walls
            var walls = new GameObject("Walls");
            AddWall(walls.transform, new Vector2(0, MapMax.y + 0.5f), new Vector2(MapMax.x - MapMin.x + 2, 1));
            AddWall(walls.transform, new Vector2(0, MapMin.y - 0.5f), new Vector2(MapMax.x - MapMin.x + 2, 1));
            AddWall(walls.transform, new Vector2(MapMin.x - 0.5f, 0), new Vector2(1, MapMax.y - MapMin.y + 2));
            AddWall(walls.transform, new Vector2(MapMax.x + 0.5f, 0), new Vector2(1, MapMax.y - MapMin.y + 2));

            // greybox houses (odd clusters, off-axis per composition laws)
            AddHouse(blockSprite, new Vector2(-10, 10), new Vector2(6, 4));
            AddHouse(blockSprite, new Vector2(9, 12), new Vector2(5, 4));
            AddHouse(blockSprite, new Vector2(-9, -12), new Vector2(5, 4));
            AddHouse(blockSprite, new Vector2(11, -16), new Vector2(4, 3));

            // pack behavior prefabs — the tutorialized interactions
            PlacePrefab($"{PackRoot}/Prefabs_with_behavior/chest_open_on_contact/chest_open_on_contact.prefab", new Vector3(6, 6, 0));
            PlacePrefab($"{PackRoot}/Prefabs_with_behavior/block_push_on_contact/block_01_push_on_contact.prefab", new Vector3(-5, 2, 0));
            for (int gx = 10; gx <= 13; gx++)
                for (int gy = -7; gy <= -4; gy++)
                    PlacePrefab($"{PackRoot}/Prefabs_with_behavior/tall_grass_react_on_contact/tall_grass_react_on_contact.prefab", new Vector3(gx, gy, 0));

            // torches with warm halos flanking the main street + plaza + one near spawn
            foreach (var pos in new[] { new Vector2(-3, 5), new Vector2(4, 5), new Vector2(-3, -7), new Vector2(4, -7), new Vector2(0, 13), new Vector2(2, -18) })
            {
                var torch = PlacePrefab($"{PackRoot}/Prefabs/Torches/Torch_01.prefab", new Vector3(pos.x, pos.y, 0));
                if (torch != null)
                {
                    var l = torch.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.color = new Color(1f, 0.68f, 0.38f);
                    l.intensity = 0.9f;
                    l.pointLightInnerRadius = 1.2f;
                    l.pointLightOuterRadius = 3.5f;
                }
            }

            // training dummies (sword-combo targets)
            var dummySprite = FindDummySprite();
            AddDummy(dummySprite, new Vector2(10, 16));
            AddDummy(dummySprite, new Vector2(13, 16));

            // ---------- Pip ----------
            var pip = new GameObject("Pip");
            pip.transform.position = new Vector3(0, -22, 0);
            var rb = pip.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var col = pip.AddComponent<CapsuleCollider2D>();
            col.direction = CapsuleDirection2D.Horizontal;
            col.offset = new Vector2(0, -0.72f);
            col.size = new Vector2(0.75f, 0.4f);
            var sr = pip.AddComponent<SpriteRenderer>();
            sr.sprite = FindHeroIdleSprite();
            sr.sortingOrder = 10;
            var anim = pip.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            var pipHealth = pip.AddComponent<Health>();
            pipHealth.maxHearts = 3;
            var pipCtl = pip.AddComponent<PipController>();
            var colorSwap = pip.AddComponent<HeroColorSwap>();
            colorSwap.colors = new[]
            {
                controller,
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimatorPath}/../Pip_color_2.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimatorPath}/../Pip_color_3.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimatorPath}/../Pip_color_4.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimatorPath}/../Pip_color_5.overrideController"),
            };

            // lantern (child, warm flicker — the Little Ember itself)
            var lanternGo = new GameObject("Lantern");
            lanternGo.transform.SetParent(pip.transform, false);
            lanternGo.transform.localPosition = new Vector3(0, 0.3f, 0);
            var lanternLight = lanternGo.AddComponent<Light2D>();
            lanternLight.lightType = Light2D.LightType.Point;
            lanternLight.color = new Color(1f, 0.76f, 0.47f);
            lanternLight.intensity = 1.15f;
            lanternLight.pointLightInnerRadius = 2.0f;
            lanternLight.pointLightOuterRadius = 5.5f;
            var lantern = pip.AddComponent<LanternLight>();
            lantern.lantern = lanternLight;
            lantern.hpSource = pipHealth;

            follow.target = pip.transform;

            // ---------- portrait HUD (luxury set: gem buttons, ornate joystick, hints) ----------
            HudBuilder.Build(pipHealth, pipCtl, "EMBERHOLT · TRAINING GROUNDS", true);

            // ---------- save + register ----------
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            // register at the END if missing — ShellSceneBuilder owns the final
            // order (Splash first); never hijack index 0 or the game boots here.
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Greybox] Scene built and saved to {ScenePath}.");
        }

        // ------------------------------------------------------------------ world pieces

        static int CurveOffset(int y) => (int)(Mathf.Sin(y * 0.11f) * 1.6f); // no straight road longer than 6 tiles

        static bool IsPath(int wx, int wy)
        {
            // vertical main street with a gentle curve, south edge up to the plaza
            if (wy < 14 && wy >= (int)MapMin.y)
            {
                int cx = CurveOffset(wy);
                if (wx >= cx - 2 && wx <= cx + 1) return true;
            }
            // horizontal plaza road
            if (wy >= 1 && wy <= 3 && wx >= -14 && wx <= 14) return true;
            return false;
        }

        static void AddWall(Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject($"Wall_{parent.childCount}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
        }

        static void AddHouse(Sprite blockSprite, Vector2 center, Vector2 size)
        {
            var go = new GameObject("House");
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = blockSprite;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = 2;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
        }

        static void AddDummy(Sprite sprite, Vector2 pos)
        {
            var go = new GameObject("TrainingDummy");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = new Vector2(0.8f, 1.0f);
            b.offset = new Vector2(0, -0.2f);
            go.AddComponent<DummyTarget>();
        }

        static GameObject PlacePrefab(string path, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning($"[Greybox] Prefab not found: {path}"); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
            return go;
        }

        static Sprite FindHeroIdleSprite()
        {
            const string p = PackRoot + "/Hero/hero/color_1/idle/hero_idle_DOWN.png";
            var spr = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().FirstOrDefault();
            if (spr == null) Debug.LogWarning("[Greybox] hero_idle_DOWN sprite not found.");
            return spr;
        }

        static Sprite FindDummySprite()
        {
            const string folder = PackRoot + "/Hero/dummy/idle";
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            Array.Sort(guids);
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var spr = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (spr != null) return spr;
            }
            Debug.LogWarning("[Greybox] No dummy sprite found — dummies will be blank.");
            return null;
        }

        // ------------------------------------------------------------------ generated art (greybox placeholders)

        static Sprite MakeSprite(string name, Color32[] pixels, int w, int h, int ppu)
        {
            EnsureFolder(ArtRoot);
            return WriteSprite($"{ArtRoot}/{name}.png", pixels, w, h, ppu);
        }

        static Sprite MakeUiSprite(string name, Color32[] pixels)
        {
            EnsureFolder(UiRoot);
            int side = (int)Mathf.Sqrt(pixels.Length);
            return WriteSprite($"{UiRoot}/{name}.png", pixels, side, side, 100);
        }

        static Sprite WriteSprite(string path, Color32[] pixels, int w, int h, int ppu)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static Color32[] GroundPixels()
        {
            const int w = 36 * 16, h = 56 * 16;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int tx = x >> 4, ty = y >> 4;                 // tile coords 0..35, 0..55
                    int wx = tx + (int)MapMin.x, wy = ty + (int)MapMin.y; // world tile coords
                    int n = (int)(((uint)(x * 73856093) ^ (uint)(y * 19349663)) & 0xffff);

                    Color32 c;
                    if (IsPath(wx, wy))
                    {
                        byte v = (byte)(128 + n % 18 - 9);
                        c = new Color32(v, (byte)(v - 22), (byte)(v - 51), 255); // dirt
                        if (n % 31 == 0) c = new Color32(160, 140, 105, 255);    // pebble
                    }
                    else
                    {
                        byte v = (byte)(118 + n % 26 - 13);
                        c = new Color32((byte)(v - 52), v, (byte)(v - 46), 255); // grass
                        if (n % 97 == 0) c = new Color32(230, 220, 120, 255);    // rare flower
                        else if (n % 12 == 0) c = new Color32(86, 160, 80, 255); // light blade
                    }

                    // subtle edge vignette so map bounds read at a glance
                    int edge = Mathf.Min(tx, ty, 35 - tx, 55 - ty);
                    if (edge < 2)
                    {
                        float f = edge == 0 ? 0.72f : 0.88f;
                        c = new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), 255);
                    }
                    px[y * w + x] = c;
                }
            }
            return px;
        }

        static Color32[] BlockPixels()
        {
            var px = new Color32[256];
            for (int i = 0; i < 256; i++)
            {
                int x = i % 16, y = i / 16;
                bool border = x == 0 || y == 0 || x == 15 || y == 15;
                px[i] = border ? new Color32(70, 52, 38, 255) : new Color32(96, 72, 54, 255);
            }
            return px;
        }

        static Color32[] RingPixels(int side, int stroke)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, r = side * 0.46f;
            for (int i = 0; i < px.Length; i++)
            {
                int x = i % side, y = i / side;
                float d = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) - r);
                float a = Mathf.Clamp01(stroke * 0.5f - d) * 0.9f;
                px[i] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            return px;
        }

        static Color32[] DiscPixels(int side)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, r = side * 0.46f;
            for (int i = 0; i < px.Length; i++)
            {
                int x = i % side, y = i / side;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(r - d);
                px[i] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            return px;
        }

        static Color32[] ButtonPixels(int side)
        {
            var px = new Color32[side * side];
            float c = (side - 1) * 0.5f, r = side * 0.46f;
            for (int i = 0; i < px.Length; i++)
            {
                int x = i % side, y = i / side;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = Mathf.Clamp01(r - d);
                byte v = d > r - side * 0.06f ? (byte)255 : (byte)235; // subtle rim
                px[i] = new Color32(v, v, v, (byte)(a * 255));
            }
            return px;
        }

        static Color32[] HeartPixels(int side, Color32 fill)
        {
            var px = new Color32[side * side];
            var outline = new Color32(0x2A, 0x14, 0x18, 255);
            bool[,] inside = new bool[side, side];
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    float nx = (x - side * 0.5f + 0.5f) / (side * 0.32f);
                    float ny = (y - side * 0.44f) / (side * 0.32f);
                    float f = (nx * nx + ny * ny - 1f);
                    inside[x, y] = f * f * f - nx * nx * ny * ny * ny <= 0f;
                }
            for (int y = 0; y < side; y++)
                for (int x = 0; x < side; x++)
                {
                    if (!inside[x, y]) { px[y * side + x] = new Color32(0, 0, 0, 0); continue; }
                    bool edge = x == 0 || y == 0 || x == side - 1 || y == side - 1
                        || !inside[x - 1, y] || !inside[x + 1, y] || !inside[x, y - 1] || !inside[x, y + 1];
                    px[y * side + x] = edge ? outline : fill;
                }
            return px;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
