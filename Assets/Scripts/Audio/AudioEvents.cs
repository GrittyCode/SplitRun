using System;

namespace SplitRun.Audio
{
    public static class AudioEvents
    {
        public static event Action<SfxType> OnSfxRequested;
        public static event Action<BgmType> OnBgmRequested;

        public static void RequestSfx(SfxType type) => OnSfxRequested?.Invoke(type);

        public static void RequestBgm(BgmType type) => OnBgmRequested?.Invoke(type);
    }
}
