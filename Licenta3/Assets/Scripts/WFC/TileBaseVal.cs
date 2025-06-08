using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;

namespace WaveFunctionCollapse
{//clasa care imi defineste un Tile pt WFC
    public class TileBaseVal : IVal<UnityEngine.Tilemaps.TileBase>
    {
        private UnityEngine.Tilemaps.TileBase tileBase;//referinta care TileBase din Unity (din scena)

        public TileBaseVal(UnityEngine.Tilemaps.TileBase tileBase)
        {
            this.tileBase = tileBase;
        }

        public UnityEngine.Tilemaps.TileBase value => this.tileBase;

        public bool Equals(IVal<UnityEngine.Tilemaps.TileBase> x, IVal<UnityEngine.Tilemaps.TileBase> y)
        {
            return x == y;
        }

        public int GetHashCode(IVal<UnityEngine.Tilemaps.TileBase> obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(IVal<UnityEngine.Tilemaps.TileBase> other)
        {
            return other.value == this.value;
        }

        public override int GetHashCode()
        {
            return this.tileBase.GetHashCode();
        }
    }
}

