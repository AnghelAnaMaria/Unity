using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public enum Dir
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionHelper
    {
        public static Dir GetOppositeDirectionTo(this Dir direction)
        {
            switch (direction)
            {
                case Dir.Up:
                    return Dir.Down;
                case Dir.Down:
                    return Dir.Up;
                case Dir.Left:
                    return Dir.Right;
                case Dir.Right:
                    return Dir.Left;
                default:
                    return direction;
            }
        }
    }
}
