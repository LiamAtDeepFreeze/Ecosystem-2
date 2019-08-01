using UnityEngine;
using Random = System.Random;

namespace TerrainGeneration
{
    public static class HeightmapGenerator
    {
        public static float[,] GenerateHeightmap(NoiseSettings noiseSettings, int size, bool normalize = true)
        {
            var map = new float[size, size];
            var prng = new Random(noiseSettings.seed);
            var noise = new Noise(noiseSettings.seed);

            // Generate random offset for each layer
            var offsets = new Vector2[noiseSettings.numLayers];
            for (var layer = 0; layer < noiseSettings.numLayers; layer++)
                offsets[layer] = new Vector2((float) prng.NextDouble() * 2 - 1, (float) prng.NextDouble() * 2 - 1) *
                                 10000;
            // offsets[layer] += noiseSettings.offset;

            var minHeight = float.MaxValue;
            var maxHeight = float.MinValue;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var frequency = noiseSettings.scale;
                float amplitude = 1;
                float height = 0;

                // Sum layers of noise
                for (var layer = 0; layer < noiseSettings.numLayers; layer++)
                {
                    var sampleX = (double) x / size * frequency + offsets[layer].x + noiseSettings.offset.x;
                    var sampleY = (double) y / size * frequency + offsets[layer].y + noiseSettings.offset.y;
                    height += (float) noise.Evaluate(sampleX, sampleY) * amplitude;
                    frequency *= noiseSettings.lacunarity;
                    amplitude *= noiseSettings.persistence;
                }

                map[x, y] = height;
                minHeight = Mathf.Min(minHeight, height);
                maxHeight = Mathf.Max(maxHeight, height);
            }

            // Normalize values to range 0-1
            if (normalize)
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    map[x, y] = Mathf.InverseLerp(minHeight, maxHeight, map[x, y]);

            return map;
        }
    }
}