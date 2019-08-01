using System;
using System.Collections.Generic;
using System.Diagnostics;
using Behaviour;
using Datatypes;
using TerrainGeneration;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Environments
{
    public class Environment : MonoBehaviour
    {
        // Cached data:
        public static Vector3[,] tileCentres;
        public static bool[,] walkable;
        private static int size;
        private static Coord[,][] walkableNeighboursMap;
        private static List<Coord> walkableCoords;

        // array of visible tiles from any tile; value is Coord.invalid if no visible water tile
        private static Coord[,] closestVisibleWaterMap;

        private static Random prng;

        private static Map preyMap;
        private static Map plantMap;

        [Header("Populations")]
        public Population[] initialPopulations;

        public Transform mapCoordTransform;
        public float mapViewDst;

        public int seed;

        [Header("Debug")]
        public bool showMapDebug;

        private TerrainGenerator.TerrainData terrainData;

        [Header("Trees")] public MeshRenderer treePrefab;

        [Range(0, 1)] public float treeProbability;

        private void Start()
        {
            prng = new Random();

            Init();
            SpawnInitialPopulations();
        }

        private void OnDrawGizmos()
        {
            if (!showMapDebug)
            {
                return;
            }

            if (preyMap != null && mapCoordTransform != null)
            {
                var coord = new Coord((int) mapCoordTransform.position.x, (int) mapCoordTransform.position.z);
                preyMap.DrawDebugGizmos(coord, mapViewDst);
            }
        }

        public static void RegisterMove(LivingEntity entity, Coord from, Coord to)
        {
            preyMap.Move(entity, from, to);
        }

        public static void RegisterPlantDeath(Plant plant)
        {
            plantMap.Remove(plant, plant.coord);
        }

        public static Surroundings Sense(Coord coord)
        {
            var closestPlant = plantMap.ClosestEntity(coord, Animal.maxViewDistance);
            var surroundings = new Surroundings
            {
                nearestFoodSource = closestPlant,
                nearestWaterTile = closestVisibleWaterMap[coord.x, coord.y]
            };

            return surroundings;
        }

        public static Coord GetNextTileRandom(Coord current)
        {
            var neighbours = walkableNeighboursMap[current.x, current.y];
            return neighbours.Length == 0 ? current : neighbours[prng.Next(neighbours.Length)];
        }

        /// Get random neighbour tile, weighted towards those in similar direction as currently facing
        public static Coord GetNextTileWeighted(Coord current, Coord previous, double forwardProbability = 0.2,
            int weightingIterations = 3)
        {
            if (current == previous) return GetNextTileRandom(current);

            var forwardOffset = current - previous;
            // Random chance of returning foward tile (if walkable)
            if (prng.NextDouble() < forwardProbability)
            {
                var forwardCoord = current + forwardOffset;

                if (forwardCoord.x >= 0 && forwardCoord.x < size && forwardCoord.y >= 0 && forwardCoord.y < size)
                    if (walkable[forwardCoord.x, forwardCoord.y])
                        return forwardCoord;
            }

            // Get walkable neighbours
            var neighbours = walkableNeighboursMap[current.x, current.y];
            if (neighbours.Length == 0) return current;

            // From n random tiles, pick the one that is most aligned with the forward direction:
            var forwardDir = new Vector2(forwardOffset.x, forwardOffset.y).normalized;
            var bestScore = float.MinValue;
            var bestNeighbour = current;

            for (var i = 0; i < weightingIterations; i++)
            {
                var neighbour = neighbours[prng.Next(neighbours.Length)];
                Vector2 offset = neighbour - current;
                var score = Vector2.Dot(offset.normalized, forwardDir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestNeighbour = neighbour;
                }
            }

            return bestNeighbour;
        }

        // Call terrain generator and cache useful info
        private void Init()
        {
            var sw = Stopwatch.StartNew();

            var terrainGenerator = FindObjectOfType<TerrainGenerator>();
            terrainData = terrainGenerator.Generate();

            tileCentres = terrainData.tileCentres;
            walkable = terrainData.walkable;
            size = terrainData.size;

            SpawnTrees();

            walkableNeighboursMap = new Coord[size, size][];

            preyMap = new Map(size, 10);
            plantMap = new Map(size, 10);

            // Find and store all walkable neighbours for each walkable tile on the map
            for (var y = 0; y < terrainData.size; y++)
            for (var x = 0; x < terrainData.size; x++)
                if (walkable[x, y])
                {
                    var walkableNeighbours = new List<Coord>();
                    for (var offsetY = -1; offsetY <= 1; offsetY++)
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                        if (offsetX != 0 || offsetY != 0)
                        {
                            var neighbourX = x + offsetX;
                            var neighbourY = y + offsetY;
                            if (neighbourX >= 0 && neighbourX < size && neighbourY >= 0 && neighbourY < size)
                            {
                                if (walkable[neighbourX, neighbourY])
                                {
                                    walkableNeighbours.Add(new Coord(neighbourX, neighbourY));
                                }
                            }
                        }

                    walkableNeighboursMap[x, y] = walkableNeighbours.ToArray();
                }

            // Generate offsets within max view distance, sorted by distance ascending
            // Used to speed up per-tile search for closest water tile
            var viewOffsets = new List<Coord>();
            const int viewRadius = Animal.maxViewDistance;
            const int sqrViewRadius = viewRadius * viewRadius;
            for (var offsetY = -viewRadius; offsetY <= viewRadius; offsetY++)
            for (var offsetX = -viewRadius; offsetX <= viewRadius; offsetX++)
            {
                var sqrOffsetDst = offsetX * offsetX + offsetY * offsetY;
                if ((offsetX != 0 || offsetY != 0) && sqrOffsetDst <= sqrViewRadius)
                    viewOffsets.Add(new Coord(offsetX, offsetY));
            }

            viewOffsets.Sort((a, b) => (a.x * a.x + a.y * a.y).CompareTo(b.x * b.x + b.y * b.y));
            var viewOffsetsArr = viewOffsets.ToArray();

            // Find closest accessible water tile for each tile on the map:
            closestVisibleWaterMap = new Coord[size, size];
            for (var y = 0; y < terrainData.size; y++)
            {
                for (var x = 0; x < terrainData.size; x++)
                {
                    var foundWater = false;
                    if (walkable[x, y])
                        for (var i = 0; i < viewOffsets.Count; i++)
                        {
                            var targetX = x + viewOffsetsArr[i].x;
                            var targetY = y + viewOffsetsArr[i].y;
                            if (targetX >= 0 && targetX < size && targetY >= 0 && targetY < size)
                            {
                                if (terrainData.shore[targetX, targetY])
                                {
                                    if (EnvironmentUtility.TileIsVisibile(x, y, targetX, targetY))
                                    {
                                        closestVisibleWaterMap[x, y] = new Coord(targetX, targetY);
                                        foundWater = true;
                                        break;
                                    }
                                }
                            }
                        }

                    if (!foundWater)
                    {
                        closestVisibleWaterMap[x, y] = Coord.Invalid;
                    }
                }
            }

            Debug.Log(message: $"Init time: {sw.ElapsedMilliseconds.ToString()}");
        }

        private void SpawnTrees()
        {
            // Settings:
            float maxRot = 4;
            var maxScaleDeviation = .2f;
            var colVariationFactor = 0.15f;
            var minCol = .8f;

            var spawnPrng = new Random(seed);
            var treeHolder = new GameObject("Tree holder").transform;
            walkableCoords = new List<Coord>();

            for (var y = 0; y < terrainData.size; y++)
            for (var x = 0; x < terrainData.size; x++)
                if (walkable[x, y])
                {
                    if (prng.NextDouble() < treeProbability)
                    {
                        // Randomize rot/scale
                        var rotX = Mathf.Lerp(-maxRot, maxRot, (float) spawnPrng.NextDouble());
                        var rotZ = Mathf.Lerp(-maxRot, maxRot, (float) spawnPrng.NextDouble());
                        var rotY = (float) spawnPrng.NextDouble() * 360f;
                        var rot = Quaternion.Euler(rotX, rotY, rotZ);
                        var scale = 1 + ((float) spawnPrng.NextDouble() * 2 - 1) * maxScaleDeviation;

                        // Randomize colour
                        var col = Mathf.Lerp(minCol, 1, (float) spawnPrng.NextDouble());
                        var r = col + ((float) spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;
                        var g = col + ((float) spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;
                        var b = col + ((float) spawnPrng.NextDouble() * 2 - 1) * colVariationFactor;

                        // Spawn
                        var tree = Instantiate(treePrefab, tileCentres[x, y], rot);
                        tree.transform.parent = treeHolder;
                        tree.transform.localScale = Vector3.one * scale;
                        tree.material.color = new Color(r, g, b);

                        // Mark tile unwalkable
                        walkable[x, y] = false;
                    }
                    else
                    {
                        walkableCoords.Add(new Coord(x, y));
                    }
                }
        }

        private void SpawnInitialPopulations()
        {
            var spawnPrng = new Random(seed);
            var spawnCoords = new List<Coord>(walkableCoords);

            foreach (var pop in initialPopulations)
            {
                for (var i = 0; i < pop.count; i++)
                {
                    if (spawnCoords.Count == 0)
                    {
                        Debug.Log("Ran out of empty tiles to spawn initial population");
                        break;
                    }

                    var spawnCoordIndex = spawnPrng.Next(0, spawnCoords.Count);
                    var coord = spawnCoords[spawnCoordIndex];
                    spawnCoords.RemoveAt(spawnCoordIndex);

                    var entity = Instantiate(pop.prefab);
                    entity.Init(coord);

                    if (entity is Plant)
                    {
                        plantMap.Add(entity, coord);
                    }
                    else
                    {
                        preyMap.Add(entity, coord);
                        mapCoordTransform = entity.transform;
                    }
                }
            }
        }

        [Serializable]
        public struct Population
        {
            public LivingEntity prefab;
            public int count;
        }
    }
}