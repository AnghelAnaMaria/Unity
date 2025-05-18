using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{//interfata care ne asigura ca clasele ce o mostenesc au un getter care returneaza valoarea de tip T, au metoda de comparatie Equals si metoda GetHashCode.
    public interface IValue<T> : IEqualityComparer<IValue<T>>, IEquatable<IValue<T>>
    {
        T value { get; }
    }
}
