using System;

namespace SplitRun.Audio
{
    // Bridges audio raise points (services, UI, runtime-spawned characters) to AudioService without DI coupling.
    public static class AudioEvents
    {
        public static event Action<SfxType> OnSfxRequested;
        public static event Action<BgmType> OnBgmRequested;

        /// <summary>Requests a one-shot SFX. No-op when no AudioService listens or the clip is unassigned.</summary>
        public static void RequestSfx(SfxType type) => OnSfxRequested?.Invoke(type);

        /// <summary>Requests the looping BGM track. Re-requesting the playing track keeps it running unbroken.</summary>
        public static void RequestBgm(BgmType type) => OnBgmRequested?.Invoke(type);
    }
}
