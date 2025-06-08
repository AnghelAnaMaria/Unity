using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{//interfata ca sa ne asiguram ca clasele care o mostenesc au jagged array completat cu IValue<T>
    public interface IGenericInput<T>
    {
        IVal<T>[][] ReadInputToGrid();
    }

}
