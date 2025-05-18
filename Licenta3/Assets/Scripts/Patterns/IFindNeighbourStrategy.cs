using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public interface IFindNeighbourStrategy//interfata
    {
        Dictionary<int, PatternNeighbours> FindNeighbours(PatternDataResults patternFinderResult);//Dicționar (index pattern, lista vecini per directie)
    }
}

