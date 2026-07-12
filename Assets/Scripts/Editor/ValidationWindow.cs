using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace SplitRun.EditorTools
{
    // Modeless so the author can fix assets while it stays open, then Re-validate to clear them.
    public abstract class ValidationWindow<TWindow, TItem> : EditorWindow
        where TWindow : ValidationWindow<TWindow, TItem>
    {
        private List<TItem> _items = new List<TItem>();
        private Vector2     _scroll;

        protected IReadOnlyList<TItem> Items => _items;

        protected abstract string  WindowTitle    { get; }
        protected abstract Vector2 WindowMinSize  { get; }
        protected abstract string  EmptyMessage   { get; }
        protected abstract string  ProblemMessage { get; }

        protected abstract List<TItem> Collect();
        protected abstract bool        IsAlive(TItem item);
        protected abstract void        DrawRow(TItem item);

        protected virtual MessageType ProblemSeverity => MessageType.Error;

        protected virtual void OnBeforeRows() { }

        protected static void ShowWith(List<TItem> items)
        {
            var window = GetWindow<TWindow>(utility: false);
            window._items       = items;
            window.titleContent = new GUIContent(window.WindowTitle);
            window.minSize      = window.WindowMinSize;
            window.Show();
            window.Focus();
        }

        private void OnGUI()
        {
            PruneDestroyed();

            EditorGUILayout.Space();

            if (GUILayout.Button("Re-validate"))
                _items = Collect();

            EditorGUILayout.Space();

            if (_items.Count == 0)
            {
                EditorGUILayout.HelpBox(EmptyMessage, MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(ProblemMessage, ProblemSeverity);
            EditorGUILayout.Space();

            OnBeforeRows();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (TItem item in _items)
                DrawRow(item);
            EditorGUILayout.EndScrollView();
        }

        private void PruneDestroyed()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (!IsAlive(_items[i])) _items.RemoveAt(i);
            }
        }

        protected static void DrawPing(Object target)
        {
            if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                EditorGUIUtility.PingObject(target);
        }
    }
}
