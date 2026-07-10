using UnityEngine.UI;

using R3;

using SplitRun.Audio;

namespace SplitRun.UI
{
    public static class ButtonExtensions
    {
        /// <summary>Click stream that also raises the shared UI click cue.</summary>
        public static Observable<Unit> OnClickWithSfx(this Button button) =>
            button.OnClickAsObservable().Do(static _ => AudioEvents.RequestSfx(SfxType.Click));
    }
}
