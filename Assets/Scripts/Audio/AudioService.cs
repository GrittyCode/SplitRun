using System;

using UnityEngine;

using VContainer.Unity;

namespace SplitRun.Audio
{
    // Plays audio raised through AudioEvents. Root-scoped with a persistent host so sound carries
    // across scene loads; an unassigned clip is a silent no-op.
    public sealed class AudioService : IStartable, IDisposable
    {
        private readonly AudioLibrary _library;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        public AudioService(AudioLibrary library) => _library = library;

        public void Start()
        {
            GameObject host = new GameObject("[Audio]");
            UnityEngine.Object.DontDestroyOnLoad(host);

            _sfxSource             = host.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _bgmSource             = host.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop        = true;
            _bgmSource.volume      = _library ? _library.BgmVolume : 0f;

            AudioEvents.OnSfxRequested += PlaySfx;
            AudioEvents.OnBgmRequested += PlayBgm;
        }

        public void Dispose()
        {
            AudioEvents.OnSfxRequested -= PlaySfx;
            AudioEvents.OnBgmRequested -= PlayBgm;

            if (_sfxSource)
                UnityEngine.Object.Destroy(_sfxSource.gameObject);
        }

        private void PlaySfx(SfxType type)
        {
            if (!_library) return;

            AudioClip clip = _library.ClipFor(type);
            if (!clip) return;

            _sfxSource.PlayOneShot(clip, _library.SfxVolume);
        }

        private void PlayBgm(BgmType type)
        {
            if (!_library) return;

            AudioClip clip = _library.ClipFor(type);

            // Re-entering a scene that already owns the playing track must not restart it mid-loop.
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmSource.clip = clip;

            if (clip)
                _bgmSource.Play();
            else
                _bgmSource.Stop();
        }
    }
}
