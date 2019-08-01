using System;
using Toolbox.Editor;
using UnityEditor;
using UnityEngine;

namespace Datatypes.Editor
{
    public class StatsTrackerWindow : EditorWindow
    {
        private static StatsTrackerWindow _window;

        private const float SidebarWidth = 50;

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
                    DrawCategoryList();
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

        private void DrawCategoryList()
        {

        }

        private void DrawMainDisplay()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayoutHelper.CenteredMessage("Simulation In-Active");
                return;
            }
        }
    }
}
