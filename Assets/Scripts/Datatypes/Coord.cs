using System;
using UnityEngine;

// Replacement for Vector2Int, which was causing slowdowns in big loops due to x,y accessor overhead
namespace Datatypes
{
    [Serializable]
    public struct Coord
    {
        public int x;
        public int y;
        public int z;

        public Coord(int x, int y, int z = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static float SqrDistance(Coord a, Coord b)
        {
            return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z);
        }

        public static float Distance(Coord a, Coord b)
        {
            return (float) Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) + (a.z - b.z) * (a.z - b.z));
        }

        public static bool AreNeighbours(Coord a, Coord b)
        {
            return Math.Abs(a.x - b.x) <= 1 && Math.Abs(a.y - b.y) <= 1;
        }

        public static Coord Invalid => new Coord(-1, -1);

        public static Coord Up => new Coord(0, 1);

        public static Coord Down => new Coord(0, -1);

        public static Coord Left => new Coord(-1, 0);

        public static Coord Right => new Coord(1, 0);

        public static Coord operator +(Coord a, Coord b)
        {
            return new Coord(a.x + b.x, a.y + b.y);
        }

        public static Coord operator -(Coord a, Coord b)
        {
            return new Coord(a.x - b.x, a.y - b.y);
        }

        public static bool operator ==(Coord a, Coord b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(Coord a, Coord b)
        {
            return a.x != b.x || a.y != b.y;
        }

        public static implicit operator Vector2(Coord v)
        {
            return new Vector2(v.x, v.y);
        }

        public static implicit operator Vector3(Coord v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        public override bool Equals(object other)
        {
            return other != null && (Coord) other == this;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return $"({x.ToString()} : {y.ToString()} : {z.ToString()})";
        }
    }
}