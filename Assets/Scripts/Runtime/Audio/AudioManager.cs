using UnityEngine;
using LittleEmber.Core;

namespace LittleEmber.Audio
{
    /// <summary>
    /// Persistent audio bus: Music / SFX / VO sources with volumes driven by
    /// SettingsData. Self-bootstraps before the first scene; survives loads.
    /// No music ships yet (pack has no audio) — structure is ready for it.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        AudioSource _music;
        AudioSource _sfx;
        AudioSource _vo;

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
            _sfx = MakeSource("SFX");
            _vo = MakeSource("VO");
            _music.loop = true;

            SettingsData.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        void OnDestroy()
        {
            SettingsData.Changed -= ApplyVolumes;
        }

        AudioSource MakeSource(string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        void ApplyVolumes()
        {
            if (_music == null) return;
            _music.volume = SettingsData.Master * SettingsData.Music;
            _sfx.volume = SettingsData.Master * SettingsData.Sfx;
            _vo.volume = SettingsData.Master * SettingsData.Vo;
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
            if (Instance == null || clip == null || Instance._sfx == null) return;
            Instance._sfx.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------------ Music (stub until music exists)

        public static void PlayMusic(AudioClip clip)
        {
            if (Instance == null || clip == null || Instance._music == null) return;
            if (Instance._music.clip == clip && Instance._music.isPlaying) return;
            Instance._music.clip = clip;
            Instance._music.Play();
        }
    }
}
