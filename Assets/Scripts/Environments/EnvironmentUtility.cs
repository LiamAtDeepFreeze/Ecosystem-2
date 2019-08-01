using System;
using Datatypes;

namespace Environments
{
    public static class EnvironmentUtility
    {
        // returns true if unobstructed line of sight to target tile
        public static bool TileIsVisibile(int x, int y, int x2, int y2)
        {
            // bresenham line algorithm
            var w = x2 - x;
            var h = y2 - y;
            var absW = Math.Abs(w);
            var absH = Math.Abs(h);

            // Is neighbouring tile
            if (absW <= 1 && absH <= 1) return true;

            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0)
            {
                dx1 = -1;
                dx2 = -1;
            }
            else if (w > 0)
            {
                dx1 = 1;
                dx2 = 1;
            }

            if (h < 0)
                dy1 = -1;
            else if (h > 0) dy1 = 1;

            var longest = absW;
            var shortest = absH;
            if (longest <= shortest)
            {
                longest = absH;
                shortest = absW;
                if (h < 0)
                    dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> 1;
            for (var i = 1; i < longest; i++)
            {
                numerator += shortest;
                if (numerator >= longest)
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }

                if (!global::Environments.Environment.walkable[x, y]) return false;
            }

            return true;
        }

        // returns null if path is obstructed
        public static Coord[] GetPath(int x, int y, int x2, int y2)
        {
            // bresenham line algorithm
            var w = x2 - x;
            var h = y2 - y;
            var absW = Math.Abs(w);
            var absH = Math.Abs(h);

            // Is neighbouring tile
            if (absW <= 1 && absH <= 1) return null;

            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0)
            {
                dx1 = -1;
                dx2 = -1;
            }
            else if (w > 0)
            {
                dx1 = 1;
                dx2 = 1;
            }

            if (h < 0)
                dy1 = -1;
            else if (h > 0) dy1 = 1;

            var longest = absW;
            var shortest = absH;
            if (longest <= shortest)
            {
                longest = absH;
                shortest = absW;
                if (h < 0)
                    dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> 1;
            var path = new Coord[longest];
            for (var i = 1; i <= longest; i++)
            {
                numerator += shortest;
                if (numerator >= longest)
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }

                // If not walkable, path is invalid so return null
                // (unless is target tile, which may be unwalkable e.g water)
                if (i != longest && !global::Environments.Environment.walkable[x, y]) return null;
                path[i - 1] = new Coord(x, y);
            }

            return path;
        }

        public static Coord GetNextInPath(int x, int y, int targetX, int targetY)
        {
            // bresenham line algorithm
            var w = targetX - x;
            var h = targetY - y;
            var absW = Math.Abs(w);
            var absH = Math.Abs(h);

            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0)
            {
                dx1 = -1;
                dx2 = -1;
            }
            else if (w > 0)
            {
                dx1 = 1;
                dx2 = 1;
            }

            if (h < 0)
                dy1 = -1;
            else if (h > 0) dy1 = 1;

            var longest = absW;
            var shortest = absH;
            if (longest <= shortest)
            {
                longest = absH;
                shortest = absW;
                if (h < 0)
                    dy2 = -1;
                else if (h > 0) dy2 = 1;
                dx2 = 0;
            }

            var numerator = longest >> (1 + shortest);
            if (numerator >= longest)
            {
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }

            return new Coord(x, y);
        }
    }
}