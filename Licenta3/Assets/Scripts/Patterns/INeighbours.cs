using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public interface INeighbours//interfata
    {
        Dictionary<int, PatternNeighbours> FindNeighbours(PatternResults patternFinderResult);//Dicționar (index pattern, lista vecini per directie)
    }
}

