using UnityEditor;
using UnityEngine;

namespace TerrainGeneration
{
    [CustomEditor(typeof(TerrainGenerator))]
    public class TerrainGeneratorEditor : Editor
    {
        private TerrainGenerator terrainGen;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Refresh")) terrainGen.Generate();
        }

        private void OnEnable()
        {
            terrainGen = (TerrainGenerator) target;
        }
    }
}