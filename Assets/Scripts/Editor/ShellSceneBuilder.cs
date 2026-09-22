using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LittleEmber.Cine;
using LittleEmber.UI;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Stage A shell: imports generated art (app icon + lantern panel), applies
    /// Android adaptive icons + Unity splash settings, bakes shared UI sprites,
    /// and builds Splash / MainMenu / Cinematic scenes. All procedural.
    /// Stage B polish: glowing splash, letterspaced titles, letterbox cinematic.
    /// </summary>
    public static class ShellSceneBuilder
    {
        const string ScenesRoot = "Assets/Scenes";
        const string CineArtRoot = "Assets/Art/Cine";
        const string IconRoot = "Assets/Art/Icon";
        const string VoRoot = "Assets/Art/Audio/VO";
        const string PlatesRoot = "Assets/Gif/Super_Retro_Collection/Resources/Backgrounds";
        const string GeneratedImages = @"C:\Users\ASUS\.factory\generated-images";

        static readonly Vector2 RefRes = new Vector2(1080, 1920);

        static readonly Color Amber = new Color(0.79f, 0.47f, 0.18f);
        static readonly Color BlueBtn = new Color(0.22f, 0.33f, 0.52f);
        static readonly Color SlateBtn = new Color(0.20f, 0.24f, 0.33f);
        static readonly Color Cream = new Color(0.98f, 0.93f, 0.80f);
        static readonly Color CardBg = new Color(0.09f, 0.115f, 0.18f, 0.98f);
        static readonly Color NightBlue = new Color(0.043f, 0.055f, 0.102f);

        [MenuItem("LittleEmber/StageA/1 - Build Shell (Splash, Menu, Cinematic, Icons)")]
        public static void BuildAll()
        {
            var iconSprite = ImportLatestGenerated("mobile-game-app-icon*.png", IconRoot, "icon_source");
            var lanternSprite = ImportLatestGenerated("painterly-storybook-illustration*.png", CineArtRoot, "cine_07_lantern");
            if (iconSprite == null)
            {
                Debug.LogError("[Shell] App icon source not found in " + GeneratedImages);
                return;
            }

            ApplyAppIcons(iconSprite.texture);
            ApplySplashSettings(iconSprite);

            var white = UiSpriteGen.RoundedRect("ui_panel_white", 96, 22, new Color32(255, 255, 255, 255));
            var handle = UiSpriteGen.SoftDisc("ui_handle", 64);
            var scrim = UiSpriteGen.GradientV("ui_scrim", 8, 256, new Color32(5, 6, 10, 240), new Color32(5, 6, 10, 0));
            var ember = UiSpriteGen.SoftDisc("ui_ember", 32);

            BuildSplashScene(iconSprite, ember);
            BuildMainMenuScene(lanternSprite, white, handle, ember, scrim);
            BuildCinematicScene(white, scrim);

            RegisterScenes();

            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.productName = "Little Ember";
            AssetDatabase.SaveAssets();
            Debug.Log("[Shell] Stage A shell built: Splash, MainMenu, Cinematic + icons + splash settings.");
        }

        // ================================================================== icons & splash

        static void ApplyAppIcons(Texture2D src)
        {
            var bg = new Color32(0x1A, 0x22, 0x38, 255);

            // Unity 6: Adaptive is the only supported kind (Round/Legacy are
            // derived from it automatically at build time).
            ApplyIconKind(AndroidPlatformIconKind.Adaptive, (size, layer) =>
                layer == 1
                    ? SaveIcon(SolidIcon(size, bg), $"icon_adaptive_{size}_bg")
                    : SaveIcon(ComposeIcon(src, size, 0.66f, false, bg, true), $"icon_adaptive_{size}_fg"));

            Debug.Log("[Shell] Android adaptive icons applied.");
        }

        static void ApplyIconKind(PlatformIconKind kind, System.Func<int, int, Texture2D> make)
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var icon in icons)
                for (int layer = 0; layer < icon.maxLayerCount; layer++)
                    icon.SetTexture(make(icon.width, layer), layer);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        static Texture2D SolidIcon(int size, Color32 color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Texture2D ComposeIcon(Texture2D src, int size, float scale, bool round, Color32 bg, bool transparent)
        {
            var rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, transparent ? (Color)new Color32(0, 0, 0, 0) : (Color)bg);
            float s = size * scale;
            Graphics.DrawTexture(new Rect((size - s) * 0.5f, (size - s) * 0.5f, s, s), src);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            if (round)
            {
                var px = tex.GetPixels32();
                float c = (size - 1) * 0.5f, r = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                        float a = Mathf.Clamp01(r - d + 0.5f);
                        var p = px[y * size + x];
                        px[y * size + x] = new Color32(p.r, p.g, p.b, (byte)(p.a * a));
                    }
                tex.SetPixels32(px);
                tex.Apply();
            }
            return tex;
        }

        static Texture2D SaveIcon(Texture2D tex, string name)
        {
            UiSpriteGen.EnsureFolder(IconRoot);
            string path = $"{IconRoot}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void ApplySplashSettings(Sprite iconSprite)
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.backgroundColor = NightBlue;
            PlayerSettings.SplashScreen.logos = new[]
            {
                PlayerSettings.SplashScreenLogo.Create(2.2f, iconSprite)
            };
        }

        // ================================================================== art import

        static Sprite ImportLatestGenerated(string pattern, string destRoot, string destName)
        {
            if (!Directory.Exists(GeneratedImages)) return null;
            var files = Directory.GetFiles(GeneratedImages, pattern)
                .OrderByDescending(File.GetLastWriteTime).ToArray();
            if (files.Length == 0) return null;
            UiSpriteGen.EnsureFolder(destRoot);
            string dst = $"{destRoot}/{destName}.png";
            File.Copy(files[0], dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
        }

        static Sprite ImportPlate(string plateName, string destName)
        {
            string src = $"{PlatesRoot}/{plateName}.png";
            if (!File.Exists(src)) { Debug.LogWarning("[Shell] Missing plate: " + src); return null; }
            UiSpriteGen.EnsureFolder(CineArtRoot);
            string dst = $"{CineArtRoot}/{destName}.png";
            File.Copy(src, dst, true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(dst);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = false;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(dst);
        }

        // ================================================================== ember particles (shared)

        static void MakeEmbers(Sprite emberSprite, Vector3 pos, float rate, float sizeMin, float sizeMax)
        {
            UiSpriteGen.EnsureFolder(UiSpriteGen.UiRoot);
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{UiSpriteGen.UiRoot}/m_ember.mat");
            if (mat == null)
            {
                mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = emberSprite.texture };
                AssetDatabase.CreateAsset(mat, $"{UiSpriteGen.UiRoot}/m_ember.mat");
            }

            var psGo = new GameObject("Embers");
            psGo.transform.position = pos;
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 6f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.60f, 0.24f), new Color(1f, 0.86f, 0.52f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(16f, 0.5f, 1f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            vel.y = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f); // all three axes must share one curve mode
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.18f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            ps.GetComponent<ParticleSystemRenderer>().material = mat;
        }

        // ================================================================== splash scene

        static void BuildSplashScene(Sprite iconSprite, Sprite emberSprite)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NightBlue;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            MakeEmbers(emberSprite, new Vector3(0, -10.5f, 0), 10f, 0.05f, 0.22f);

            var canvasGo = NewCanvas("Canvas");
            var font = BuiltinFont();

            var group = NewRect("LogoGroup", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(900, 1000));
            var cg = group.gameObject.AddComponent<CanvasGroup>();

            // warm breathing glow behind the icon
            var glowGo = NewRect("Glow", group, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(780, 780));
            var glowImg = glowGo.gameObject.AddComponent<Image>();
            glowImg.sprite = emberSprite;
            glowImg.color = new Color(1f, 0.62f, 0.25f, 0.34f);
            glowImg.raycastTarget = false;

            var iconGo = NewRect("Icon", group, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(380, 380));
            var iconImg = iconGo.gameObject.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            MakeLabel(group, "Title", "L I T T L E   E M B E R", new Vector2(0, -160), new Vector2(1040, 140),
                84, new Color(0.96f, 0.76f, 0.42f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0.5f), true);
            MakeLabel(group, "Subtitle", "a tiny lantern tale", new Vector2(0, -258), new Vector2(900, 56),
                36, new Color(0.88f, 0.83f, 0.70f, 0.8f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0.5f), false);
            MakeLabel(canvasGo.transform, "Footer", "LittleEmber Studio", new Vector2(0, 70), new Vector2(700, 44),
                27, new Color(1f, 1f, 1f, 0.35f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0f), false);

            var splash = canvasGo.AddComponent<SplashScreen>();
            splash.logoGroup = cg;
            splash.logoImage = iconGo;
            splash.glow = glowGo;
            splash.nextScene = "MainMenu";

            SaveScene(scene, $"{ScenesRoot}/Splash.unity");
        }

        // ================================================================== main menu scene

        static void BuildMainMenuScene(Sprite bgSprite, Sprite white, Sprite handle, Sprite emberSprite, Sprite scrim)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NightBlue;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            MakeEmbers(emberSprite, new Vector3(0, -10.5f, 0), 6f, 0.04f, 0.14f);

            var canvasGo = NewCanvas("Canvas");
            var font = BuiltinFont();

            // background plate, cover-fit
            var bgGo = NewRect("Background", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, RefRes);
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.raycastTarget = false;
            if (bgSprite != null)
            {
                var t = bgSprite.texture;
                float cover = Mathf.Max(RefRes.x / t.width, RefRes.y / t.height);
                bgGo.sizeDelta = new Vector2(t.width * cover, t.height * cover);
                bgImg.sprite = bgSprite;
            }
            bgImg.color = new Color(0.85f, 0.85f, 0.9f);
            var dim = NewRect("Dim", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dimImg = dim.gameObject.AddComponent<Image>();
            dimImg.color = new Color(0.02f, 0.03f, 0.06f, 0.42f);
            dimImg.raycastTarget = false;
            // bottom scrim so buttons read clearly
            var menuScrim = NewRect("BottomScrim", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 480), new Vector2(0, 960));
            var menuScrimImg = menuScrim.gameObject.AddComponent<Image>();
            menuScrimImg.sprite = scrim;
            menuScrimImg.color = new Color(1f, 1f, 1f, 0.85f);
            menuScrimImg.raycastTarget = false;

            // ---- main panel ----
            var mainPanel = NewRect("MainPanel", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = MakeLabel(mainPanel, "Title", "L I T T L E   E M B E R", new Vector2(0, -250), new Vector2(1060, 160),
                80, new Color(0.96f, 0.76f, 0.42f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), true);
            var glow = title.gameObject.AddComponent<Shadow>();
            glow.effectColor = new Color(1f, 0.55f, 0.15f, 0.4f);
            glow.effectDistance = new Vector2(0, 0);
            glow.useGraphicAlpha = false;
            MakeLabel(mainPanel, "Subtitle", "a tiny lantern tale", new Vector2(0, -352), new Vector2(1020, 54),
                36, new Color(0.88f, 0.83f, 0.70f, 0.85f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), false);

            var newBtn = MakeButton(mainPanel, "BtnNewGame", "NEW GAME", new Vector2(0, -60), new Vector2(640, 148), white, Amber, Cream, 48, font);
            var contBtn = MakeButton(mainPanel, "BtnContinue", "CONTINUE", new Vector2(0, -238), new Vector2(640, 148), white, BlueBtn, Cream, 48, font);
            var setBtn = MakeButton(mainPanel, "BtnSettings", "SETTINGS", new Vector2(0, -416), new Vector2(640, 148), white, SlateBtn, Cream, 48, font);
            var aboutBtn = MakeButton(mainPanel, "BtnAbout", "ABOUT", new Vector2(0, -594), new Vector2(640, 148), white, SlateBtn, Cream, 48, font);

            var contInfo = MakeLabel(mainPanel, "ContinueInfo", "", new Vector2(0, -322), new Vector2(640, 40),
                26, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0.5f), false);
            var version = MakeLabel(mainPanel, "Version", "", new Vector2(24, 150), new Vector2(700, 40),
                26, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleLeft, font, new Vector2(0f, 0f), false);

            // ---- name entry panel ----
            var nameCard = MakeCard(canvasGo.transform, "NamePanel", new Vector2(900, 780), white, out GameObject nameRoot);
            MakeLabel(nameCard, "Title", "NAME YOUR SPARK", new Vector2(0, -70), new Vector2(860, 80),
                56, Cream, TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), true);
            var nameInput = MakeInput(nameCard, new Vector2(0, -210), new Vector2(640, 110), white, font, "Pip");
            var nameError = MakeLabel(nameCard, "Error", "", new Vector2(0, -300), new Vector2(760, 40),
                28, new Color(0.95f, 0.45f, 0.38f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), false);
            MakeLabel(nameCard, "Note", "Saved automatically. Empty means \"Pip\".", new Vector2(0, -345), new Vector2(760, 40),
                26, new Color(1f, 1f, 1f, 0.55f), TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), false);
            var beginBtn = MakeButton(nameCard, "BtnBegin", "BEGIN", new Vector2(-170, -470), new Vector2(300, 104), white, Amber, Cream, 42, font);
            var nameBackBtn = MakeButton(nameCard, "BtnBack", "BACK", new Vector2(170, -470), new Vector2(300, 104), white, SlateBtn, Cream, 42, font);
            nameRoot.SetActive(false);

            // ---- settings panel ----
            var setCard = MakeCard(canvasGo.transform, "SettingsPanel", new Vector2(900, 1300), white, out GameObject setRoot);
            MakeLabel(setCard, "Title", "SETTINGS", new Vector2(0, -70), new Vector2(860, 80),
                64, Cream, TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), true);

            var masterS = MakeSliderRow(setCard, "MASTER", -190, white, handle, font);
            var musicS = MakeSliderRow(setCard, "MUSIC", -300, white, handle, font);
            var sfxS = MakeSliderRow(setCard, "SFX", -410, white, handle, font);
            var voS = MakeSliderRow(setCard, "VOICE", -520, white, handle, font);

            MakeLabel(setCard, "QualityLabel", "QUALITY", new Vector2(-300, -640), new Vector2(240, 56),
                34, Cream, TextAnchor.MiddleRight, font, new Vector2(0.5f, 1f), false);
            var qBtns = new Button[3];
            string[] qNames = { "LOW", "BALANCED", "HIGH" };
            for (int i = 0; i < 3; i++)
                qBtns[i] = MakeButton(setCard, $"BtnQ{i}", qNames[i], new Vector2(-90 + i * 155, -640), new Vector2(140, 84), white, SlateBtn, Cream, 28, font);

            MakeLabel(setCard, "VibrationLabel", "VIBRATION", new Vector2(-300, -760), new Vector2(240, 56),
                34, Cream, TextAnchor.MiddleRight, font, new Vector2(0.5f, 1f), false);
            var vibToggle = MakeToggle(setCard, new Vector2(270, -760), white, handle);

            var resetBtn = MakeButton(setCard, "BtnReset", "ERASE SAVE DATA", new Vector2(0, -910), new Vector2(520, 96), white, new Color(0.45f, 0.16f, 0.16f), Cream, 34, font);
            var resetLabel = resetBtn.GetComponentInChildren<Text>();
            var setBackBtn = MakeButton(setCard, "BtnBack", "BACK", new Vector2(0, -1050), new Vector2(520, 96), white, SlateBtn, Cream, 40, font);

            var settings = setRoot.AddComponent<SettingsPanel>();
            settings.masterSlider = masterS;
            settings.musicSlider = musicS;
            settings.sfxSlider = sfxS;
            settings.voSlider = voS;
            settings.qualityButtons = qBtns;
            settings.vibrationToggle = vibToggle;
            settings.resetButton = resetBtn;
            settings.resetLabel = resetLabel;
            settings.backButton = setBackBtn;
            setRoot.SetActive(false);

            // ---- about panel ----
            var aboutCard = MakeCard(canvasGo.transform, "AboutPanel", new Vector2(900, 1300), white, out GameObject aboutRoot);
            MakeLabel(aboutCard, "Title", "ABOUT", new Vector2(0, -70), new Vector2(860, 80),
                64, Cream, TextAnchor.MiddleCenter, font, new Vector2(0.5f, 1f), true);
            var credits = MakeLabel(aboutCard, "Credits", "", new Vector2(0, -170), new Vector2(780, 540),
                31, new Color(0.92f, 0.88f, 0.78f), TextAnchor.UpperCenter, font, new Vector2(0.5f, 1f), false);
            credits.horizontalOverflow = HorizontalWrapMode.Wrap;
            credits.verticalOverflow = VerticalWrapMode.Overflow;
            var rateBtn = MakeButton(aboutCard, "BtnRate", "RATE ON GOOGLE PLAY", new Vector2(0, -780), new Vector2(560, 96), white, Amber, Cream, 34, font);
            var shareBtn = MakeButton(aboutCard, "BtnShare", "SHARE WITH A FRIEND", new Vector2(0, -900), new Vector2(560, 96), white, BlueBtn, Cream, 34, font);
            var supportBtn = MakeButton(aboutCard, "BtnSupport", "SUPPORT US (AD)", new Vector2(0, -1020), new Vector2(560, 96), white, SlateBtn, Cream, 34, font);
            var supportLabel = supportBtn.GetComponentInChildren<Text>();
            var aboutBackBtn = MakeButton(aboutCard, "BtnBack", "BACK", new Vector2(0, -1140), new Vector2(560, 96), white, SlateBtn, Cream, 40, font);

            var about = aboutRoot.AddComponent<AboutPanel>();
            about.rateButton = rateBtn;
            about.shareButton = shareBtn;
            about.supportAdButton = supportBtn;
            about.supportLabel = supportLabel;
            about.backButton = aboutBackBtn;
            about.creditsText = credits;
            aboutRoot.SetActive(false);

            // ---- controller ----
            var menu = canvasGo.AddComponent<MainMenuController>();
            menu.mainPanel = mainPanel.gameObject;
            menu.namePanel = nameRoot;
            menu.settingsPanel = setRoot;
            menu.aboutPanel = aboutRoot;
            menu.newGameButton = newBtn;
            menu.continueButton = contBtn;
            menu.settingsButton = setBtn;
            menu.aboutButton = aboutBtn;
            menu.continueInfo = contInfo;
            menu.versionLabel = version;
            menu.nameInput = nameInput;
            menu.nameError = nameError;
            menu.beginButton = beginBtn;
            menu.nameBackButton = nameBackBtn;
            menu.settings = settings;
            menu.about = about;

            AddEventSystem();
            SaveScene(scene, $"{ScenesRoot}/MainMenu.unity");
        }

        // ================================================================== cinematic scene (letterbox)

        static void BuildCinematicScene(Sprite white, Sprite scrim)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            var canvasGo = NewCanvas("Canvas");
            var font = BuiltinFont();

            CanvasGroup MakeSlot(string name, float alpha, out Image back, out Image fore)
            {
                var slot = NewRect(name, canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var cg = slot.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = alpha;
                cg.interactable = false;
                cg.blocksRaycasts = false;

                var backGo = NewRect("Back", slot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                back = backGo.gameObject.AddComponent<Image>();
                back.raycastTarget = false;
                back.color = new Color(0.22f, 0.21f, 0.27f); // dark, desaturated fill

                var foreGo = NewRect("Fore", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 190), RefRes);
                fore = foreGo.gameObject.AddComponent<Image>();
                fore.raycastTarget = false;
                fore.color = Color.white;
                return cg;
            }

            var slotB = MakeSlot("SlotB", 0f, out Image backB, out Image foreB);
            var slotA = MakeSlot("SlotA", 1f, out Image backA, out Image foreA);

            var scrimGo = NewRect("Scrim", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0, 430), new Vector2(0, 860));
            var scrimImg = scrimGo.gameObject.AddComponent<Image>();
            scrimImg.sprite = scrim;
            scrimImg.raycastTarget = false;

            var caption = MakeLabel(canvasGo.transform, "Caption", "", new Vector2(0, 270), new Vector2(980, 420),
                46, Cream, TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0f), true);
            caption.horizontalOverflow = HorizontalWrapMode.Wrap;
            caption.verticalOverflow = VerticalWrapMode.Overflow;
            caption.lineSpacing = 1.15f;

            var skipBtn = MakeButton(canvasGo.transform, "BtnSkip", "SKIP", new Vector2(-110, -60), new Vector2(170, 80), white,
                new Color(0.08f, 0.10f, 0.16f, 0.6f), new Color(1f, 1f, 1f, 0.85f), 30, font);
            ((RectTransform)skipBtn.transform).anchorMin = new Vector2(1f, 1f);
            ((RectTransform)skipBtn.transform).anchorMax = new Vector2(1f, 1f);

            // panels: pack plates + generated lantern close-up, VO-synced
            string[] plateFiles = { "PlainA", "ForestB", "ForestA", "DungeonB", "ForestC", "PlainB" };
            var panels = new List<CinePanel>();
            for (int i = 0; i < 7; i++)
            {
                Sprite spr = i < 6
                    ? ImportPlate(plateFiles[i], $"cine_0{i + 1}_{plateFiles[i].ToLowerInvariant()}")
                    : AssetDatabase.LoadAssetAtPath<Sprite>($"{CineArtRoot}/cine_07_lantern.png");
                var vo = AssetDatabase.LoadAssetAtPath<AudioClip>($"{VoRoot}/prologue_0{i + 1}.mp3");
                if (vo == null) Debug.LogWarning($"[Shell] VO clip prologue_0{i + 1}.mp3 missing — panel {i + 1} will use timed fallback.");
                var p = new CinePanel
                {
                    image = spr,
                    vo = vo,
                    text = StoryLines.Prologue[i],
                    zoomFrom = i == 6 ? 1.14f : 1.03f,
                    zoomTo = i == 6 ? 1.02f : 1.12f,
                    panFrom = i % 2 == 0 ? new Vector2(-28, 12) : new Vector2(24, -16),
                    panTo = i % 2 == 0 ? new Vector2(28, -12) : new Vector2(-24, 16),
                };
                panels.Add(p);
            }

            var director = new GameObject("Director");
            var player = director.AddComponent<CinematicPlayer>();
            player.panels = panels;
            player.slotA = slotA;
            player.slotB = slotB;
            player.foreA = foreA;
            player.foreB = foreB;
            player.backA = backA;
            player.backB = backB;
            player.caption = caption;
            player.skipButton = skipBtn;
            player.nextScene = "Emberholt_Village";

            AddEventSystem();
            SaveScene(scene, $"{ScenesRoot}/Cinematic.unity");
        }

        // ================================================================== shared builders

        static void RegisterScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene($"{ScenesRoot}/Splash.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/Cinematic.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/Emberholt_Village.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/Interior_Elder.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/Interior_Home.unity", true),
                new EditorBuildSettingsScene($"{ScenesRoot}/Emberholt_Greybox.unity", true),
            };
        }

        static GameObject NewCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefRes;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        static void AddEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            UiSpriteGen.EnsureFolder(ScenesRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[Shell] Saved " + path);
        }

        static RectTransform MakeCard(Transform parent, string name, Vector2 size, Sprite panelSprite, out GameObject root)
        {
            root = NewRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var dim = root.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            var card = NewRect("Card", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var img = card.gameObject.AddComponent<Image>();
            img.sprite = panelSprite;
            img.type = Image.Type.Sliced;
            img.color = CardBg;
            return card;
        }

        static Slider MakeSliderRow(Transform card, string label, float y, Sprite white, Sprite handleSprite, Font font)
        {
            MakeLabel(card, label + "Label", label, new Vector2(-300, y), new Vector2(240, 56),
                34, Cream, TextAnchor.MiddleRight, font, new Vector2(0.5f, 1f), false);

            var root = NewRect(label + "Slider", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(85, y), new Vector2(470, 56));

            var bgGo = NewRect("Background", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0, 14));
            var bgImg = bgGo.gameObject.AddComponent<Image>();
            bgImg.sprite = white;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.05f, 0.07f, 0.12f);

            var fillArea = NewRect("Fill Area", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0, 14));
            var fillGo = NewRect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fillGo.gameObject.AddComponent<Image>();
            fillImg.sprite = white;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = new Color(0.85f, 0.55f, 0.20f);

            var handleArea = NewRect("Handle Slide Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handleArea.offsetMin = new Vector2(22, 0);
            handleArea.offsetMax = new Vector2(-22, 0);
            var handleGo = NewRect("Handle", handleArea, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(46, 46));
            var handleImg = handleGo.gameObject.AddComponent<Image>();
            handleImg.sprite = handleSprite;
            handleImg.color = Color.white;

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fillGo;
            slider.handleRect = handleGo;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        static Toggle MakeToggle(Transform card, Vector2 pos, Sprite box, Sprite checkSprite)
        {
            var root = NewRect("Toggle", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(64, 64));
            var bgImg = root.gameObject.AddComponent<Image>();
            bgImg.sprite = box;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.05f, 0.07f, 0.12f);
            var checkGo = NewRect("Checkmark", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34, 34));
            var checkImg = checkGo.gameObject.AddComponent<Image>();
            checkImg.sprite = checkSprite;
            checkImg.color = new Color(0.95f, 0.65f, 0.25f);
            checkImg.raycastTarget = false;
            var toggle = root.gameObject.AddComponent<Toggle>();
            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;
            return toggle;
        }

        static InputField MakeInput(Transform card, Vector2 pos, Vector2 size, Sprite bg, Font font, string placeholder)
        {
            var root = NewRect("NameInput", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, size);
            var img = root.gameObject.AddComponent<Image>();
            img.sprite = bg;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.05f, 0.07f, 0.12f);

            var textGo = NewRect("Text", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            textGo.offsetMin = new Vector2(26, 4);
            textGo.offsetMax = new Vector2(-26, -4);
            var txt = textGo.gameObject.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 44;
            txt.color = Cream;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.supportRichText = false;

            var phGo = NewRect("Placeholder", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            phGo.offsetMin = new Vector2(26, 4);
            phGo.offsetMax = new Vector2(-26, -4);
            var ph = phGo.gameObject.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = 44;
            ph.fontStyle = FontStyle.Italic;
            ph.color = new Color(1f, 1f, 1f, 0.28f);
            ph.alignment = TextAnchor.MiddleLeft;
            ph.text = placeholder;
            ph.raycastTarget = false;

            var input = root.gameObject.AddComponent<InputField>();
            input.textComponent = txt;
            input.placeholder = ph;
            input.characterLimit = 12;
            input.contentType = InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 size,
            Sprite sprite, Color fill, Color textColor, int fontSize, Font font)
        {
            var go = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = fill;
            var btn = go.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            cb.fadeDuration = 0.06f;
            btn.colors = cb;

            var txt = MakeLabel(go.transform, "Label", label, Vector2.zero, size, fontSize, textColor,
                TextAnchor.MiddleCenter, font, new Vector2(0.5f, 0.5f), true);
            txt.raycastTarget = false;
            return btn;
        }

        static Text MakeLabel(Transform parent, string name, string content, Vector2 pos, Vector2 size,
            int fontSize, Color color, TextAnchor align, Font font, Vector2 anchor, bool shadow)
        {
            var go = NewRect(name, parent, anchor, anchor, pos, size);
            var txt = go.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = align;
            txt.raycastTarget = false;
            if (shadow)
            {
                var sh = go.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.65f);
                sh.effectDistance = new Vector2(0, -3);
            }
            return txt;
        }

        static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static Font cachedFont;
        static Font BuiltinFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }
    }
}
