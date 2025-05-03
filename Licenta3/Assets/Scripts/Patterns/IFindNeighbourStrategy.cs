using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public interface IFindNeighbourStrategy//interfata
    {
        Dictionary<int, PatternNeighbours> FindNeighbours(PatternDataResults patternFinderResult);//Dicționar ce mapează indexul fiecărui pattern la obiectul PatternNeighbours care deține colecțiile de vecini per direcție.
    }
}

