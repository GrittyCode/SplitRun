using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Build;

namespace SplitRun.EditorTools
{
    // One registration path for the "abort Play / fail the build" invariant guards.
    public static class PlayModeBuildGuard
    {
        /// <summary>
        /// Scans once when edit mode exits and hands the result to <paramref name="report"/>.
        /// When <paramref name="blocksPlay"/> the Play transition is cancelled before it starts.
        /// </summary>
        public static void Register<T>(Func<List<T>> scan, Action<List<T>> report, bool blocksPlay)
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.ExitingEditMode) return;

                List<T> issues = scan();
                if (issues.Count == 0) return;

                // Cancelling isPlaying here aborts the transition, so Play never actually starts.
                if (blocksPlay)
                    EditorApplication.isPlaying = false;

                report(issues);
            };
        }

        public static void FailBuild(int issueCount, string what, string where) =>
            throw new BuildFailedException($"{issueCount} {what}. See the {where}.");
    }
}
