using System;
using System.Collections.Generic;
using Datatypes;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainGeneration
{
    [ExecuteInEditMode]
    public class TerrainGenerator : MonoBehaviour
    {
        private const string MeshHolderName = "Terrain Mesh";

        public bool autoUpdate = true;

        public bool centralize = true;
        public float edgeDepth = .2f;
        public Biome grass;
        public Material mat;
        private Mesh mesh;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        private bool needsUpdate;
        public int numLandTiles;

        [Header("Info")]
        public int numTiles;

        public int numWaterTiles;
        public Biome sand;

        public NoiseSettings terrainNoise;

        public Biome water;
        public float waterDepth = .2f;
        public float waterPercent;
        public int worldSize = 20;

        private void Update()
        {
            if (needsUpdate && autoUpdate)
            {
                needsUpdate = false;
                Generate();
            }
            else
            {
                if (!Application.isPlaying) UpdateColours();
            }
        }

        public TerrainData Generate()
        {
            CreateMeshComponents();

            var numTilesPerLine = Mathf.CeilToInt(worldSize);
            var min = centralize ? -numTilesPerLine / 2f : 0;
            var map = HeightmapGenerator.GenerateHeightmap(terrainNoise, numTilesPerLine);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();

            // Some convenience stuff:
            var biomes = new[] {water, sand, grass};
            Vector3[] upVectorX4 = {Vector3.up, Vector3.up, Vector3.up, Vector3.up};
            Coord[] nswe = {Coord.Up, Coord.Down, Coord.Left, Coord.Right};
            int[][] sideVertIndexByDir = {new[] {0, 1}, new[] {3, 2}, new[] {2, 0}, new[] {1, 3}};
            Vector3[] sideNormalsByDir = {Vector3.forward, Vector3.back, Vector3.left, Vector3.right};

            // Terrain data:
            var terrainData = new TerrainData(numTilesPerLine);
            numLandTiles = 0;
            numWaterTiles = 0;

            for (var y = 0; y < numTilesPerLine; y++)
            for (var x = 0; x < numTilesPerLine; x++)
            {
                var uv = GetBiomeInfo(map[x, y], biomes);
                uvs.AddRange(new[] {uv, uv, uv, uv});

                var isWaterTile = uv.x == 0f;
                var isLandTile = !isWaterTile;
                if (isWaterTile)
                    numWaterTiles++;
                else
                    numLandTiles++;

                // Vertices
                var vertIndex = vertices.Count;
                var height = isWaterTile ? -waterDepth : 0;
                var nw = new Vector3(min + x, height, min + y + 1);
                var ne = nw + Vector3.right;
                var sw = nw - Vector3.forward;
                var se = sw + Vector3.right;
                Vector3[] tileVertices = {nw, ne, sw, se};
                vertices.AddRange(tileVertices);
                normals.AddRange(upVectorX4);

                // Add triangles
                triangles.Add(vertIndex);
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex + 3);
                triangles.Add(vertIndex + 2);

                // Bridge gaps between water and land tiles, and also fill in sides of map
                var isEdgeTile = x == 0 || x == numTilesPerLine - 1 || y == 0 || y == numTilesPerLine - 1;
                if (isLandTile || isEdgeTile)
                    for (var i = 0; i < nswe.Length; i++)
                    {
                        var neighbourX = x + nswe[i].x;
                        var neighbourY = y + nswe[i].y;
                        var neighbourIsOutOfBounds = neighbourX < 0 || neighbourX >= numTilesPerLine ||
                                                     neighbourY < 0 || neighbourY >= numTilesPerLine;
                        var neighbourIsWater = false;
                        if (!neighbourIsOutOfBounds)
                        {
                            var neighbourHeight = map[neighbourX, neighbourY];
                            neighbourIsWater = neighbourHeight <= biomes[0].height;
                            if (neighbourIsWater) terrainData.shore[neighbourX, neighbourY] = true;
                        }

                        if (neighbourIsOutOfBounds || isLandTile && neighbourIsWater)
                        {
                            var depth = waterDepth;
                            if (neighbourIsOutOfBounds) depth = isWaterTile ? edgeDepth : edgeDepth + waterDepth;
                            vertIndex = vertices.Count;
                            var edgeVertIndexA = sideVertIndexByDir[i][0];
                            var edgeVertIndexB = sideVertIndexByDir[i][1];
                            vertices.Add(tileVertices[edgeVertIndexA]);
                            vertices.Add(tileVertices[edgeVertIndexA] + Vector3.down * depth);
                            vertices.Add(tileVertices[edgeVertIndexB]);
                            vertices.Add(tileVertices[edgeVertIndexB] + Vector3.down * depth);

                            uvs.AddRange(new[] {uv, uv, uv, uv});
                            int[] sideTriIndices =
                                {vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 1, vertIndex + 3, vertIndex + 2};
                            triangles.AddRange(sideTriIndices);
                            normals.AddRange(new[]
                                {sideNormalsByDir[i], sideNormalsByDir[i], sideNormalsByDir[i], sideNormalsByDir[i]});
                        }
                    }

                // Terrain data:
                terrainData.tileCentres[x, y] = nw + new Vector3(0.5f, 0, -0.5f);
                terrainData.walkable[x, y] = isLandTile;
            }

            // Update mesh:
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0, true);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);

            meshRenderer.sharedMaterial = mat;
            UpdateColours();

            numTiles = numLandTiles + numWaterTiles;
            waterPercent = numWaterTiles / (float) numTiles;
            return terrainData;
        }

        private void UpdateColours()
        {
            if (mat != null)
            {
                Color[] startCols = {water.startCol, sand.startCol, grass.startCol};
                Color[] endCols = {water.endCol, sand.endCol, grass.endCol};
                mat.SetColorArray("_StartCols", startCols);
                mat.SetColorArray("_EndCols", endCols);
            }
        }

        private Vector2 GetBiomeInfo(float height, Biome[] biomes)
        {
            // Find current biome
            var biomeIndex = 0;
            float biomeStartHeight = 0;
            for (var i = 0; i < biomes.Length; i++)
            {
                if (height <= biomes[i].height)
                {
                    biomeIndex = i;
                    break;
                }

                biomeStartHeight = biomes[i].height;
            }

            var biome = biomes[biomeIndex];
            var sampleT = Mathf.InverseLerp(biomeStartHeight, biome.height, height);
            sampleT = (int) (sampleT * biome.numSteps) / (float) Mathf.Max(biome.numSteps, 1);

            // UV stores x: biomeIndex and y: val between 0 and 1 for how close to prev/next biome
            var uv = new Vector2(biomeIndex, sampleT);
            return uv;
        }

        private void CreateMeshComponents()
        {
            GameObject holder = null;

            if (meshFilter == null)
            {
                if (GameObject.Find(MeshHolderName))
                {
                    holder = GameObject.Find(MeshHolderName);
                }
                else
                {
                    holder = new GameObject(MeshHolderName);
                    holder.AddComponent<MeshRenderer>();
                    holder.AddComponent<MeshFilter>();
                }

                meshFilter = holder.GetComponent<MeshFilter>();
                meshRenderer = holder.GetComponent<MeshRenderer>();
            }

            if (meshFilter.sharedMesh == null)
            {
                mesh = new Mesh();
                mesh.indexFormat = IndexFormat.UInt32;
                meshFilter.sharedMesh = mesh;
            }
            else
            {
                mesh = meshFilter.sharedMesh;
                mesh.Clear();
            }

            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void OnValidate()
        {
            needsUpdate = true;
        }

        [Serializable]
        public class Biome
        {
            public Color endCol;

            [Range(0, 1)] public float height;

            public int numSteps;
            public Color startCol;
        }

        public class TerrainData
        {
            public bool[,] shore;
            public int size;
            public Vector3[,] tileCentres;
            public bool[,] walkable;

            public TerrainData(int size)
            {
                this.size = size;
                tileCentres = new Vector3[size, size];
                walkable = new bool[size, size];
                shore = new bool[size, size];
            }
        }
    }
}