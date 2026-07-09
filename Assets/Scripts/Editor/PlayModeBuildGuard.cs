using System;

using UnityEditor;
using UnityEditor.Build;

namespace SplitRun.EditorTools
{
    // One registration path for the "abort Play / fail the build" invariant guards.
    public static class PlayModeBuildGuard
    {
        /// <summary>
        /// Runs <paramref name="report"/> when edit mode exits and issues exist. When
        /// <paramref name="blocksPlay"/> the Play transition is cancelled before it starts.
        /// </summary>
        public static void RegisterPlayModeCheck(Func<bool> hasIssues, Action report, bool blocksPlay)
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingEditMode) return;
                if (!hasIssues()) return;

                // Cancelling isPlaying here aborts the transition, so Play never actually starts.
                if (blocksPlay)
                    EditorApplication.isPlaying = false;

                report();
            };
        }

        public static void FailBuild(int issueCount, string what, string where) =>
            throw new BuildFailedException($"{issueCount} {what}. See the {where}.");
    }
}
