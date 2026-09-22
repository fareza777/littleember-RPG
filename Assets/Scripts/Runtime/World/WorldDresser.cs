using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using LittleEmber.Core;

namespace LittleEmber.World
{
    /// <summary>
    /// Runtime ambience layer. Committed scenes can't be re-baked without the editor,
    /// so this spawns hand-configured ParticleSystems per scene: ember motes and
    /// fireflies over Emberholt, chimney smoke on the houses, drifting petals,
    /// dust motes and candle-ember flicker indoors, rising embers on the menu.
    /// Emission rates scale down on the battery quality tier.
    /// </summary>
    public static class WorldDresser
    {
        static Material _matAdd, _matAlpha;
        static Texture2D _dot, _puff, _petal;
        static GameObject _root;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // safe under Enter-Play-Mode-without-domain-reload: never double-subscribe
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _root = null; _matAdd = null; _matAlpha = null;
            _dot = null; _puff = null; _petal = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply(SceneManager.GetActiveScene().name);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene.name);

        static void Apply(string scene)
        {
            EnsureAssets();
            _root = new GameObject("~WorldDresser");

            float q = QualityScale();

            if (scene == "Emberholt_Village")
            {
                // warm embers drifting on the wind, map-wide
                Emitter(_root.transform, new Vector3(0, 0, 0), new Spec
                {
                    box = new Vector3(48, 76, 1), rate = 6f * q, max = 90,
                    life = new MinMax(5f, 9f), speed = new MinMax(0.1f, 0.35f),
                    size = new MinMax(0.04f, 0.1f), color = new Color(1f, 0.62f, 0.28f, 0.85f),
                    drift = new Vector3(0.25f, 0.18f, 0), additive = true, order = 40, tex = _dot
                });
                // fireflies around the pond (15, 25) and the treelines
                Fireflies(new Vector3(15, 25, 0), new Vector3(14, 10, 1), 26 * q);
                Fireflies(new Vector3(-20, 8, 0), new Vector3(6, 60, 1), 20 * q);
                Fireflies(new Vector3(20, 8, 0), new Vector3(6, 60, 1), 20 * q);
                // chimney smoke over each house
                SmokeAt(new Vector3(-17.2f, -5.4f, 0));
                SmokeAt(new Vector3(-10.2f, 4.8f, 0));
                SmokeAt(new Vector3(11.8f, 6.8f, 0));
                SmokeAt(new Vector3(-11.2f, -18.2f, 0));
                SmokeAt(new Vector3(12.8f, -8.2f, 0));
                SmokeAt(new Vector3(-6.2f, 20.8f, 0));
                SmokeAt(new Vector3(7.8f, 21.8f, 0));
                SmokeAt(new Vector3(17.8f, -26.2f, 0));
                // festival petals on the breeze
                Emitter(_root.transform, new Vector3(0, 4, 0), new Spec
                {
                    box = new Vector3(40, 60, 1), rate = 1.4f * q, max = 40,
                    life = new MinMax(7f, 12f), speed = new MinMax(0.05f, 0.2f),
                    size = new MinMax(0.08f, 0.14f), color = new Color(1f, 0.82f, 0.88f, 0.9f),
                    drift = new Vector3(0.35f, -0.05f, 0), gravity = -0.015f,
                    spin = 60f, additive = false, order = 41, tex = _petal
                });
                EmberFlickerAtWarmLights(q);
            }
            else if (scene.StartsWith("Interior"))
            {
                // dust motes in the lamplight
                Emitter(_root.transform, new Vector3(0, 0, 0), new Spec
                {
                    box = new Vector3(10, 5.5f, 1), rate = 4f * q, max = 40,
                    life = new MinMax(4f, 8f), speed = new MinMax(0.02f, 0.08f),
                    size = new MinMax(0.015f, 0.04f), color = new Color(1f, 0.85f, 0.6f, 0.35f),
                    drift = new Vector3(0.05f, -0.03f, 0), additive = true, order = 40, tex = _dot
                });
                EmberFlickerAtWarmLights(q);
            }
            else if (scene == "MainMenu" || scene == "Splash" || scene == "Cinematic")
            {
                // embers rising through the frame, sized to whatever the camera sees
                var cam = Camera.main;
                if (cam != null)
                {
                    float h = cam.orthographic ? cam.orthographicSize * 2f : 12f;
                    float w = h * cam.aspect;
                    var pos = cam.transform.position;
                    pos.z = 0;
                    Emitter(_root.transform, pos, new Spec
                    {
                        box = new Vector3(w + 2f, h + 2f, 1), rate = 10f * q, max = 80,
                        life = new MinMax(3f, 6f), speed = new MinMax(0.4f, 1.1f),
                        size = new MinMax(0.05f, 0.14f), color = new Color(1f, 0.55f, 0.2f, 0.9f),
                        drift = new Vector3(0.3f, 0.6f, 0), additive = true, order = 40, tex = _dot
                    });
                }
            }
        }

        static float QualityScale()
        {
            return SettingsData.Quality == 0 ? 0.45f : (SettingsData.Quality == 1 ? 0.8f : 1f);
        }

        struct MinMax
        {
            public float min, max;
            public MinMax(float a, float b) { min = a; max = b; }
            public ParticleSystem.MinMaxCurve Curve() => new ParticleSystem.MinMaxCurve(min, max);
        }

        struct Spec
        {
            public Vector3 box;
            public float rate;
            public int max;
            public MinMax life, speed, size;
            public Color color;
            public Vector3 drift;
            public float gravity, spin, grow;
            public bool additive;
            public int order;
            public Texture2D tex;
            public bool blink;
        }

