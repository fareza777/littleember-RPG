using System.Collections.Generic;
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
using LittleEmber.World;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/Emberholt_Village.unity — the real prologue town.
    /// Ground is BAKED from the pack's environment atlas (grass/dirt/cobble/water
    /// 16px crops composited into one texture: no tilemap batchmode issues, one
    /// draw call). Structures use pack prefabs (houses/trees/lamps/fires/torii),
    /// festival decor uses atlas sprite lookup by approximate pixel center.
    /// NPCs wander via VillagerNpc, everything depth-sorts via YSort.
    /// </summary>
    public static class VillageSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Emberholt_Village.unity";
        const string PackRoot = "Assets/Gif/Super_Retro_Collection/Resources";
        const string PrefabRoot = PackRoot + "/Prefabs";
        const string AtlasPath = PackRoot + "/Environments/original_atlas.png";
        const string AnimatorPath = "Assets/Art/Animations/Hero/Pip.controller";
        const string BuildArtRoot = "Assets/Art/Build";

        const int MapW = 48;
        const int MapH = 76;
        static readonly Vector2 MapMin = new Vector2(-24, -38);
        static readonly Vector2 MapMax = new Vector2(24, 38);

        // atlas 16px tile picks (image coords, TOP-LEFT origin; Y flipped when sampling)
        // all four verified seamless + magenta-free via Tools/tile_check.ps1
        static readonly Vector2Int GrassPick = new Vector2Int(471, 278);
        static readonly Vector2Int DirtPick = new Vector2Int(378, 282);
        static readonly Vector2Int CobblePick = new Vector2Int(508, 282);
        static readonly Vector2Int WaterPick = new Vector2Int(200, 542);
        const int AtlasH = 1280;
        const int TilePx = 16;

        enum Zone { Grass = 0, Dirt = 1, Cobble = 2, Water = 3 }

        // main street: south gate -> plaza -> north gate
        static readonly Vector2[] Street =
        {
            new Vector2(0, -39), new Vector2(0, -30), new Vector2(1, -26), new Vector2(2, -22),
            new Vector2(0, -18), new Vector2(-2, -14), new Vector2(-1, -8), new Vector2(0, -4),
            new Vector2(0, 0), new Vector2(0, 13), new Vector2(0, 30), new Vector2(0, 39)
        };
        static readonly Vector2[] WestStreet = { new Vector2(-2, -14), new Vector2(-9, -13), new Vector2(-16, -10) };
        static readonly Vector2[] FarmStreet = { new Vector2(0, -18), new Vector2(8, -20), new Vector2(15, -20) };

        [MenuItem("LittleEmber/StageB/1 - Build Emberholt Village Scene")]
        public static void Build()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (controller == null)
            {
                Debug.LogError("[Village] Pip.controller missing — run M0/1 first.");
                return;
            }

            var groundSprite = BakeGround();
            if (groundSprite == null)
            {
                Debug.LogError("[Village] Ground bake failed.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- camera ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.05f, 0.055f, 0.09f);
            camGo.transform.position = new Vector3(0, -31, -10);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.useBounds = true;
            follow.minBounds = MapMin;
            follow.maxBounds = MapMax;
            cam.GetUniversalAdditionalCameraData();

            // ---------- dusk global light (festival eve) ----------
            var glGo = new GameObject("Global Light 2D");
            var gl = glGo.AddComponent<Light2D>();
            gl.lightType = Light2D.LightType.Global;
            gl.color = new Color(0.72f, 0.66f, 0.82f);
            gl.intensity = 0.66f;

            // ---------- ground ----------
            var groundGo = new GameObject("Ground");
            groundGo.transform.position = new Vector3(0, 0, 0);
            var groundSr = groundGo.AddComponent<SpriteRenderer>();
            groundSr.sprite = groundSprite;
            // YSort spans roughly -608..+608 across the map; ground must sit below all of it
            groundSr.sortingOrder = -1000;

            // ---------- border walls ----------
            var walls = new GameObject("Walls");
            AddWall(walls.transform, new Vector2(0, MapMax.y + 0.5f), new Vector2(MapW + 2, 1));
            AddWall(walls.transform, new Vector2(0, MapMin.y - 0.5f), new Vector2(MapW + 2, 1));
            AddWall(walls.transform, new Vector2(MapMin.x - 0.5f, 0), new Vector2(1, MapH + 2));
            AddWall(walls.transform, new Vector2(MapMax.x + 0.5f, 0), new Vector2(1, MapH + 2));

            var world = new GameObject("World");
            var decor = new GameObject("Decor");

            // ---------- gates ----------
            string torii = FirstPrefabIn("Torii");
            PlacePrefab(torii, new Vector3(0, -35, 0));
            PlacePrefab(torii, new Vector3(0, 31, 0));

            // ---------- houses ----------
            PlaceHouse("house_01", new Vector2(-18, -6));   // Elder's house (west street end)
            PlaceHouse("house_02", new Vector2(-11, 4));
            PlaceHouse("house_03", new Vector2(11, 6));
            PlaceHouse("house_04", new Vector2(-12, -19));
            PlaceHouse("house_05", new Vector2(12, -9));
            PlaceHouse("house_06", new Vector2(-7, 20));
            PlaceHouse("house_07", new Vector2(7, 21));
            PlaceHouse("house_08", new Vector2(17, -27));   // farmhouse

            // ---------- festival square ----------
            PlacePrefab($"{PrefabRoot}/Statues/statue_01.prefab", new Vector3(0, 7, 0));
            AddSolid("StatueSolid", new Vector2(0, 7), new Vector2(0.9f, 0.7f), decor.transform);

            var fire = PlacePrefab($"{PrefabRoot}/Fires/Fire_01.prefab", new Vector3(0, 2.5f, 0));
            if (fire != null)
            {
                var fl = fire.AddComponent<Light2D>();
                fl.lightType = Light2D.LightType.Point;
                fl.color = new Color(1f, 0.62f, 0.30f);
                fl.intensity = 1.5f;
                fl.pointLightInnerRadius = 1.4f;
                fl.pointLightOuterRadius = 5.5f;
                fire.AddComponent<FlickerLight2D>().amount = 0.3f;
            }
            AddSolid("BonfireSolid", new Vector2(0, 2.5f), new Vector2(1.1f, 0.8f), decor.transform);

            // market stalls + goods (atlas decor sprites)
            PlaceAtlasDecor(new Vector2(1317, 268), new Vector2(-5, 9), "StallGreen", true, new Vector2(2.4f, 1.1f));
            PlaceAtlasDecor(new Vector2(1374, 268), new Vector2(5, 9), "StallOrange", true, new Vector2(2.4f, 1.1f));
            PlaceAtlasDecor(new Vector2(1320, 350), new Vector2(-6.8f, 8.6f), "Pots", false);
            PlaceAtlasDecor(new Vector2(1300, 315), new Vector2(7, 3.2f), "Bench", true, new Vector2(1.6f, 0.5f));

            // festival string lights across the plaza (two rows)
            for (int i = 0; i < 3; i++)
            {
                PlaceAtlasDecor(new Vector2(1010, 784), new Vector2(-4.4f + i * 4.4f, 11.6f - Mathf.Abs(i - 1) * 0.25f), "StringN" + i, false, default, 60);
                PlaceAtlasDecor(new Vector2(1010, 784), new Vector2(-4.4f + i * 4.4f, 1.6f - Mathf.Abs(i - 1) * 0.25f), "StringS" + i, false, default, 60);
            }

            // crates & barrels clusters
            PlacePrefab($"{PrefabRoot}/Crates/crate_01.prefab", new Vector3(9.5f, 0.6f, 0), true);
            PlacePrefab($"{PrefabRoot}/Barrels/barrel_01.prefab", new Vector3(10.3f, 1.4f, 0), true);
            PlacePrefab($"{PrefabRoot}/Crates/crate_03.prefab", new Vector3(-9.6f, 11.4f, 0), true);
            PlacePrefab($"{PrefabRoot}/Barrels/barrel_02.prefab", new Vector3(-10.4f, 10.5f, 0), true);

            // ---------- lamps along streets (+ warm flickering lights) ----------
            foreach (var p in new[]
            {
                new Vector2(-2.5f, -33.5f), new Vector2(2.5f, -33.5f),
                new Vector2(2.5f, -23), new Vector2(-3.5f, -15), new Vector2(2, -8.5f),
                new Vector2(-6, 1), new Vector2(6, 1), new Vector2(-6, 11), new Vector2(6, 11),
                new Vector2(2, 18), new Vector2(-2, 24),
                new Vector2(-15, -10.5f), new Vector2(10.5f, -19.5f),
            })
            {
                var lamp = PlacePrefab($"{PrefabRoot}/Lamps/lamp_01.prefab", new Vector3(p.x, p.y, 0));
                if (lamp != null)
                {
                    var l = lamp.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.color = new Color(1f, 0.72f, 0.42f);
                    l.intensity = 1.0f;
                    l.pointLightInnerRadius = 0.8f;
                    l.pointLightOuterRadius = 3.4f;
                    lamp.AddComponent<FlickerLight2D>();
                    AddSolid("LampSolid", p, new Vector2(0.35f, 0.35f), decor.transform);
                }
            }

            // ---------- pond rocks ----------
            for (int i = 1; i <= 3; i++)
                PlacePrefab($"{PrefabRoot}/Rocks/rock_0{i}.prefab", new Vector3(11.4f + i * 2.4f, 22.6f + (i % 2), 0), true);
            AddSolid("PondSolidA", new Vector2(15, 25), new Vector2(5.4f, 2.6f), decor.transform);

            // ---------- trees ----------
            var rng = new System.Random(1234);
            for (float x = MapMin.x + 1.5f; x < MapMax.x - 1f; x += 2.5f)
            {
                JitterTree(rng, new Vector2(x, 33.5f));
                JitterTree(rng, new Vector2(x + 1.2f, 36));
            }
            for (float y = -30; y < 31; y += 4f)
            {
                JitterTree(rng, new Vector2(MapMin.x + 1.6f, y));
                JitterTree(rng, new Vector2(MapMax.x - 1.6f, y + 1.7f));
            }
            foreach (var p in new[] { new Vector2(-15, 7), new Vector2(16, 10), new Vector2(-17, -26), new Vector2(8, -31), new Vector2(-6, -28), new Vector2(20, 26), new Vector2(-19, 24) })
                JitterTree(rng, p);

            // ---------- farm fences ----------
            var fenceSprite = AtlasSpriteNear(682, 42, "FenceH");
            if (fenceSprite != null)
            {
                float fenceW = Mathf.Max(0.5f, fenceSprite.rect.width / 16f);
                for (float x = 11; x + fenceW * 0.5f < 21.2f; x += fenceW)
                {
                    if (x > 13f && x < 16.8f) continue; // gate gap where the path enters
                    PlaceDecorRaw(fenceSprite, new Vector2(x + fenceW * 0.5f, -17), "FenceN", false);
                }
                for (float y = -18; y > -25; y -= fenceW)
                    PlaceDecorRaw(fenceSprite, new Vector2(11, y), "FenceW", false);
                AddSolid("FenceSolidA", new Vector2(12.2f, -17), new Vector2(2.6f, 0.4f), decor.transform);
                AddSolid("FenceSolidB", new Vector2(18.9f, -17), new Vector2(4.4f, 0.4f), decor.transform);
                AddSolid("FenceSolidC", new Vector2(11, -21.5f), new Vector2(0.4f, 9), decor.transform);
            }

            // ---------- crops in the field ----------
            for (int i = 0; i < 8; i++)
                PlacePrefab($"{PrefabRoot}/Crops/crop_{(i % 6) + 1:00}.prefab",
                    new Vector3(13 + (i % 4) * 2.2f, -19.5f - (i / 4) * 2.4f, 0), false);

            // ---------- interactive pack bits ----------
            PlacePrefab($"{PackRoot}/Prefabs_with_behavior/chest_open_on_contact/chest_open_on_contact.prefab", new Vector3(-21, -3.5f, 0));
            for (int i = 0; i < 6; i++)
                PlacePrefab($"{PackRoot}/Prefabs_with_behavior/tall_grass_react_on_contact/tall_grass_react_on_contact.prefab",
                    new Vector3(3.5f + (i % 3), 15.5f + (i / 3) * 1.2f, 0));

            // ---------- villagers ----------
            var npcParent = new GameObject("NPCs");
            var vBram = AddVillager(npcParent.transform, "chara_01", new Vector2(3, 4.5f), 3f);
            var vLin = AddVillager(npcParent.transform, "chara_02", new Vector2(-4.5f, 8.5f), 1.4f);
            var vTam = AddVillager(npcParent.transform, "chara_03", new Vector2(-8, -13), 3f);
            var vHobb = AddVillager(npcParent.transform, "chara_01", new Vector2(14, -21), 2.4f);
            var vWren = AddVillager(npcParent.transform, "chara_02", new Vector2(-2, 20), 2.6f);

            // ---------- grass tufts + flowers (atlas overlays) ----------
            var tuft = AtlasSpriteNear(28, 24, "TuftA");
            var tuft2 = AtlasSpriteNear(66, 40, "TuftB");
            var flower = AtlasSpriteNear(30, 222, "FlowerA");
            var rngD = new System.Random(777);
            for (int i = 0; i < 90; i++)
            {
                float x = Mathf.Lerp(MapMin.x + 1, MapMax.x - 1, (float)rngD.NextDouble());
                float y = Mathf.Lerp(MapMin.y + 1, MapMax.y - 1, (float)rngD.NextDouble());
                if (ZoneAt(x, y) != Zone.Grass) continue;
                var s = rngD.NextDouble() < 0.18f ? flower : (rngD.NextDouble() < 0.5f ? tuft : tuft2);
                if (s == null) continue;
                PlaceDecorRaw(s, new Vector2(x, y), "Tuft", false, -90 + 1);
            }

            // ---------- Pip ----------
            var pip = new GameObject("Pip");
            pip.transform.position = new Vector3(0, -31, 0);
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
            var anim = pip.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            var pipHealth = pip.AddComponent<Health>();
            pipHealth.maxHearts = 3;
            var pipCtl = pip.AddComponent<PipController>();
            pip.AddComponent<YSort>();
            var colorSwap = pip.AddComponent<HeroColorSwap>();
            colorSwap.colors = new[]
            {
                controller,
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Hero/Pip_color_2.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Hero/Pip_color_3.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Hero/Pip_color_4.overrideController"),
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Hero/Pip_color_5.overrideController"),
            };

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

            // ---------- HUD + dialogue ----------
            var hud = HudBuilder.Build(pipHealth, pipCtl, "EMBERHOLT · FESTIVAL EVE", true);

            // ---------- house doors (enterable) ----------
            AddDoor(new Vector2(-18, -7.9f), "Interior_Elder", new Vector2(-18, -8.6f));
            AddDoor(new Vector2(-11, 2.1f), "Interior_Home", new Vector2(-11, 1.4f));
            AddDoor(new Vector2(17, -28.9f), "Interior_Home", new Vector2(17, -29.6f));

            // ---------- villager dialogue ----------
            var portraitM = HudBuilder.EnsureUiSprite("Assets/Art/Portraits/portrait_villager_m.png");
            var portraitF = HudBuilder.EnsureUiSprite("Assets/Art/Portraits/portrait_villager_f.png");
            var portraitPip = HudBuilder.EnsureUiSprite("Assets/Art/Portraits/portrait_pip.png");

            MakeTalkable(vBram, hud.dialogue, new[]
            {
                Line("Bram", portraitM, "Evening, Pip! Stalls are stocked, barrels rolled out. All that's left is the Kindling Flame."),
                Line("Pip", portraitPip, "It'll be the brightest one yet, Bram. Wait and see."),
            });
            MakeTalkable(vLin, hud.dialogue, new[]
            {
                Line("Lin", portraitF, "I strung every lantern myself. At dusk the square looks like a sky full of embers, doesn't it?"),
                Line("Lin", portraitF, "Elder Mara was asking after you, by the way. Her door's the big house at the west end."),
            });
            MakeTalkable(vTam, hud.dialogue, new[]
            {
                Line("Tam", portraitM, "Mind the south gate after dark, little spark. The wilds have been sniffing around the fences lately."),
            });
            MakeTalkable(vHobb, hud.dialogue, new[]
            {
                Line("Hobb", portraitM, "Yams came in early this year. Good sign — the soil's still warm, no matter how cold the nights get."),
            });
            MakeTalkable(vWren, hud.dialogue, new[]
            {
                Line("Wren", portraitF, "The pond's been still as glass all week. Even the fish are waiting for the festival, I think."),
                Line("Pip", portraitPip, "Or they're just shy, Wren. Like someone I know."),
            });

            // ---------- save + register ----------
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Splash.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Cinematic.unity", true),
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Scenes/Interior_Elder.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Interior_Home.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Emberholt_Greybox.unity", true),
            };

            AssetDatabase.SaveAssets();
            Debug.Log("[Village] Emberholt built and saved to " + ScenePath);
        }

        // ================================================================== ground bake

        static Sprite BakeGround()
        {
            byte[] bytes = File.ReadAllBytes(AtlasPath);
            var atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!atlas.LoadImage(bytes))
            {
                Debug.LogError("[Village] Could not decode atlas PNG.");
                return null;
            }

            Color[] grass = SampleTile(atlas, GrassPick);
            Color[] dirt = SampleTile(atlas, DirtPick);
            Color[] cobble = SampleTile(atlas, CobblePick);
            Color[] water = SampleTile(atlas, WaterPick);
            Object.DestroyImmediate(atlas);

            // mute the dirt: the raw tile reads neon under warm lantern/lamp light
            for (int i = 0; i < dirt.Length; i++)
            {
                var c = dirt[i];
                float lum = c.r * 0.3f + c.g * 0.55f + c.b * 0.15f;
                dirt[i] = new Color(Mathf.Lerp(c.r, lum, 0.18f) * 0.88f,
                    Mathf.Lerp(c.g, lum, 0.18f) * 0.88f, Mathf.Lerp(c.b, lum, 0.18f) * 0.88f, 1f);
            }

            // probe strip so picks can be verified visually (Tools + Art/Build)
            WriteProbe(grass, dirt, cobble, water);

            int W = MapW * TilePx, H = MapH * TilePx;
            var px = new Color[W * H];
            var zone = new Zone[MapW, MapH];

            for (int ty = 0; ty < MapH; ty++)
                for (int tx = 0; tx < MapW; tx++)
                    zone[tx, ty] = ZoneAt(MapMin.x + tx + 0.5f, MapMin.y + ty + 0.5f);

            for (int ty = 0; ty < MapH; ty++)
            {
                for (int tx = 0; tx < MapW; tx++)
                {
                    var src = zone[tx, ty] == Zone.Dirt ? dirt : zone[tx, ty] == Zone.Cobble ? cobble : zone[tx, ty] == Zone.Water ? water : grass;
                    int tileSeed = (tx * 73856093) ^ (ty * 19349663);
                    float tileVar = 0.95f + ((tileSeed >> 3) & 0xF) / 255f * 1.6f; // subtle per-tile luminance shift
                    // patterned tiles (cobble/water) get random mirrors so the 16px motif doesn't band
                    bool flipX = ((tileSeed >> 9) & 1) != 0 && (zone[tx, ty] == Zone.Cobble || zone[tx, ty] == Zone.Water);
                    bool flipY = ((tileSeed >> 11) & 1) != 0 && zone[tx, ty] == Zone.Water;
                    for (int y = 0; y < TilePx; y++)
                    {
                        for (int x = 0; x < TilePx; x++)
                        {
                            var c = src[(flipY ? TilePx - 1 - y : y) * TilePx + (flipX ? TilePx - 1 - x : x)];
                            float v = tileVar;
                            if (zone[tx, ty] == Zone.Grass)
                            {
                                int n = (x * 97 + y * 57 + tileSeed) & 0xFF;
                                v *= 0.96f + n / 255f * 0.08f;
                            }
                            int dxp = tx * TilePx + x, dyp = ty * TilePx + y;
                            px[dyp * W + dxp] = new Color(c.r * v, c.g * v, c.b * v, 1f);
                        }
                    }
                }
            }

            // edge darkening: path/plaza/water tiles bordering grass get a worn 2px rim
            for (int ty = 1; ty < MapH - 1; ty++)
            {
                for (int tx = 1; tx < MapW - 1; tx++)
                {
                    if (zone[tx, ty] == Zone.Grass) continue;
                    DarkenEdge(px, W, tx, ty, zone[tx, ty + 1] == Zone.Grass, 0, TilePx - 2, TilePx, TilePx);   // north
                    DarkenEdge(px, W, tx, ty, zone[tx, ty - 1] == Zone.Grass, 0, 0, TilePx, 2);                 // south
                    DarkenEdge(px, W, tx, ty, zone[tx + 1, ty] == Zone.Grass, TilePx - 2, 0, TilePx, TilePx);   // east
                    DarkenEdge(px, W, tx, ty, zone[tx - 1, ty] == Zone.Grass, 0, 0, 2, TilePx);                 // west
                }
            }

            UiSpriteGen.EnsureFolder(BuildArtRoot);
            string path = $"{BuildArtRoot}/village_ground.png";
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = false;
            ti.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log($"[Village] Ground baked {W}x{H}px from atlas picks G{GrassPick} D{DirtPick} C{CobblePick} W{WaterPick}");
            return sprite;
        }

        static void DarkenEdge(Color[] px, int W, int tx, int ty, bool condition, int x0, int y0, int x1, int y1)
        {
            // darkens a 2px rim inside tile (tx,ty) over the half-open pixel range [x0,x1) x [y0,y1)
            if (!condition) return;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int idx = (ty * TilePx + y) * W + tx * TilePx + x;
                    px[idx] = new Color(px[idx].r * 0.78f, px[idx].g * 0.78f, px[idx].b * 0.78f, 1f);
                }
        }

        static void WriteProbe(Color[] grass, Color[] dirt, Color[] cobble, Color[] water)
        {
            const int scale = 8;
            int w = 4 * TilePx * scale, h = TilePx * scale;
            var probe = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var sets = new[] { grass, dirt, cobble, water };
            for (int s = 0; s < 4; s++)
                for (int y = 0; y < TilePx * scale; y++)
                    for (int x = 0; x < TilePx * scale; x++)
                        probe.SetPixel(s * TilePx * scale + x, y, sets[s][(y / scale) * TilePx + (x / scale)]);
            probe.Apply();
            UiSpriteGen.EnsureFolder(BuildArtRoot);
            File.WriteAllBytes($"{BuildArtRoot}/village_tiles_probe.png", probe.EncodeToPNG());
            Object.DestroyImmediate(probe);
            AssetDatabase.ImportAsset($"{BuildArtRoot}/village_tiles_probe.png", ImportAssetOptions.ForceUpdate);
        }

        static Color[] SampleTile(Texture2D atlas, Vector2Int pick)
        {
            // pick is in image coords (top-left origin); GetPixels is bottom-left origin
            return atlas.GetPixels(pick.x, AtlasH - pick.y - TilePx, TilePx, TilePx);
        }

        // ================================================================== zones

        static Zone ZoneAt(float x, float y)
        {
            if (InPond(x, y, 1f)) return Zone.Water;
            if (InPond(x, y, 1.35f)) return Zone.Cobble; // shore ring
            if (InPlaza(x, y)) return Zone.Cobble;
            if (OnPolyline(Street, x, y, 1.6f) || OnPolyline(WestStreet, x, y, 1.25f) || OnPolyline(FarmStreet, x, y, 1.25f)) return Zone.Dirt;
            if (x >= 11 && x <= 21 && y >= -25 && y <= -17) return Zone.Dirt; // farm field
            return Zone.Grass;
        }

        static bool InPlaza(float x, float y)
        {
            float dx = Mathf.Max(Mathf.Abs(x) - (8f - 2.5f), 0f);
            float dy = Mathf.Max(Mathf.Abs(y - 6f) - (6f - 2.5f), 0f);
            return dx * dx + dy * dy <= 2.5f * 2.5f;
        }

        static bool InPond(float x, float y, float scale)
        {
            float dx = (x - 15f) / (4f * scale);
            float dy = (y - 25f) / (2.6f * scale);
            return dx * dx + dy * dy <= 1f;
        }

        static bool OnPolyline(Vector2[] line, float x, float y, float halfWidth)
        {
            var p = new Vector2(x, y);
            for (int i = 0; i < line.Length - 1; i++)
                if (DistToSegment(p, line[i], line[i + 1]) <= halfWidth) return true;
            return false;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ================================================================== placement

        static Sprite[] _atlasSprites;

        static Sprite AtlasSpriteNear(float imgX, float imgY, string label)
        {
            if (_atlasSprites == null)
                _atlasSprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            float targetY = AtlasH - imgY; // sprite rect origin = bottom-left
            Sprite best = null;
            float bestD = float.MaxValue;
            foreach (var s in _atlasSprites)
            {
                float d = Vector2.Distance(s.rect.center, new Vector2(imgX, targetY));
                if (d < bestD) { bestD = d; best = s; }
            }
            if (best == null || bestD > 48f)
                Debug.LogWarning($"[Village] Atlas sprite '{label}' lookup at ({imgX},{imgY}) is {bestD:F0}px off — check placement.");
            return best;
        }

        static void PlaceAtlasDecor(Vector2 imgCoord, Vector2 pos, string name, bool solid, Vector2 solidSize = default, int sortOffset = 0)
        {
            var sprite = AtlasSpriteNear(imgCoord.x, imgCoord.y, name);
            if (sprite == null) return;
            PlaceDecorRaw(sprite, pos, name, solid, sortOffset, solidSize);
        }

        static GameObject PlaceDecorRaw(Sprite sprite, Vector2 pos, string name, bool solid, int sortOffset = 0, Vector2 solidSize = default)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var ys = go.AddComponent<YSort>();
            ys.isStatic = true;
            ys.offset = sortOffset;
            if (solid)
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.size = solidSize == default ? new Vector2(0.8f, 0.6f) : solidSize;
            }
            return go;
        }

        static void PlaceHouse(string name, Vector2 pos)
        {
            var go = PlacePrefab($"{PrefabRoot}/Houses/{name}.prefab", new Vector3(pos.x, pos.y, 0));
            if (go == null) return;
            // prefab ships one BoxCollider2D already; ensure it sits at the base
            var b = go.GetComponent<BoxCollider2D>();
            if (b == null)
            {
                b = go.AddComponent<BoxCollider2D>();
                b.size = new Vector2(3f, 1.4f);
            }
        }

        static void JitterTree(System.Random rng, Vector2 pos)
        {
            float jx = pos.x + (float)(rng.NextDouble() - 0.5) * 1.4f;
            float jy = pos.y + (float)(rng.NextDouble() - 0.5) * 1.0f;
            int variant = rng.Next(1, 9);
            var go = PlacePrefab($"{PrefabRoot}/Trees/tree_{variant:00}.prefab", new Vector3(jx, jy, 0));
            if (go != null && go.GetComponent<Collider2D>() == null)
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.size = new Vector2(0.7f, 0.5f);
                b.offset = new Vector2(0, 0.25f);
            }
        }

        static GameObject PlacePrefab(string path, Vector3 pos, bool addYSort = true)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[Village] Missing prefab: " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
            if (addYSort && go.GetComponent<YSort>() == null && go.GetComponentInChildren<SpriteRenderer>() != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var ys = go.AddComponent<YSort>();
                    ys.isStatic = true;
                }
            }
            return go;
        }

        static string FirstPrefabIn(string folder)
        {
            string dir = $"{PrefabRoot}/{folder}";
            if (!AssetDatabase.IsValidFolder(dir)) { Debug.LogWarning("[Village] No folder: " + dir); return null; }
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { dir });
            System.Array.Sort(guids);
            return guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
        }

        static void AddWall(Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject($"Wall_{parent.childCount}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
        }

        static void AddSolid(string name, Vector2 center, Vector2 size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
        }

        static GameObject AddVillager(Transform parent, string charaFolder, Vector2 pos, float wanderRadius)
        {
            string sheetPath = $"{PackRoot}/Characters/{charaFolder}/spritesheet.png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length < 12)
            {
                Debug.LogWarning("[Village] " + charaFolder + " has " + sprites.Length + " sprites, expected 12.");
                return null;
            }
            System.Array.Sort(sprites, (a, b) => NumericSuffix(a.name).CompareTo(NumericSuffix(b.name)));

            var go = new GameObject($"Villager_{charaFolder}_{parent.childCount}");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var col = go.AddComponent<CapsuleCollider2D>();
            col.size = new Vector2(0.6f, 0.5f);
            col.offset = new Vector2(0, -0.5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[1];
            var npc = go.AddComponent<VillagerNpc>();
            npc.down = new[] { sprites[0], sprites[1], sprites[2] };
            npc.left = new[] { sprites[3], sprites[4], sprites[5] };
            npc.right = new[] { sprites[6], sprites[7], sprites[8] };
            npc.up = new[] { sprites[9], sprites[10], sprites[11] };
            npc.wanderRadius = wanderRadius;
            go.AddComponent<YSort>();
            return go;
        }

        static void AddDoor(Vector2 pos, string targetScene, Vector2 returnSpawn)
        {
            var go = new GameObject($"Door_{targetScene}_{pos.x:0}_{pos.y:0}");
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            go.AddComponent<Interactable>();
            var door = go.AddComponent<DoorLink>();
            door.targetScene = targetScene;
            door.targetSpawn = new Vector2(0, -2.6f); // interior door mat
            door.returnSpawn = returnSpawn;
        }

        static DialogueLine Line(string speaker, Sprite portrait, string text)
            => new DialogueLine { speaker = speaker, portrait = portrait, text = text };

        static void MakeTalkable(GameObject npcGo, DialogueUI dialogue, DialogueLine[] lines)
        {
            if (npcGo == null) return;
            if (npcGo.GetComponent<Interactable>() == null) npcGo.AddComponent<Interactable>();
            var talk = npcGo.AddComponent<Talkable>();
            talk.dialogue = dialogue;
            talk.npc = npcGo.GetComponent<VillagerNpc>();
            talk.lines = lines;
        }

        static int NumericSuffix(string n)
        {
            int i = n.LastIndexOf('_');
            return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
        }

        static Sprite FindHeroIdleSprite()
        {
            const string p = PackRoot + "/Hero/hero/color_1/idle/hero_idle_DOWN.png";
            var spr = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().FirstOrDefault();
            if (spr == null) Debug.LogWarning("[Village] hero_idle_DOWN sprite not found.");
            return spr;
        }

    }
}
