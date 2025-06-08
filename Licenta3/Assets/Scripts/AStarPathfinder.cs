using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
    private const int TURN_PENALTY = 10;

    public struct AStarNodeState
    {
        public Vector2Int position;
        public Vector2Int direction;  //direcția cu care am intrat aici; pentru start poate fi zero
        public int segmentLength;     //câte tile-uri am parcurs consecutiv în aceeași direcție (ca sa penalizam schimbarea de directie)

        public AStarNodeState(Vector2Int pos, Vector2Int dir, int segLen)
        {
            position = pos;
            direction = dir;
            segmentLength = segLen;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AStarNodeState))
                return false;
            AStarNodeState other = (AStarNodeState)obj;
            return position.Equals(other.position) &&
                   direction.Equals(other.direction) &&
                   segmentLength == other.segmentLength;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + position.GetHashCode();
            hash = hash * 23 + direction.GetHashCode();
            hash = hash * 23 + segmentLength.GetHashCode();
            return hash;
        }
    }


    public static List<Vector2Int> AStarPathfindingExtended(Vector2Int start, Vector2Int goal)
    {
        AStarNodeState startState = new AStarNodeState(start, Vector2Int.zero, 0);

        var openSet = new PriorityQueue<AStarNodeState, int>();
        var cameFrom = new Dictionary<AStarNodeState, AStarNodeState>(); // sau folosește un sistem de chei personalizat
        var gScore = new Dictionary<AStarNodeState, int>();
        var fScore = new Dictionary<AStarNodeState, int>();

        gScore[startState] = 0;
        fScore[startState] = ManhattanDistance(start, goal);
        openSet.Enqueue(startState, fScore[startState]);

        while (openSet.Count > 0)
        {
            AStarNodeState current = openSet.Dequeue();

            if (current.position == goal)
            {
                return ReconstructPathExtended(cameFrom, current);
            }

            foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighborPos = current.position + dir;
                if (!IsWalkable(neighborPos)) continue;

                // Calculează direcția candidat:
                Vector2Int candidateDir = dir;
                int newSegmentLength;
                int turnPenalty = 0;

                // Dacă nu suntem la start și direcția s-a schimbat:
                if (current.direction != Vector2Int.zero && !candidateDir.Equals(current.direction))
                {
                    // Dacă segmentul curent nu a ajuns la pragul minim (de exemplu, 2), penalizează foarte tare
                    if (current.segmentLength < 2)
                        turnPenalty = 1000;
                    else
                        turnPenalty = TURN_PENALTY;

                    newSegmentLength = 1;
                }
                else
                {
                    newSegmentLength = current.segmentLength + 1;
                }

                AStarNodeState neighborState = new AStarNodeState(neighborPos, candidateDir, newSegmentLength);

                int moveCost = GetTileCost(neighborPos);
                int tentativeGScore = gScore[current] + moveCost + turnPenalty;

                // Verificăm dacă am găsit un drum mai bun pentru neighborState
                if (!gScore.ContainsKey(neighborState) || tentativeGScore < gScore[neighborState])
                {
                    cameFrom[neighborState] = current;
                    gScore[neighborState] = tentativeGScore;
                    fScore[neighborState] = tentativeGScore + ManhattanDistance(neighborPos, goal);
                    openSet.Enqueue(neighborState, fScore[neighborState]);
                }
            }
        }

        Debug.LogWarning("AStarExtended: Nu s-a găsit niciun drum!");
        return new List<Vector2Int>();
    }

    private static List<Vector2Int> ReconstructPathExtended(Dictionary<AStarNodeState, AStarNodeState> cameFrom, AStarNodeState current)
    {
        List<Vector2Int> path = new List<Vector2Int> { current.position };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current.position);
        }
        path.Reverse();
        return path;
    }


    public static List<Vector2Int> GetNeighbors(Vector2Int node)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int neighbor = node + dir;
            if (IsWalkable(neighbor))
            {
                //Debug.Log("Vecin walkable: " + neighbor);
                neighbors.Add(neighbor);
            }
            else
            {
                //Debug.Log("Vecin blocat: " + neighbor);
            }
        }
        return neighbors;
    }

    public static bool IsWalkable(Vector2Int pos)
    {
        //Assuming DungeonData.Instance returns your singleton instance.
        ApartmentData dungeonData = ApartmentData.Instance;
        return !dungeonData.GetDungeonRoomTiles().Contains(pos);
    }

    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    /*
        public static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }*/

    public static int GetTileCost(Vector2Int pos)
    {
        ApartmentData dungeonData = ApartmentData.Instance;
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var d in directions)
        {
            Vector2Int adjacent = pos + d;
            if (dungeonData.GetDungeonRoomTiles().Contains(adjacent))
            {
                return 1; //cost scăzut
            }
        }
        return 1000; //cost crescut pentru celelalte tile-uri
    }

}
