using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using LittleEmber.Core;

namespace LittleEmber.Audio
{
    /// <summary>
    /// Persistent audio bus: Music / SFX / VO / Ambient sources with volumes
    /// driven by SettingsData. Self-bootstraps before the first scene; survives
    /// loads. Named clips live in Assets/Resources/Audio (loaded+cached on first
    /// use). Music crossfades on scene change; DuckMusic softens it under dialogue.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource _music;
        AudioSource[] _sfxPool;
        int _sfxNext;
        AudioSource _vo;
        AudioSource _amb;

        static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        Coroutine _musicFade;
        bool _ducked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[AudioManager]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioManager>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _music = MakeSource("Music");
            _sfxPool = new AudioSource[4];
            for (int i = 0; i < _sfxPool.Length; i++) _sfxPool[i] = MakeSource("SFX_" + i);
            _vo = MakeSource("VO");
            _amb = MakeSource("Ambient");
            _music.loop = true;
            _amb.loop = true;

            SettingsData.Changed += ApplyVolumes;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyVolumes();
            PlayForScene(SceneManager.GetActiveScene().name, instant: true);
        }

        void OnDestroy()
        {
            SettingsData.Changed -= ApplyVolumes;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        AudioSource MakeSource(string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        void ApplyVolumes()
        {
            float m = SettingsData.Master;
            if (_music != null) _music.volume = m * SettingsData.Music * (_ducked ? 0.3f : 1f);
            if (_sfxPool != null)
                foreach (var s in _sfxPool) if (s != null) s.volume = m * SettingsData.Sfx;
            if (_vo != null) _vo.volume = m * SettingsData.Vo;
            if (_amb != null) _amb.volume = m * SettingsData.Sfx * 0.8f;
        }

        // ------------------------------------------------------------- clip cache

        static AudioClip ClipFor(string path)
        {
            if (_cache.TryGetValue(path, out var c)) return c;
            c = Resources.Load<AudioClip>("Audio/" + path);
            if (c == null) Debug.LogWarning("[Audio] Missing clip: Resources/Audio/" + path);
            _cache[path] = c;
            return c;
        }

        // ------------------------------------------------------------------ VO

        public static void PlayVO(AudioClip clip)
        {
            if (Instance == null || clip == null || Instance._vo == null) return;
            Instance._vo.Stop();
            Instance._vo.clip = clip;
            Instance._vo.Play();
        }

        public static void StopVO()
        {
            if (Instance != null && Instance._vo != null) Instance._vo.Stop();
        }

        // ------------------------------------------------------------------ SFX

        public static void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (Instance == null || clip == null || Instance._sfxPool == null) return;
            var src = NextSfx();
            src.pitch = 1f;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>Play a clip from Resources/Audio/Sfx/&lt;name&gt; by name.</summary>
        public static void PlaySfxName(string name, float volume = 1f)
        {
            if (Instance == null || string.IsNullOrEmpty(name)) return;
            var clip = ClipFor("Sfx/" + name);
            if (clip == null) return;
            float v = volume * Random.Range(0.92f, 1.05f); // subtle per-play variance
            var src = NextSfx();
            src.pitch = Random.Range(0.95f, 1.06f);
            src.PlayOneShot(clip, v);
        }

        AudioSource NextSfx()
        {
            var src = _sfxPool[_sfxNext];
            _sfxNext = (_sfxNext + 1) % _sfxPool.Length;
            return src;
        }

        // ------------------------------------------------------------------ Music

        public static void PlayMusic(AudioClip clip)
        {
            if (Instance == null || clip == null) return;
            Instance.FadeToNewTrack(clip);
        }

        /// <summary>Play a music loop from Resources/Audio/Music/&lt;name&gt; by name.</summary>
        public static void PlayMusicName(string name)
        {
            if (Instance == null || string.IsNullOrEmpty(name)) return;
            PlayMusic(ClipFor("Music/" + name));
        }

        /// <summary>Soften music under dialogue/cinematics; restore on release.</summary>
        public static void DuckMusic(bool duck)
        {
            if (Instance == null || Instance._ducked == duck) return;
            Instance._ducked = duck;
            Instance.ApplyVolumes();
        }

        void FadeToNewTrack(AudioClip clip)
        {
            if (_music == null) return;
            if (_music.clip == clip && _music.isPlaying) return;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(Crossfade(clip));
        }

        IEnumerator Crossfade(AudioClip next)
        {
            float baseVol = SettingsData.Master * SettingsData.Music;
            float t = 0f;
            const float dur = 0.9f;
            float startVol = _music.volume;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _music.volume = Mathf.Lerp(startVol, 0f, t / dur);
                yield return null;
            }
            _music.Stop();
            _music.clip = next;
            if (next == null) { ApplyVolumes(); yield break; }
            _music.Play();
            t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _music.volume = Mathf.Lerp(0f, baseVol * (_ducked ? 0.3f : 1f), t / dur);
                yield return null;
            }
            ApplyVolumes();
            _musicFade = null;
        }

        // ------------------------------------------------------------------ Ambient

        /// <summary>Looping ambience bed (birds, hearth crackle) from Resources/Audio/Ambient/&lt;name&gt;.</summary>
        public static void PlayAmbient(string name)
        {
            if (Instance == null || Instance._amb == null) return;
            var clip = string.IsNullOrEmpty(name) ? null : ClipFor("Ambient/" + name);
            if (Instance._amb.clip == clip) return;
            Instance._amb.clip = clip;
            if (clip != null) Instance._amb.Play();
            else Instance._amb.Stop();
        }

        // ------------------------------------------------------------------ scene map

        void OnSceneLoaded(Scene s, LoadSceneMode mode) => PlayForScene(s.name, instant: false);

        void PlayForScene(string scene, bool instant)
        {
            string music, amb;
            switch (scene)
            {
                case "Emberholt_Village":
                case "Emberholt_Greybox":
                    music = "music_village"; amb = "amb_village"; break;
                case "Interior_Elder":
                case "Interior_Home":
                    music = "music_hearth"; amb = "amb_hearth"; break;
                case "MainMenu":
                case "Splash":
                    music = "music_title"; amb = null; break;
                case "Cinematic":
                    music = "music_prologue"; amb = null; break;
                default:
                    music = "music_village"; amb = null; break;
            }
            if (instant)
            {
                var clip = ClipFor("Music/" + music);
                _music.clip = clip;
                if (clip != null) _music.Play();
                ApplyVolumes();
            }
            else
            {
                PlayMusicName(music);
            }
            PlayAmbient(amb);
        }
    }
}
