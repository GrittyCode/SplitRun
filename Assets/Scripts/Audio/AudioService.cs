using System;

using UnityEngine;

using VContainer.Unity;

namespace SplitRun.Audio
{
    public sealed class AudioService : IStartable, IDisposable
    {
        private readonly AudioLibrary _library;

        private GameObject  _host;
        private AudioSource _sfxSource;
        private AudioSource _bgmSource;

        public AudioService(AudioLibrary library) => _library = library;

        public void Start()
        {
            _host = new GameObject("[Audio]");
            UnityEngine.Object.DontDestroyOnLoad(_host);

            _sfxSource             = _host.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _bgmSource             = _host.AddComponent<AudioSource>();
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

            if (_host)
                UnityEngine.Object.Destroy(_host);
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
