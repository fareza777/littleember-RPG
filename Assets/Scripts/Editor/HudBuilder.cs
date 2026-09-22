using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using LittleEmber.Combat;
using LittleEmber.Player;
using LittleEmber.UI;

namespace LittleEmber.EditorTools
{
    /// <summary>
    /// Shared luxury HUD + dialogue canvas builder. Gem action buttons (gold rim,
    /// glass face), ornate joystick ring with amber gem knob, gold-framed hint
    /// card and dialogue box. Used by the village, greybox and interior scenes.
    /// </summary>
    public static class HudBuilder
    {
        public class HudRefs
        {
            public GameObject canvas;
            public DialogueUI dialogue;
            public ContextActionButton contextButton;
            public OnboardingHints hints;
            public VirtualJoystick joystick;
        }

        static Sprite _heartFull, _heartEmpty, _joyRing, _joyKnob, _gemRed, _gemBlue, _gemGreen,
            _iconFlame, _iconRoll, _goldPanel;

        public static void BakeSprites()
        {
            _joyRing = UiSpriteGen.OrnateRing("ui_joy_ring", 256);
            _joyKnob = UiSpriteGen.GemKnob("ui_joy_knob", 112);
            _gemRed = UiSpriteGen.GemButton("ui_gem_red", 128, new Color(0.72f, 0.20f, 0.12f));
            _gemBlue = UiSpriteGen.GemButton("ui_gem_blue", 128, new Color(0.14f, 0.32f, 0.66f));
            _gemGreen = UiSpriteGen.GemButton("ui_gem_green", 128, new Color(0.16f, 0.46f, 0.28f));
            // icons are hand-drawn by Tools\gen_icons.ps1 (bold AA glyphs) —
            // do NOT regenerate them procedurally here, just load what's on disk.
            _iconFlame = EnsureUiSprite("Assets/Art/UI/ui_icon_flame.png");
            _iconRoll = EnsureUiSprite("Assets/Art/UI/ui_icon_roll.png");
            _goldPanel = UiSpriteGen.GoldPanel("ui_panel_gold", 96, 22, new Color32(10, 14, 26, 242));
        }

        public static HudRefs Build(Health pipHealth, PipController pipCtl, string titleText, bool withHints)
        {
            BakeSprites();
            _heartFull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_heart_full.png");
            _heartEmpty = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/ui_heart_empty.png");
            var font = BuiltinFont();
            var refs = new HudRefs();

            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            refs.canvas = canvasGo;

            // ---------------- hearts ----------------
            var heartsGo = NewRect("Hearts", canvasGo.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -32), new Vector2(320, 96));
            heartsGo.pivot = new Vector2(0, 1);
            var heartImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var h = NewRect($"Heart_{i}", heartsGo.transform, new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(48 + i * 80, -48), new Vector2(64, 64));
                heartImgs[i] = h.gameObject.AddComponent<Image>();
                heartImgs[i].sprite = _heartFull;
                heartImgs[i].preserveAspect = true;
            }
            var hudHearts = canvasGo.AddComponent<HudHearts>();
            hudHearts.target = pipHealth;
            hudHearts.hearts = heartImgs;
            hudHearts.fullHeart = _heartFull;
            hudHearts.emptyHeart = _heartEmpty;

            // ---------------- ornate joystick ----------------
            var joyGo = NewRect("Joystick", canvasGo.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(230, 260), new Vector2(300, 300));
            var joyImg = joyGo.gameObject.AddComponent<Image>();
            joyImg.sprite = _joyRing;
            var knobGo = NewRect("Knob", joyGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(122, 122));
            var knobImg = knobGo.gameObject.AddComponent<Image>();
            knobImg.sprite = _joyKnob;
            knobImg.raycastTarget = false;
            var joystick = joyGo.gameObject.AddComponent<VirtualJoystick>();
            joystick.handle = knobGo;
            joystick.radius = 118f;
            refs.joystick = joystick;

            // ---------------- gem action buttons ----------------
            var atkBtn = GemActionButton(canvasGo.transform, "BtnAttack", _gemRed, _iconFlame,
                new Vector2(-168, 225), 205, font);
            var rollBtn = GemActionButton(canvasGo.transform, "BtnRoll", _gemBlue, _iconRoll,
                new Vector2(-190, 490), 150, font);