        static void Fireflies(Vector3 center, Vector3 box, float count)
        {
            Emitter(_root.transform, center, new Spec
            {
                box = box, rate = count / 8f, max = Mathf.CeilToInt(count),
                life = new MinMax(4f, 8f), speed = new MinMax(0.1f, 0.5f),
                size = new MinMax(0.05f, 0.1f), color = new Color(0.75f, 1f, 0.45f, 0.9f),
                drift = new Vector3(0.4f, 0.3f, 0), additive = true, order = 40,
                tex = _dot, blink = true
            });
        }

        static void SmokeAt(Vector3 pos)
        {
            Emitter(_root.transform, pos, new Spec
            {
                box = new Vector3(0.4f, 0.2f, 1), rate = 1.6f, max = 22,
                life = new MinMax(3.5f, 6f), speed = new MinMax(0.25f, 0.45f),
                size = new MinMax(0.3f, 0.6f), color = new Color(0.62f, 0.58f, 0.66f, 0.3f),
                drift = new Vector3(0.12f, 0.5f, 0), grow = 1.6f,
                additive = false, order = 39, tex = _puff
            });
        }

        // small ember puffs on every warm point light (lamps, torches, hearth fires)
        static void EmberFlickerAtWarmLights(float q)
        {
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l == null || l.lightType != Light2D.LightType.Point) continue;
                if (l.color.b > 0.6f) continue; // only warm flames, skip cool/moon fills
                Emitter(_root.transform, l.transform.position, new Spec
                {
                    box = new Vector3(0.3f, 0.3f, 1), rate = 2.2f * q, max = 12,
                    life = new MinMax(0.6f, 1.4f), speed = new MinMax(0.2f, 0.6f),
                    size = new MinMax(0.03f, 0.07f), color = new Color(1f, 0.5f, 0.15f, 0.9f),
                    drift = new Vector3(0.05f, 0.4f, 0), additive = true, order = 42, tex = _dot
                });
            }
        }

        static ParticleSystem Emitter(Transform parent, Vector3 pos, Spec s)
        {
            var go = new GameObject("fx_" + (s.additive ? "glow" : "puff"));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = s.life.Curve();
            main.startSpeed = s.speed.Curve();
            main.startSize = s.size.Curve();
            main.startColor = new ParticleSystem.MinMaxGradient(s.color);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = Mathf.Max(1, s.max);
            main.gravityModifier = s.gravity;
            main.prewarm = true;

            var emission = ps.emission;
            emission.rateOverTime = s.rate;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = s.box;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            if (s.blink)
            {
                // firefly pulse: quick fade in, double blink, fade out
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                        new GradientAlphaKey(0.25f, 0.45f), new GradientAlphaKey(1f, 0.6f),
                        new GradientAlphaKey(0.3f, 0.8f), new GradientAlphaKey(0f, 1f)
                    });
            }
            else
            {
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[]
                    {
                        new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f),
                        new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f)
                    });
            }
            col.color = grad;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(s.drift.x - 0.08f, s.drift.x + 0.08f);
            vel.y = new ParticleSystem.MinMaxCurve(s.drift.y - 0.08f, s.drift.y + 0.08f);

            if (s.grow > 0f)
            {
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f,
                    new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, s.grow)));
            }

            if (s.spin > 0f)
            {
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-s.spin * Mathf.Deg2Rad, s.spin * Mathf.Deg2Rad);
            }

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.None;
            r.sharedMaterial = s.additive ? MatFor(true, s.tex) : MatFor(false, s.tex);
            r.sortingOrder = s.order;
            r.minParticleSize = 0.001f;
            r.maxParticleSize = 0.5f;
            return ps;
        }

        static Material MatFor(bool additive, Texture tex)
        {
            var m = new Material(additive ? _matAdd : _matAlpha);
            m.mainTexture = tex;
            return m;
        }

        static void EnsureAssets()
        {
            if (_dot == null) _dot = MakeDot();
            if (_puff == null) _puff = MakePuff();
            if (_petal == null) _petal = MakePetal();
            if (_matAdd == null) _matAdd = MakeMaterial(true);
            if (_matAlpha == null) _matAlpha = MakeMaterial(false);
        }

        static Material MakeMaterial(bool additive)
        {
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            if (sh.name == "Universal Render Pipeline/Particles/Unlit")
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Cull", (float)CullMode.Off);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }

        static Texture2D MakeDot()
        {
            const int N = 32;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    px[y * N + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            t.name = "fx_dot";
            return t;
        }

        static Texture2D MakePuff()
        {
            const int N = 64;
            var rng = new System.Random(7);
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            float ox = (float)rng.NextDouble() * 100f, oy = (float)rng.NextDouble() * 100f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(N / 2f, N / 2f)) / (N / 2f);
                    float n = Mathf.PerlinNoise(ox + x * 0.14f, oy + y * 0.14f);
                    float a = Mathf.Clamp01(1f - d) * Mathf.Lerp(0.5f, 1f, n);
                    px[y * N + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            t.name = "fx_puff";
            return t;
        }

        static Texture2D MakePetal()
        {
            const int N = 16;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f - N / 2f) / (N * 0.24f);
                    float v = (y + 0.5f - N / 2f) / (N * 0.4f);
                    float a = Mathf.Clamp01(1f - (u * u + v * v));
                    px[y * N + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px);
            t.Apply();
            t.filterMode = FilterMode.Bilinear;
            t.name = "fx_petal";
            return t;
        }
    }
}
