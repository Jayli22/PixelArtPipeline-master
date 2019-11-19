using System;
using UnityEngine;

namespace SBS
{
    public enum TileType
    {
        Square,
        Hexagon
    }

    [Serializable]
    public class ViewProperty
    {
        public const int VIEW_INITIAL_SIZE = 4;

        public float slopeAngle = 30;

        public bool showTile = false;
        public TileType tileType = TileType.Square;
        public Vector2 tileAspectRatio = new Vector2(2.0f, 1.0f);

        public int size = VIEW_INITIAL_SIZE;
        public bool[] toggles = new bool[VIEW_INITIAL_SIZE] { true, true, true, true };
        public float initialDegree = 0f;
    }
}
