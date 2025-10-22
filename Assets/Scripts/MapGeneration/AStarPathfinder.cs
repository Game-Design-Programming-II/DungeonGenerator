using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public static class AStarPathfinder
    {
        // Core A* pathfinding for RoomGrid. Treats Floor and Prop as walkable, Wall as blocked.
        public static List<Vector2Int> FindPath(RoomGrid grid, Vector2Int start, Vector2Int goal)
        {
            bool Walkable(int x, int y) => grid.InBounds(x, y) && grid.Cells[x, y] != CellType.Wall;

            List<Node> open = new List<Node>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();

            Node StartNode() => new Node(start, g: 0, h: Heuristic(start, goal), parent: default);
            open.Add(StartNode());
            nodes[start] = open[0];

            while (open.Count > 0)
            {
                // pick the node with smallest f = g + h
                int bestIdx = 0;
                for (int i = 1; i < open.Count; i++)
                    if (open[i].F < open[bestIdx].F)
                        bestIdx = i;
                Node current = open[bestIdx];
                open.RemoveAt(bestIdx);

                if (current.Pos == goal)
                    return Reconstruct(current, nodes); // path found

                closed.Add(current.Pos);

                // 4-neighbors
                TryNeighbor(current.Pos + Vector2Int.right, 1);
                TryNeighbor(current.Pos + Vector2Int.left, 1);
                TryNeighbor(current.Pos + Vector2Int.up, 1);
                TryNeighbor(current.Pos + Vector2Int.down, 1);

                void TryNeighbor(Vector2Int np, int stepCost)
                {
                    if (!Walkable(np.x, np.y)) return;
                    if (closed.Contains(np)) return;

                    int tentativeG = current.G + stepCost;

                    if (nodes.TryGetValue(np, out Node existing))
                    {
                        if (tentativeG < existing.G)
                        {
                            existing.G = tentativeG;
                            existing.Parent = current.Pos;
                            // keep H; F is derived
                        }
                    }
                    else
                    {
                        Node n = new Node(np, tentativeG, Heuristic(np, goal), current.Pos);
                        nodes[np] = n;
                        open.Add(n);
                    }
                }
            }

            return null; // no path (should be rare in our simple rooms)

            int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

            List<Vector2Int> Reconstruct(Node end, Dictionary<Vector2Int, Node> map)
            {
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int cur = end.Pos;
                while (true)
                {
                    path.Add(cur);
                    Node n = map[cur];
                    if (n.Parent == null) break;
                    cur = n.Parent.Value;
                }

                path.Reverse();
                return path;
            }
        }

        // local node type (kept internal to the pathfinder)
        struct Node
        {
            public Vector2Int Pos;
            public int G; // cost so far
            public int H; // heuristic
            public int F => G + H;
            public Vector2Int? Parent;

            public Node(Vector2Int pos, int g, int h, Vector2Int? parent)
            { Pos = pos; G = g; H = h; Parent = parent; }
        }
    }
}
