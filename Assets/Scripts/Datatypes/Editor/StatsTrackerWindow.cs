using System;
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;

namespace Datatypes.Editor
{
    public class StatsTrackerWindow : EditorWindow
    {
        private static StatsTrackerWindow _window;
        public enum Mode
        {
            Stats,
            Environment,
            Settings
        }

        private const float SidebarWidth = 50;
        private Vector2 _scrollPosMain;

        private static Mode mode;

        [MenuItem("Window/Stats/Stats Tracker")]
        public static void Initialize()
        {
            _window = GetWindow<StatsTrackerWindow>();
            _window.titleContent = new GUIContent("Stats Tracker");
            _window.Show();
        }

        private void OnEnable()
        {

        }

        private void OnGUI()
        {
            using (new HorizontalBlock())
            {
                DrawSidebar();
                using (new VerticalBlock())
                {
                    DrawMainDisplay();
                }
            }
        }

        private void DrawSidebar()
        {
            using (new VerticalBlock(GUILayout.Width(SidebarWidth)))
            {
                GUILayout.Space(4);
                if (GUILayout.Button("Stats", GUILayout.Height(SidebarWidth)))
                {

                }

                if (GUILayout.Button("", GUILayout.Height(SidebarWidth)))
                {

                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Settings", GUILayout.Height(SidebarWidth)))
                {

                }
                GUILayout.Space(4);
            }
        }

        private void DrawMainDisplay()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayoutHelper.CenteredMessage("Simulation In-Active");
                return;
            }

            switch(mode)
            {
                case Mode.Stats:
                DrawStatsWindow();
                break;

                case Mode.Environment:
                DrawEnvironmentWindow();
                break;

                case Mode.Settings:
                DrawSettingsWindow();
                break;
            }
        }

        private void DrawStatsWindow()
        {
            using(new VerticalBlock())
            {
                using(new VerticalBlock(EditorStyles.helpBox))
                {

                }

                using(new VerticalBlock(EditorStyles.helpBox))
                {
                    using (new ScrollviewBlock(ref _scrollPosMain))
                    {
                        foreach(var entry in StatsTracker.EntityPopulations.Values)
                        {
                            DrawStatsElement(entry);
                        }
                    }

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void DrawStatsElement(StatsEntry entry)
        {
            using(new HorizontalBlock(EditorStyles.helpBox, GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                GUILayout.Label(entry.id);
                GUILayout.FlexibleSpace();
                GUILayout.Label(entry.count.ToString());
            }
        }

        private void DrawEnvironmentWindow()
        {

        }

        private void DrawSettingsWindow()
        {

        }
    }
}