            // ---------------- context button (TALK / ENTER) ----------------
            var ctxGo = NewRect("BtnContext", canvasGo.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-185, 680), new Vector2(170, 170));
            var ctxGroup = ctxGo.gameObject.AddComponent<CanvasGroup>();
            ctxGroup.alpha = 0f;
            ctxGroup.interactable = false;
            ctxGroup.blocksRaycasts = false;
            var ctxImg = ctxGo.gameObject.AddComponent<Image>();
            ctxImg.sprite = _gemGreen;
            var ctxBtn = ctxGo.gameObject.AddComponent<TouchButton>();
            var ctxLabelGo = NewRect("Label", ctxGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 60));
            var ctxLabel = ctxLabelGo.gameObject.AddComponent<Text>();
            ctxLabel.text = "TALK";
            ctxLabel.font = font;
            ctxLabel.fontSize = 34;
            ctxLabel.fontStyle = FontStyle.Bold;
            ctxLabel.alignment = TextAnchor.MiddleCenter;
            ctxLabel.color = new Color(1f, 0.96f, 0.85f);
            ctxLabel.raycastTarget = false;
            var ctxShadow = ctxLabelGo.gameObject.AddComponent<Shadow>();
            ctxShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            ctxShadow.effectDistance = new Vector2(0, -2);
            var ctx = canvasGo.AddComponent<ContextActionButton>();
            ctx.pip = pipCtl;
            ctx.group = ctxGroup;
            ctx.label = ctxLabel;
            ctx.button = ctxBtn;
            refs.contextButton = ctx;

            // ---------------- location title ----------------
            if (!string.IsNullOrEmpty(titleText))
            {
                var title = NewRect("Title", canvasGo.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(900, 50));
                var titleTextC = title.gameObject.AddComponent<Text>();
                titleTextC.text = titleText;
                titleTextC.alignment = TextAnchor.MiddleCenter;
                titleTextC.fontSize = 30;
                titleTextC.color = new Color(1f, 0.9f, 0.7f, 0.5f);
                titleTextC.font = font;
                titleTextC.raycastTarget = false;
            }

            // ---------------- onboarding hint card ----------------
            if (withHints)
            {
                var cardGo = NewRect("HintCard", canvasGo.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 800), new Vector2(920, 190));
                var cardImg = cardGo.gameObject.AddComponent<Image>();
                cardImg.sprite = _goldPanel;
                cardImg.type = Image.Type.Sliced;
                cardGo.gameObject.AddComponent<Button>();
                var cardTxtGo = NewRect("Text", cardGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(860, 120));
                var cardTxt = cardTxtGo.gameObject.AddComponent<Text>();
                cardTxt.font = font;
                cardTxt.fontSize = 34;
                cardTxt.alignment = TextAnchor.MiddleCenter;
                cardTxt.color = new Color(1f, 0.94f, 0.82f);
                cardTxt.raycastTarget = false;
                var cardProgGo = NewRect("Progress", cardGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -72), new Vector2(860, 34));
                var cardProg = cardProgGo.gameObject.AddComponent<Text>();
                cardProg.font = font;
                cardProg.fontSize = 22;
                cardProg.alignment = TextAnchor.MiddleCenter;
                cardProg.color = new Color(1f, 1f, 1f, 0.45f);
                cardProg.raycastTarget = false;
                var hints = canvasGo.AddComponent<OnboardingHints>();
                hints.card = cardGo.gameObject;
                hints.hintText = cardTxt;
                hints.progressText = cardProg;
                hints.joystick = joystick;
                hints.pip = pipCtl;
                refs.hints = hints;
            }

            // ---------------- dialogue box ----------------
            refs.dialogue = BuildDialogue(canvasGo.transform, font, pipCtl);

            pipCtl.joystick = joystick;
            pipCtl.attackButton = atkBtn;
            pipCtl.rollButton = rollBtn;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            return refs;
        }

        static DialogueUI BuildDialogue(Transform canvasT, Font font, PipController pipCtl)
        {
            // full-screen tap catcher (advances dialogue); below the panel itself
            var dlgRoot = NewRect("Dialogue", canvasT, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var dlgBtn = dlgRoot.gameObject.AddComponent<Button>();
            dlgBtn.transition = Selectable.Transition.None;
            var dlgCatcher = dlgRoot.gameObject.AddComponent<Image>();
            dlgCatcher.color = new Color(0f, 0f, 0f, 0.35f); // dims the world a touch

            // bottom panel with gold frame
            var panel = NewRect("Panel", dlgRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1000, 420));
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.sprite = _goldPanel;
            panelImg.type = Image.Type.Sliced;
            panelImg.raycastTarget = false;

            // portrait (square, gold-framed, overlapping panel's left edge)
            var portFrame = NewRect("PortraitFrame", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120, 40), new Vector2(220, 220));
            var portFrameImg = portFrame.gameObject.AddComponent<Image>();
            portFrameImg.sprite = _goldPanel;
            portFrameImg.type = Image.Type.Sliced;
            portFrameImg.raycastTarget = false;
            var portGo = NewRect("Portrait", portFrame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(196, 196));
            var portImg = portGo.gameObject.AddComponent<Image>();
            portImg.preserveAspect = true;
            portImg.raycastTarget = false;

            var nameGo = NewRect("Name", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430, -70), new Vector2(520, 56));
            var nameTxt = nameGo.gameObject.AddComponent<Text>();
            nameTxt.font = font;
            nameTxt.fontSize = 36;
            nameTxt.fontStyle = FontStyle.Bold;
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.color = new Color(0.95f, 0.78f, 0.42f);
            nameTxt.raycastTarget = false;

            var bodyGo = NewRect("Body", panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(500, -230), new Vector2(880, 250));
            var bodyTxt = bodyGo.gameObject.AddComponent<Text>();
            bodyTxt.font = font;
            bodyTxt.fontSize = 40;
            bodyTxt.alignment = TextAnchor.UpperLeft;
            bodyTxt.color = new Color(0.98f, 0.94f, 0.84f);
            bodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            bodyTxt.lineSpacing = 1.12f;
            bodyTxt.raycastTarget = false;

            var hintGo = NewRect("Hint", panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-60, 36), new Vector2(160, 40));
            var hintTxt = hintGo.gameObject.AddComponent<Text>();
            hintTxt.text = "tap ▸";
            hintTxt.font = font;
            hintTxt.fontSize = 26;
            hintTxt.alignment = TextAnchor.MiddleRight;
            hintTxt.color = new Color(1f, 1f, 1f, 0.5f);
            hintTxt.raycastTarget = false;

            var dlg = dlgRoot.gameObject.AddComponent<DialogueUI>();
            dlg.panel = dlgRoot.gameObject;
            dlg.portraitImage = portImg;
            dlg.portraitRing = portFrameImg;
            dlg.nameText = nameTxt;
            dlg.bodyText = bodyTxt;
            dlg.continueHint = hintTxt;
            dlg.pip = pipCtl;
            // persistent listener — plain AddListener is runtime-only and would be lost on scene save
            UnityEditor.Events.UnityEventTools.AddPersistentListener(dlgBtn.onClick, dlg.OnPanelTapped);

            dlgRoot.gameObject.SetActive(false);
            return dlg;
        }

        static TouchButton GemActionButton(Transform parent, string name, Sprite gem, Sprite icon, Vector2 anchoredPos, int size, Font font)
        {
            var go = NewRect(name, parent, new Vector2(1, 0), new Vector2(1, 0), anchoredPos, new Vector2(size, size));
            var img = go.gameObject.AddComponent<Image>();
            img.sprite = gem;
            var btn = go.gameObject.AddComponent<TouchButton>();
            var iconGo = NewRect("Icon", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, size * 0.03f), new Vector2(size * 0.46f, size * 0.46f));
            var iconImg = iconGo.gameObject.AddComponent<Image>();
            iconImg.sprite = icon;
            iconImg.color = new Color(1f, 0.97f, 0.90f, 0.95f);
            iconImg.raycastTarget = false;
            var sh = iconGo.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.5f);
            sh.effectDistance = new Vector2(0, -3);
            return btn;
        }

        static Font cachedFont;
        public static Font BuiltinFont()
        {
            if (cachedFont != null) return cachedFont;
            cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }

        public static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
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

        /// <summary>Imports a PNG as a UI sprite if needed (portraits etc.).</summary>
        public static Sprite EnsureUiSprite(string path)
        {
            var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null) return spr;
            if (!System.IO.File.Exists(path)) return null;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 512; // portraits show at ~196px; saves texture memory on low-RAM phones
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
