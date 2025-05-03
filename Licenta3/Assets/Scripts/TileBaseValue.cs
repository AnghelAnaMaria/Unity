using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;

namespace WaveFunctionCollapse
{//clasa care imi defineste un Tile pt WFC
    public class TileBaseValue : IValue<TileBase>
    {
        private TileBase tileBase;//referinta care TileBase din Unity (din scena)

        public TileBaseValue(TileBase tileBase)
        {
            this.tileBase = tileBase;
        }

        public TileBase value => this.tileBase;

        public bool Equals(IValue<TileBase> x, IValue<TileBase> y)
        {
            return x == y;
        }

        public int GetHashCode(IValue<TileBase> obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(IValue<TileBase> other)
        {
            return other.value == this.value;
        }

        public override int GetHashCode()
        {
            return this.tileBase.GetHashCode();
        }
    }
}

