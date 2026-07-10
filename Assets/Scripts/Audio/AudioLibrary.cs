using UnityEngine;

using SplitRun.Utility;

namespace SplitRun.Audio
{
    public enum SfxType
    {
        Hit            = 0,
        Coin           = 1,
        Magnet         = 2,
        LaneChange     = 3,
        Jump           = 4,
        Slide          = 5,
        ShieldActivate = 6,
        DashActivate   = 7,
        GameOver       = 8,
    }

    public enum BgmType
    {
        Lobby = 0,
        Game  = 1,
    }

    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "SplitRun/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.5f;

        [Header("Clips")]
        [SerializeField] private EnumKeyedArray<SfxType, AudioClip> _sfxClips = new EnumKeyedArray<SfxType, AudioClip>();
        [SerializeField] private EnumKeyedArray<BgmType, AudioClip> _bgmClips = new EnumKeyedArray<BgmType, AudioClip>();

        public float SfxVolume => _sfxVolume;
        public float BgmVolume => _bgmVolume;

        public AudioClip ClipFor(SfxType type) => _sfxClips[type];

        public AudioClip ClipFor(BgmType type) => _bgmClips[type];
    }
}
