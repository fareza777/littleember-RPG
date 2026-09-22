using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using LittleEmber.CameraWork;
using LittleEmber.Combat;
using LittleEmber.Player;
using LittleEmber.UI;
using LittleEmber.World;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Builds cozy house interiors (Elder's home, a generic home). Floor and
    /// walls are one baked texture (procedural planks, wainscot, rug, dark
    /// surround), furniture comes from pack prefabs, warm torch + lantern light.
    /// Exit: step onto the door mat (ExitZone) — back to the village.
    /// </summary>
    public static class InteriorSceneBuilder
    {
        const string PackRoot = "Assets/Gif/Super_Retro_Collection/Resources";
        const string AnimatorPath = "Assets/Art/Animations/Hero/Pip.controller";
        const string BuildArtRoot = "Assets/Art/Build";

        // room geometry (world units)
        const float RoomW = 12f, RoomH = 9f;
        const float WallTop = 2.2f;    // top wall band height
        const float WallSide = 0.7f;   // side/bottom band thickness
        static readonly Vector2 BakeMin = new Vector2(-10, -8.5f); // bake includes dark surround
        const int PPU = 32;

        [MenuItem("LittleEmber/StageB/2 - Build Interior Scenes")]
        public static void Build()
        {
            // targetSpawn is only a fallback — DoorLink sets SceneFlow.ReturnSpawn dynamically
            BuildInterior("Interior_Elder", "ELDER MARAS HOME", true, new Vector2(-18, -8.6f));
            BuildInterior("Interior_Home", "EMBERHOLT HOME", false, new Vector2(-11, 1.4f));
            Debug.Log("[Interior] Both interiors built.");
        }

        static void BuildInterior(string sceneName, string title, bool elder, Vector2 villageReturnOverride)
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);
            if (controller == null) { Debug.LogError("[Interior] Pip.controller missing."); return; }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---------- camera ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.8f;
            cam.backgroundColor = new Color(0.03f, 0.02f, 0.02f);
            camGo.transform.position = new Vector3(0, -1, -10);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.useBounds = true;
            follow.minBounds = new Vector2(-3.4f, -1.4f);
            follow.maxBounds = new Vector2(3.4f, 0.6f);
            cam.GetUniversalAdditionalCameraData();

            // ---------- warm dim global light ----------
            var glGo = new GameObject("Global Light 2D");
            var gl = glGo.AddComponent<Light2D>();
            gl.lightType = Light2D.LightType.Global;
            gl.color = new Color(0.92f, 0.80f, 0.66f);
            gl.intensity = 0.42f;

            // ---------- baked room ----------
            var roomGo = new GameObject("Room");
            var roomSr = roomGo.AddComponent<SpriteRenderer>();
            Debug.Log("[Interior] step: bake room " + sceneName);
            roomSr.sprite = BakeRoom(elder);
            roomSr.sortingOrder = -1000;
            Debug.Log("[Interior] step: room baked " + sceneName);

            // ---------- wall colliders (door gap at bottom center) ----------
            var walls = new GameObject("Walls");
            float inTop = RoomH / 2f - WallTop;          // walkable ceiling  y = 2.3
            float inBot = -RoomH / 2f + WallSide;        // walkable floor    y = -3.8
            float inSide = RoomW / 2f - WallSide;        // walkable sides    x = ±5.3
            AddWall(walls.transform, new Vector2(0, inTop + WallTop / 2f + 0.01f), new Vector2(RoomW, WallTop)); // top
            AddWall(walls.transform, new Vector2(-inSide - WallSide / 2f, 0), new Vector2(WallSide, RoomH));     // left
            AddWall(walls.transform, new Vector2(inSide + WallSide / 2f, 0), new Vector2(WallSide, RoomH));      // right
            // bottom wall split around the door gap (|x| > 1.0)
            float segW = (RoomW - 2f) / 2f;
            AddWall(walls.transform, new Vector2(-(1f + segW / 2f), inBot - WallSide / 2f), new Vector2(segW, WallSide));
            AddWall(walls.transform, new Vector2(1f + segW / 2f, inBot - WallSide / 2f), new Vector2(segW, WallSide));

            var decor = new GameObject("Decor");

            // ---------- furniture (pack prefabs) ----------
            if (elder)
            {
                Place($"{PackRoot}/Prefabs/Books/book_01.prefab", new Vector3(-3.7f, 1.5f, 0));
                Place($"{PackRoot}/Prefabs/Books/book_03.prefab", new Vector3(-3.4f, 1.15f, 0));
                Place($"{PackRoot}/Prefabs/Books/book_05.prefab", new Vector3(-3.55f, 1.9f, 0));
                PlaceSolid($"{PackRoot}/Prefabs/Potted plants/potted_plant_03.prefab", new Vector3(-4.6f, 1.7f, 0), decor.transform);
                PlaceSolid($"{PackRoot}/Prefabs/Potted plants/potted_plant_07.prefab", new Vector3(4.6f, 1.7f, 0), decor.transform);
                PlaceSolid($"{PackRoot}/Prefabs/Barrels/barrel_01.prefab", new Vector3(-4.5f, -2.4f, 0), decor.transform);
                PlaceSolid($"{PackRoot}/Prefabs/Pots/pot_04.prefab", new Vector3(4.5f, -2.2f, 0), decor.transform);
            }
            else
            {
                PlaceSolid($"{PackRoot}/Prefabs/Potted plants/potted_plant_10.prefab", new Vector3(-4.6f, 1.7f, 0), decor.transform);
                PlaceSolid($"{PackRoot}/Prefabs/Crates/crate_02.prefab", new Vector3(4.5f, 1.6f, 0), decor.transform);
                PlaceSolid($"{PackRoot}/Prefabs/Barrels/barrel_03.prefab", new Vector3(4.5f, 0.7f, 0), decor.transform);
                Place($"{PackRoot}/Prefabs/Books/book_07.prefab", new Vector3(-3.6f, -2.3f, 0));
                PlaceSolid($"{PackRoot}/Prefabs/Pots/pot_09.prefab", new Vector3(-4.5f, 0.6f, 0), decor.transform);
            }

            // ---------- torch on the top wall ----------
            var torch = Place($"{PackRoot}/Prefabs/Torches/Torch_02.prefab", new Vector3(elder ? 3.4f : 0f, 2.25f, 0));
            if (torch != null)
            {
                var tl = torch.AddComponent<Light2D>();
                tl.lightType = Light2D.LightType.Point;
                tl.color = new Color(1f, 0.68f, 0.34f);
                tl.intensity = 1.35f;
                tl.pointLightInnerRadius = 1.2f;
                tl.pointLightOuterRadius = 6.5f;
                torch.AddComponent<FlickerLight2D>().amount = 0.22f;
            }

            // ---------- elder NPC ----------
            Debug.Log("[Interior] step: furniture done " + sceneName);
            DialogueUI dlgRefHolder = null; // assigned after HUD build; closure wired at runtime via Talkable
            GameObject elderNpc = null;
            if (elder)
            {
                elderNpc = AddVillager("chara_03", new Vector2(1.2f, 0.9f), 0.7f);
            }

            // ---------- Pip ----------
            var pip = BuildPip(controller, new Vector3(0, -2.6f, 0));
            var pipCtl = pip.GetComponent<PipController>();
            pipCtl.persistState = false; // interiors never become the saved scene
            follow.target = pip.transform;

            // ---------- HUD (no hints, room title) ----------
            Debug.Log("[Interior] step: pip done " + sceneName);
            var refs = HudBuilder.Build(pip.GetComponent<Health>(), pipCtl, title, false);
            dlgRefHolder = refs.dialogue;
            Debug.Log("[Interior] step: hud done " + sceneName);

            // elder dialogue (wired after HUD exists)
            if (elder && elderNpc != null)
            {
                var talk = elderNpc.AddComponent<Talkable>();
                talk.dialogue = dlgRefHolder;
                talk.npc = elderNpc.GetComponent<VillagerNpc>();
                talk.lines = ElderLines();
                var inter = elderNpc.GetComponent<Interactable>();
                if (inter == null) inter = elderNpc.AddComponent<Interactable>();
                inter.radius = 1.8f;
            }

            // ---------- exit door mat ----------
            var exit = new GameObject("ExitZone");
            exit.transform.position = new Vector3(0, -4.15f, 0);
            var ec = exit.AddComponent<BoxCollider2D>();
            ec.isTrigger = true;
            ec.size = new Vector2(1.8f, 0.8f);
            var ez = exit.AddComponent<ExitZone>();
            ez.targetScene = "Emberholt_Village";
            ez.targetSpawn = villageReturnOverride; // builder sets per-door when entering

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, $"Assets/Scenes/{sceneName}.unity");
            Debug.Log("[Interior] Saved " + sceneName);
        }

        // ================================================================== room bake

        static Sprite BakeRoom(bool elder)
        {
            int W = (int)((10f * 2f) * PPU);   // 640
            int H = (int)((8.5f * 2f) * PPU);  // 544
            var px = new Color32[W * H];

            var dark = new Color(0.045f, 0.032f, 0.03f);
            var wall = new Color(0.235f, 0.150f, 0.095f);
            var wallDark = new Color(0.165f, 0.105f, 0.070f);
            var baseboard = new Color(0.45f, 0.30f, 0.16f);
            var woodA = new Color(0.400f, 0.265f, 0.150f);
            var rugC = new Color(0.430f, 0.120f, 0.115f);
            var rugTrim = new Color(0.820f, 0.620f, 0.240f);

            int plankH = 16;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    // world position of this pixel
                    float wx = BakeMin.x + (x + 0.5f) / PPU;
                    float wy = BakeMin.y + (y + 0.5f) / PPU;

                    Color c = dark;
                    bool inRoomX = wx >= -RoomW / 2f && wx < RoomW / 2f;
                    bool inRoomY = wy >= -RoomH / 2f && wy < RoomH / 2f;
                    if (inRoomX && inRoomY)
                    {
                        float inTop = RoomH / 2f - WallTop;
                        float inSide = RoomW / 2f - WallSide;
                        float inBot = -RoomH / 2f + WallSide;
                        bool wallZone = wy >= inTop || Mathf.Abs(wx) >= inSide || wy <= inBot;

                        if (wallZone)
                        {
                            c = wall;
                            // vertical board shading on the top wall
                            if (wy >= inTop)
                            {
                                int board = (int)((wx + 100f) * PPU / 24);
                                c = ((board & 1) == 0) ? wall : wallDark;
                            }
                            // baseboard highlight where wall meets floor
                            if (wy >= inTop - 0.14f && wy < inTop) c = baseboard;
                            if (Mathf.Abs(Mathf.Abs(wx) - inSide) < 0.07f && wy < inTop) c = baseboard;
                            if (Mathf.Abs(wy - inBot) < 0.07f && wy < inTop && Mathf.Abs(wx) < inSide) c = baseboard;
                        }
                        else
                        {
                            // plank floor
                            int lx = x % W, ly = y % H;
                            int plank = y / plankH;
                            int h = (int)((plank * 2654435761L) & 0x7FFF);
                            float jitter = 0.90f + (h % 100) / 100f * 0.18f;
                            c = woodA * jitter;
                            if (y % plankH == 0) c *= 0.55f;                 // seam
                            int grain = (lx * 7 + ly * 13 + h) & 0x3F;
                            if (grain < 3) c *= 0.94f;                        // sparse grain
                            // plank butt joints
                            int off = (plank & 1) == 0 ? 0 : 96;
                            if ((x + off) % 192 < 2) c *= 0.7f;
                        }

                        // rug (elder: large wine rug; home: smaller)
                        float rw = elder ? 5.2f : 3.6f, rh = elder ? 3.4f : 2.4f;
                        float rdx = Mathf.Abs(wx - 0f) - rw / 2f;
                        float rdy = Mathf.Abs(wy - (-0.6f)) - rh / 2f;
                        float rd = Mathf.Max(rdx, rdy);
                        if (rd < 0f && wy < inTop && wy > inBot && Mathf.Abs(wx) < inSide)
                        {
                            c = rugC * (0.94f + ((x * 31 + y * 17) & 0xF) / 255f * 1.5f);
                            if (rd > -0.22f) c = rugTrim;                     // gold trim
                            else if (rd > -0.34f) c = rugC * 0.6f;            // inner line
                        }
                    }

                    // soft vignette in the dark surround
                    px[y * W + x] = c;
                }
            }

            UiSpriteGen.EnsureFolder(BuildArtRoot);
            string path = $"{BuildArtRoot}/room_{(elder ? "elder" : "home")}.png";
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== helpers

        static GameObject BuildPip(RuntimeAnimatorController controller, Vector3 pos)
        {
            var pip = new GameObject("Pip");
            pip.transform.position = pos;
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
            var health = pip.AddComponent<Health>();
            health.maxHearts = 3;
            pip.AddComponent<PipController>();
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
            lantern.hpSource = health;
            return pip;
        }

        static GameObject AddVillager(string charaFolder, Vector2 pos, float wanderRadius)
        {
            string sheetPath = $"{PackRoot}/Characters/{charaFolder}/spritesheet.png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath).OfType<Sprite>().ToArray();
            if (sprites.Length < 12) { Debug.LogWarning("[Interior] " + charaFolder + " sprites missing."); return null; }
            System.Array.Sort(sprites, (a, b) => NumericSuffix(a.name).CompareTo(NumericSuffix(b.name)));

            var go = new GameObject($"NPC_{charaFolder}");
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

        static DialogueLine[] ElderLines()
        {
            var elder = HudBuilder.EnsureUiSprite("Assets/Art/Portraits/portrait_elder.png");
            var pip = HudBuilder.EnsureUiSprite("Assets/Art/Portraits/portrait_pip.png");
            return new[]
            {
                new DialogueLine { speaker = "Elder Mara", portrait = elder, text = "Ah, little ember. Come in, come in — the lanterns are hung, and the whole village smells of cedar and honey." },
                new DialogueLine { speaker = "Elder Mara", portrait = elder, text = "Tomorrow night we light the Kindling Flame. The same flame your mother carried, once — before the cold crept back over the ridge." },
                new DialogueLine { speaker = "Pip", portrait = pip, text = "I'll keep it burning, Elder. Brighter than ever — promise." },
                new DialogueLine { speaker = "Elder Mara", portrait = elder, text = "I know you will, child. But mind the old paths when you wander. The dark has been... restless this year." },
            };
        }

        static GameObject Place(string path, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) { Debug.LogWarning("[Interior] Missing prefab: " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = pos;
            if (go.GetComponent<YSort>() == null && go.GetComponent<SpriteRenderer>() != null)
                go.AddComponent<YSort>().isStatic = true;
            return go;
        }

        static void PlaceSolid(string path, Vector3 pos, Transform decorParent)
        {
            var go = Place(path, pos);
            if (go == null) return;
            if (go.GetComponent<Collider2D>() == null)
            {
                var b = go.AddComponent<BoxCollider2D>();
                b.size = new Vector2(0.6f, 0.45f);
            }
        }

        static void AddWall(Transform parent, Vector2 center, Vector2 size)
        {
            var go = new GameObject($"Wall_{parent.childCount}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var b = go.AddComponent<BoxCollider2D>();
            b.size = size;
        }

        static int NumericSuffix(string n)
        {
            int i = n.LastIndexOf('_');
            return i >= 0 && int.TryParse(n.Substring(i + 1), out int v) ? v : 0;
        }

        static Sprite FindHeroIdleSprite()
        {
            const string p = PackRoot + "/Hero/hero/color_1/idle/hero_idle_DOWN.png";
            return AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().FirstOrDefault();
        }
    }
}
