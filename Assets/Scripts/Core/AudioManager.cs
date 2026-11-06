using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EcoBot
{
    /// <summary>Todos os efeitos sonoros do jogo.</summary>
    public enum Sfx
    {
        Jump, Land, Collect, ExtraLife, Energy, Hurt, Stomp, EnemyHit,
        Checkpoint, Crumble, LevelComplete, GameOver, UiMove, UiSelect, Pause
    }

    [Serializable]
    public class SfxClip
    {
        public Sfx id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Varia levemente o tom a cada execução para o som não ficar repetitivo.")]
        public bool randomPitch;
    }

    /// <summary>
    /// Toca música (com transição suave) e efeitos sonoros.
    /// Uso: AudioManager.Play(Sfx.Jump);  AudioManager.PlayMusic(clip);
    /// Tecla M liga/desliga o som.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;
        [SerializeField] SfxClip[] effects = Array.Empty<SfxClip>();
        [SerializeField] AudioClip menuMusic;
        [SerializeField] AudioClip victoryMusic;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;
        [SerializeField] float musicFadeTime = 0.6f;

        /// <summary>Avisos para o modo gravação (GameRecorder): (clip, volume, pitch).</summary>
        public static event Action<AudioClip, float, float> SfxPlayed;
        /// <summary>Música começou (clip) ou parou (null).</summary>
        public static event Action<AudioClip> MusicChanged;

        readonly Dictionary<Sfx, SfxClip> lookup = new Dictionary<Sfx, SfxClip>();
        Coroutine musicRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            foreach (var e in effects)
                if (e != null && e.clip != null) lookup[e.id] = e;
        }

        void Update()
        {
            if (GameInput.MutePressed) AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
        }

        // ------------------------------------------------------------ API estática
        public static void Play(Sfx id)
        {
            if (Instance != null) Instance.PlayInternal(id);
        }

        public static void PlayMusic(AudioClip clip)
        {
            if (Instance != null) Instance.PlayMusicInternal(clip);
        }

        public static void PlayMenuMusic()
        {
            if (Instance != null) Instance.PlayMusicInternal(Instance.menuMusic);
        }

        public static void PlayVictoryMusic()
        {
            if (Instance != null) Instance.PlayMusicInternal(Instance.victoryMusic);
        }

        public static void StopMusic()
        {
            if (Instance != null) Instance.PlayMusicInternal(null);
        }

        // ------------------------------------------------------------ implementação
        void PlayInternal(Sfx id)
        {
            if (!lookup.TryGetValue(id, out var e)) return;
            sfxSource.pitch = e.randomPitch ? UnityEngine.Random.Range(0.92f, 1.08f) : 1f;
            sfxSource.PlayOneShot(e.clip, e.volume * sfxVolume);
            SfxPlayed?.Invoke(e.clip, e.volume * sfxVolume, sfxSource.pitch);
        }

        void PlayMusicInternal(AudioClip clip)
        {
            if (musicSource.clip == clip && (clip == null || musicSource.isPlaying)) return;
            if (musicRoutine != null) StopCoroutine(musicRoutine);
            musicRoutine = StartCoroutine(CrossFade(clip));
        }

        IEnumerator CrossFade(AudioClip next)
        {
            float start = musicSource.volume;
            for (float t = 0; t < musicFadeTime && musicSource.isPlaying; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(start, 0f, t / musicFadeTime);
                yield return null;
            }
            musicSource.Stop();
            musicSource.clip = next;
            MusicChanged?.Invoke(next);
            if (next == null) yield break;

            musicSource.loop = true;
            musicSource.Play();
            for (float t = 0; t < musicFadeTime; t += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(0f, musicVolume, t / musicFadeTime);
                yield return null;
            }
            musicSource.volume = musicVolume;
        }
    }
}
