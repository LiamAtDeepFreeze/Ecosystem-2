using System.Collections.Generic;
using Behaviour;
using Datatypes;
using DebugTerminal.Runtime;
using UnityEngine;

// The map is divided into n x n regions, with a list of entities for each region
// This allows an entity to more quickly find other nearby entities
namespace Environments
{
    public class Map
    {
        private readonly Vector2[,] centres;
        public readonly List<LivingEntity>[,] map;
        private readonly int numRegions;
        private readonly int regionSize;

        private static bool _showViewedRegions;
        private static bool _showOccupancy;

        public Map(int size, int regionSize)
        {
            this.regionSize = regionSize;
            numRegions = Mathf.CeilToInt(size / (float) regionSize);
            map = new List<LivingEntity>[numRegions, numRegions];
            centres = new Vector2[numRegions, numRegions];

            for (var y = 0; y < numRegions; y++)
            for (var x = 0; x < numRegions; x++)
            {
                var regionBottomLeft = new Coord(x * regionSize, y * regionSize);
                var regionTopRight = new Coord(x * regionSize + regionSize, y * regionSize + regionSize);
                var centre = (Vector2) (regionBottomLeft + regionTopRight) / 2f;
                centres[x, y] = centre;
                map[x, y] = new List<LivingEntity>();
            }
        }

        [RegisterCommand(Help = "Enables debugging of viewed regions on the map", MaxArgCount = 1)]
        static void ShowViewedRegions(CommandArg[] args)
        {
            if (args.Length == 0)
            {
                _showViewedRegions = !_showViewedRegions;
                return;
            }
            _showViewedRegions = args[0].Bool;
        }

        [RegisterCommand(Help = "Enables debugging of the number of occupants in regions on the map", MaxArgCount = 1)]
        static void ShowOccupancy(CommandArg[] args)
        {
            if (args.Length == 0)
            {
                _showOccupancy = !_showOccupancy;
                return;
            }
            _showOccupancy = args[0].Bool;
        }

        public LivingEntity ClosestEntity(Coord origin, float viewDistance)
        {
            var visibleRegions = GetRegionsInView(origin, viewDistance);
            LivingEntity closestEntity = null;
            var closestSqrDst = viewDistance * viewDistance + 0.01f;

            for (var i = 0; i < visibleRegions.Count; i++)
            {
                // Stop searching if current closest entity is closer than the dst to the region edge
                // All remaining regions will be further as well, since sorted by dst
                if (closestSqrDst <= visibleRegions[i].sqrDstToClosestEdge)
                {
                    break;
                }

                var regionCoord = visibleRegions[i].coord;

                for (var j = 0; j < map[regionCoord.x, regionCoord.y].Count; j++)
                {
                    var entity = map[regionCoord.x, regionCoord.y][j];
                    var sqrDst = Coord.SqrDistance(entity.coord, origin);
                    if (sqrDst < closestSqrDst)
                    {
                        if (EnvironmentUtility.TileIsVisibile(origin.x, origin.y, entity.coord.x, entity.coord.y))
                        {
                            closestSqrDst = sqrDst;
                            closestEntity = entity;
                        }
                    }
                }
            }

            return closestEntity;
        }

        // Calculates coordinates of all regions that may contain entities within view from the specified viewDoord/viewDstance
        // Regions sorted nearest to farthest
        public List<RegionInfo> GetRegionsInView(Coord origin, float viewDistance)
        {
            var regions = new List<RegionInfo>();
            var originRegionX = origin.x / regionSize;
            var originRegionY = origin.y / regionSize;
            var sqrViewDst = viewDistance * viewDistance;
            var viewCentre = origin + Vector2.one * .5f;

            var searchNum = Mathf.Max(1, Mathf.CeilToInt(viewDistance / regionSize));
            // Loop over all regions that might be within the view dst to check if they actually are
            for (var offsetY = -searchNum; offsetY <= searchNum; offsetY++)
            for (var offsetX = -searchNum; offsetX <= searchNum; offsetX++)
            {
                var viewedRegionX = originRegionX + offsetX;
                var viewedRegionY = originRegionY + offsetY;

                if (viewedRegionX >= 0 && viewedRegionX < numRegions && viewedRegionY >= 0 && viewedRegionY < numRegions)
                {
                    // Calculate distance from view coord to closest edge of region to test if region is in range
                    var ox = Mathf.Max(0,
                        Mathf.Abs(viewCentre.x - centres[viewedRegionX, viewedRegionY].x) - regionSize / 2f);
                    var oy = Mathf.Max(0,
                        Mathf.Abs(viewCentre.y - centres[viewedRegionX, viewedRegionY].y) - regionSize / 2f);
                    var sqrDstFromRegionEdge = ox * ox + oy * oy;
                    if (sqrDstFromRegionEdge <= sqrViewDst)
                    {
                        var info = new RegionInfo(new Coord(viewedRegionX, viewedRegionY), sqrDstFromRegionEdge);
                        regions.Add(info);
                    }
                }
            }

            // Sort the regions list from nearest to farthest
            regions.Sort((a, b) => a.sqrDstToClosestEdge.CompareTo(b.sqrDstToClosestEdge));

            return regions;
        }

