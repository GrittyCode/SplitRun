using System;

using UnityEngine;

namespace SplitRun.Audio
{
    // Separate types so a BGM track cannot be dispatched through PlayOneShot.
    public enum SfxType
    {
        Hit,
        Coin,
        Magnet,
        LaneChange,
        Jump,
        Slide,
        ShieldActivate,
        DashActivate,
        GameOver,
    }

    public enum BgmType
    {
        Lobby,
        Game,
    }

    // An unassigned clip plays nothing, so the system ships before the clips do.
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "SplitRun/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.5f;

        [Header("Clips")]
        [SerializeField] private SfxEntry[] _sfxEntries = Array.Empty<SfxEntry>();
        [SerializeField] private BgmEntry[] _bgmEntries = Array.Empty<BgmEntry>();

        public float SfxVolume => _sfxVolume;
        public float BgmVolume => _bgmVolume;

        public AudioClip ClipFor(SfxType type)
        {
            foreach (SfxEntry entry in _sfxEntries)
            {
                if (entry.Type == type) return entry.Clip;
            }

            return null;
        }

        public AudioClip ClipFor(BgmType type)
        {
            foreach (BgmEntry entry in _bgmEntries)
            {
                if (entry.Type == type) return entry.Clip;
            }

            return null;
        }

        [Serializable]
        private struct SfxEntry
        {
            [SerializeField] private SfxType   _type;
            [SerializeField] private AudioClip _clip;

            public SfxType   Type => _type;
            public AudioClip Clip => _clip;
        }

        [Serializable]
        private struct BgmEntry
        {
            [SerializeField] private BgmType   _type;
            [SerializeField] private AudioClip _clip;

            public BgmType   Type => _type;
            public AudioClip Clip => _clip;
        }
    }
}
