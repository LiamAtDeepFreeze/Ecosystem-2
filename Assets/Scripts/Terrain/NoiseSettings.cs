using System;
using UnityEngine;

namespace TerrainGeneration
{
    [Serializable]
    public class NoiseSettings
    {
        public float lacunarity = 2;

        [Range(1, 8)] public int numLayers = 4;

        public Vector2 offset;
        public float persistence = 0.5f;
        public float scale = 1;
        public int seed;
    }
}