        public void Add(LivingEntity e, Coord coord)
        {
            var regionX = coord.x / regionSize;
            var regionY = coord.y / regionSize;

            var index = map[regionX, regionY].Count;
            // store the entity's index in the list inside the entity itself for quick access
            e.mapIndex = index;
            e.mapCoord = coord;
            map[regionX, regionY].Add(e);
        }

        public void Remove(LivingEntity e, Coord coord)
        {
            var regionX = coord.x / regionSize;
            var regionY = coord.y / regionSize;

            var index = e.mapIndex;
            var lastElementIndex = map[regionX, regionY].Count - 1;
            // If this entity is not last in the list, put the last entity in its place
            if (index != lastElementIndex)
            {
                map[regionX, regionY][index] = map[regionX, regionY][lastElementIndex];
                map[regionX, regionY][index].mapIndex = e.mapIndex;
            }

            // Remove last entity from the list
            map[regionX, regionY].RemoveAt(lastElementIndex);
        }

        public void Move(LivingEntity e, Coord fromCoord, Coord toCoord)
        {
            Remove(e, fromCoord);
            Add(e, toCoord);
        }

        public void DrawDebugGizmos(Coord coord, float viewDst)
        {
            // Settings:
            var height = Environments.Environment.tileCentres[0, 0].y + 0.1f;
            Gizmos.color = Color.black;

            // Draw:
            var regionX = coord.x / regionSize;
            var regionY = coord.y / regionSize;

            // Draw region lines
            for (var i = 0; i <= numRegions; i++)
            {
                Gizmos.DrawLine(new Vector3(i * regionSize, height, 0),
                    new Vector3(i * regionSize, height, regionSize * numRegions));
                Gizmos.DrawLine(new Vector3(0, height, i * regionSize),
                    new Vector3(regionSize * numRegions, height, i * regionSize));
            }

            // Draw region centres
            for (var y = 0; y < numRegions; y++)
            for (var x = 0; x < numRegions; x++)
            {
                var centre = new Vector3(centres[x, y].x, height, centres[x, y].y);
                Gizmos.DrawSphere(centre, .3f);
            }

            // Highlight regions in view
            if (_showViewedRegions)
            {
                var regionsInView = GetRegionsInView(coord, viewDst);

                for (var y = 0; y < numRegions; y++)
                for (var x = 0; x < numRegions; x++)
                {
                    var centre = new Vector3(centres[x, y].x, height, centres[x, y].y);
                    for (var i = 0; i < regionsInView.Count; i++)
                        if (regionsInView[i].coord.x == x && regionsInView[i].coord.y == y)
                        {
                            var prevCol = Gizmos.color;
                            Gizmos.color = new Color(1, 0, 0, 1 - i / Mathf.Max(1, regionsInView.Count - 1f) * .5f);
                            Gizmos.DrawCube(centre, new Vector3(regionSize, .1f, regionSize));
                            Gizmos.color = prevCol;
                        }
                }
            }

            if (_showOccupancy)
            {
                var maxOccupants = 0;
                for (var y = 0; y < numRegions; y++)
                for (var x = 0; x < numRegions; x++)
                    maxOccupants = Mathf.Max(maxOccupants, map[x, y].Count);
                if (maxOccupants > 0)
                    for (var y = 0; y < numRegions; y++)
                    for (var x = 0; x < numRegions; x++)
                    {
                        var centre = new Vector3(centres[x, y].x, height, centres[x, y].y);
                        var numOccupants = map[x, y].Count;
                        if (numOccupants > 0)
                        {
                            var prevCol = Gizmos.color;
                            Gizmos.color = new Color(1, 0, 0, numOccupants / (float) maxOccupants);
                            Gizmos.DrawCube(centre, new Vector3(regionSize, .1f, regionSize));
                            Gizmos.color = prevCol;
                        }
                    }
            }
        }

        public struct RegionInfo
        {
            public readonly Coord coord;
            public readonly float sqrDstToClosestEdge;

            public RegionInfo(Coord coord, float sqrDstToClosestEdge)
            {
                this.coord = coord;
                this.sqrDstToClosestEdge = sqrDstToClosestEdge;
            }
        }
    }
}