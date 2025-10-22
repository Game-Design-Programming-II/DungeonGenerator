using UnityEngine;
using MathsHelper;
using System.Collections.Generic;
using System.Collections;
using Delaunay;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace MapGeneration
{
    public class Generator : MonoBehaviour
    {
        [Header("Room Generation")]
        [SerializeField] private UIntRange _numberOfRooms;
        [SerializeField] private UIntRange _roomSize;
        [SerializeField] private UIntRange _separationRange;
        [SerializeField] private int _refreshCounterMax = 50;
        [SerializeField, Range(0, 1)] private float _cullingPercent;
        [SerializeField] private float _superTriangleOffset;
        [SerializeField, Range(0, 1)] private float _edgeReconnectionPercent;

        [Header("Tilemaps")]
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private Tilemap _floorMap;
        [SerializeField] private Tilemap _wallMap;
        [SerializeField] private Tilemap _propMap;
        
        [Header("Single-Sprite Tiles")]
        [SerializeField] private TileBase _floorTile;
        [SerializeField] private TileBase _wallTile;
        [SerializeField] private TileBase _startTile;
        [SerializeField] private TileBase _endTile;

        [Header("Prop content (placeholder until WFC implemented)")] 
        [SerializeField, Range(0f, 1f)] private float _propScatterDensity = 0.08f;

        [SerializeField] private List<WeightedProp> _propSet = new List<WeightedProp>();
        
        [Header("Test Variables, remove later")]
        [SerializeField] private TileBase _doorTile;
        [SerializeField] private TileBase _aStarTile;
        [SerializeField] private Tilemap _debugMap;

        // start and end position stuff
        private Room _startRoom, _endRoom;
        private Vector2Int _startLocal, _endLocal; // local coords inside their rooms
        private Vector3Int _startWorld, _endWorld; // world tile positions
        
        // A*
        private Dictionary<Room, List<Vector2Int>> _roomDoors = new();
        
        private List<Edge> _lastCorridors;
        private List<Room> _lastRooms;

        // Grid cache for WFC / A*
        private Dictionary<Room, RoomGrid> _roomGrids = new Dictionary<Room, RoomGrid>();
        private List<Room> _gizmoRooms = new List<Room>();
        private List<Edge> _gizmoEdges = new List<Edge>();
        private List<Edge> _gizmoMST = new List<Edge>();

        private void Start()
        {
            //Generate();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Generate();
            }
        }

        public void Generate()
        {
            // clear all tiles in case we generate again after start
            if (_floorMap != null) _floorMap.ClearAllTiles();
            if (_wallMap != null) _wallMap.ClearAllTiles();
            if (_propMap != null) _propMap.ClearAllTiles();
            if (_debugMap != null) _debugMap.ClearAllTiles();
            
            List<Room> rooms = new List<Room>();

            for (int i = 0; i < _numberOfRooms.GetRandomIntValue; i++)
            {
                rooms.Add(new Room(_roomSize.GetRandomUintValue, _roomSize.GetRandomUintValue, Vector2.zero));
            }

            _gizmoRooms = rooms;

            StartCoroutine(SeparateRooms(rooms));
        }

        private IEnumerator SeparateRooms(List<Room> rooms)
        {
            bool collisionFound = true;

            while (collisionFound == true)
            {
                collisionFound = CollisionHelper.Collision2D.DetectRoomCollision(rooms, _separationRange);
                yield return null;
            }

            StartCoroutine(Triangulate(rooms));
        }

        private IEnumerator Triangulate(List<Room> rooms)
        {
            rooms.Sort(SortBySize);
            TurnOffRoomsUnderSizeLimit(rooms);
            Vector2[] extents = FindExtents(rooms);

            float offsetAmount = _superTriangleOffset * _numberOfRooms.GetMaxValue;

            extents[0] += new Vector2(0f, offsetAmount);
            extents[1] += new Vector2(-offsetAmount, -offsetAmount);
            extents[2] += new Vector2(offsetAmount, -offsetAmount);

            List<Triangle> triangles = new List<Triangle>();
            triangles.Add(new Triangle(extents[0], extents[1], extents[2]));

            //bowyer-watson algorithm, modified
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].TurnedOff == true)
                {
                    continue;
                }

                triangles = DelaunayCalculator.BowyerWatson(triangles, rooms[i]);
                yield return null;
            }

            List<Edge> edges = DelaunayCalculator.CalculateEdges(triangles, extents);
            _gizmoEdges = edges;

            StartCoroutine(PrimMinimumSpanning(edges, rooms));
        }

        private IEnumerator PrimMinimumSpanning(List<Edge> edges, List<Room> rooms)
        {
            List<Edge> mst = new List<Edge>();

            List<Room> unreached = new List<Room>(rooms);

            for (int i = unreached.Count - 1; i >= 0; i--)
            {
                if (unreached[i].TurnedOff == true)
                {
                    unreached.RemoveAt(i);
                }
            }

            List<Room> reached = new List<Room>();
            Dictionary<Room, List<Edge>> roomDict = new Dictionary<Room, List<Edge>>();

            for (int i = 0; i < rooms.Count; i++)
            {
                List<Edge> edgesFound = DelaunayCalculator.CollectAllEdgesConnectedToPoint(edges, rooms[i].Position);
                if (roomDict.ContainsKey(rooms[i]) == false)
                {
                    roomDict.Add(rooms[i], edgesFound);
                }
                else
                {
                    roomDict[rooms[i]] = edgesFound;
                }
            }

            reached.Add(unreached[0]);
            unreached.RemoveAt(0);

            while (unreached.Count > 0)
            {
                float curBestDistance = Mathf.Infinity;

                int reachedIndex = 0;
                int unreachedIndex = 0;
                int localEdgeIndex = 0;
                Edge curEdge = null;

                for (int i = 0; i < reached.Count; i++)
                {
                    for (int j = 0; j < unreached.Count; j++)
                    {
                        Room reachedRoom = reached[i];
                        Room unreachedRoom = unreached[j];

                        List<Edge> edgesOfRoom = roomDict[reachedRoom];

                        for (int e = 0; e < edgesOfRoom.Count; e++)
                        {
                            if (edgesOfRoom[e].SharesEdge(reachedRoom.Position, unreachedRoom.Position) &&
                                curBestDistance > edgesOfRoom[e].GetDistance)
                            {
                                curBestDistance = edgesOfRoom[e].GetDistance;
                                reachedIndex = i;
                                unreachedIndex = j;
                                curEdge = edgesOfRoom[e];
                                localEdgeIndex = e;
                            }
                        }
                    }
                }

                if (curEdge != null)
                {
                    Room reachedNode = reached[reachedIndex];
                    Room unreachedNode = unreached[unreachedIndex];

                    for (int i = roomDict[unreachedNode].Count - 1; i >= 0; i--)
                    {
                        if (roomDict[unreachedNode][i].SharesEdge(unreachedNode.Position, reachedNode.Position))
                        {
                            roomDict[unreachedNode].RemoveAt(i);
                        }
                    }

                    roomDict[reachedNode].RemoveAt(localEdgeIndex);
                    reached.Add(unreachedNode);
                    unreached.RemoveAt(unreachedIndex);
                    mst.Add(curEdge);
                    edges.Remove(curEdge);
                }

                yield return null;
            }

            AddRandomEdges(edges, mst);
            List<Edge> corridors = CalculateCorridors(mst, rooms);
            BuildRoomGrids(rooms);
            _lastRooms = new List<Room>(rooms);
            _lastCorridors =  corridors;
            StartCoroutine(DrawContent(rooms, corridors));
            _gizmoMST = mst;
        }

        private IEnumerator DrawContent(List<Room> rooms, List<Edge> corridors)
        {
            int iteration = 0;
            
            // room count TODO: remove debug stuff
            int roomsIterated_Draw = rooms.Count;
            int roomsActive_Draw = 0;

            for (int i = 0; i < rooms.Count; i++)
            {
                Room curRoom = rooms[i];
                if (curRoom.TurnedOff == false)
                {
                    roomsActive_Draw++;
                    Vector2 topLeft = new Vector2(curRoom.Position.x - curRoom.GetWidth,
                        curRoom.Position.y + curRoom.GetHeight);
                    Vector2 topRight = new Vector2(curRoom.Position.x + curRoom.GetWidth,
                        curRoom.Position.y + curRoom.GetHeight);
                    Vector2 bottomLeft = new Vector2(curRoom.Position.x - curRoom.GetWidth,
                        curRoom.Position.y - curRoom.GetHeight);
                    Vector2 bottomRight = new Vector2(curRoom.Position.x + curRoom.GetWidth,
                        curRoom.Position.y - curRoom.GetHeight);

                    /* TODO: use WFC to draw floors and walls in their own functions on their own tile maps
                     Also switch to using tilemap if possible*/
                    //floor tiles
                    MapVisualController.RectFill(curRoom.Position, curRoom.GetWidth,
                        curRoom.GetHeight, _floorTile, _floorMap);
                    //wall tiles
                    MapVisualController.RectFill(topLeft, topRight, 1, _wallTile, _wallMap);
                    MapVisualController.RectFill(topLeft, bottomLeft, 1, _wallTile, _wallMap);
                    MapVisualController.RectFill(bottomLeft, bottomRight, 1, _wallTile, _wallMap);
                    MapVisualController.RectFill(bottomRight, topRight, 1, _wallTile, _wallMap);

                    iteration++;

                    if (iteration >= _refreshCounterMax)
                    {
                        iteration = 0;
                        yield return null;
                    }
                }
            }
            
            Debug.Log($"[DrawContent] Rooms iterated: {roomsIterated_Draw}, active drawn: {roomsActive_Draw}");

            /* TODO: rewrite corridor drawing so that walls and floors can exist on their own
             tile maps without wall appearing inside rooms. Also need to include directional wall pieces, likely with WFC*/
            for (int i = 0; i < corridors.Count; i++)
            {
                Edge curEdge = corridors[i];
                // for now add wall tiles to floor map when drawing corridors
                MapVisualController.RectFill(curEdge.GetPointA, curEdge.GetPointB, 2, _wallTile, _floorMap); 
                MapVisualController.RectFill(curEdge.GetPointA, curEdge.GetPointB, 1, _floorTile, _floorMap, true);
                MapVisualController.RectFill(curEdge.GetPointA, curEdge.GetPointB, 1, null, _wallMap, true); 
                
                iteration++;

                if (iteration >= _refreshCounterMax)
                {
                    iteration = 0;
                    yield return null;
                }
            }

            GenerateRoomContent();
            EnsureRoomConnectivityAStar(); // guarantee paths and clear blocking
        }

        private void GenerateRoomContent()
{
    if (_propMap != null) _propMap.ClearAllTiles();

    var rng = new System.Random();
    IRoomContentGenerator gen = new RandomScatterGenerator(_propScatterDensity);

    // Choose Start/End rooms (from active grids) randomly
    var activeRooms = new List<Room>(_roomGrids.Keys);
    if (activeRooms.Count >= 2)
    {
        int idxStart = rng.Next(activeRooms.Count);
        int idxEnd = rng.Next(activeRooms.Count - 1);
        if (idxEnd >= idxStart) idxEnd++; // ensure distinct

        _startRoom = activeRooms[idxStart];
        _endRoom   = activeRooms[idxEnd];

        // START
        var gS = _roomGrids[_startRoom];
        _startLocal = PickRandomInteriorFloor(gS, rng);
        _startWorld = gS.CellToWorld(_startLocal.x, _startLocal.y);
        gS.Cells[_startLocal.x, _startLocal.y] = CellType.SpecialStart;
        if (_startTile != null) _propMap.SetTile(_startWorld, _startTile);
        else Debug.LogWarning("[Start/End] _startTile not assigned; start marker will be invisible.");

        // END
        var gE = _roomGrids[_endRoom];
        _endLocal = PickRandomInteriorFloor(gE, rng);
        _endWorld = gE.CellToWorld(_endLocal.x, _endLocal.y);
        gE.Cells[_endLocal.x, _endLocal.y] = CellType.SpecialEnd;
        if (_endTile != null) _propMap.SetTile(_endWorld, _endTile);
        else Debug.LogWarning("[Start/End] _endTile not assigned; end marker will be invisible.");
    }
    else
    {
        _startRoom = _endRoom = null;
        Debug.LogWarning("[Start/End] Fewer than 2 active rooms — skipping start/end placement.");
    }

    // --- Scatter normal props in all OTHER rooms only ---
    foreach (var kv in _roomGrids)
    {
        var room = kv.Key;
        if (room == _startRoom || room == _endRoom) continue; // skip start/end rooms

        RoomGrid grid = kv.Value;
        var localPlacements = gen.Generate(grid, rng); // returns local coords

        foreach (var p in localPlacements)
        {
            if (!grid.InBounds(p.x, p.y)) continue;
            if (grid.Cells[p.x, p.y] != CellType.Floor) continue;

            // Mark logical grid and paint to PropMap
            grid.Cells[p.x, p.y] = CellType.Prop;

            var world = grid.CellToWorld(p.x, p.y);
            var chosen = PickWeightedProp(rng); // from earlier step
            if (chosen != null) _propMap.SetTile(world, chosen);
        }
    }

    // Optional: quick log so you can verify which rooms were selected and where
    if (_startRoom != null && _endRoom != null)
    {
        Debug.Log($"[Start/End] Start at world {_startWorld} in room centered {_startRoom.Position}, End at world {_endWorld} in room centered {_endRoom.Position}");
    }
}


        private void AddRandomEdges(List<Edge> edges, List<Edge> mst)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                float rng = Random.Range(0f, 1f);

                if (rng < _edgeReconnectionPercent)
                {
                    mst.Add(edges[i]);
                }
            }
        }

        private List<Edge> CalculateCorridors(List<Edge> mst, List<Room> rooms)
        {
            List<Edge> corridors = new List<Edge>();

            for (int i = 0; i < mst.Count; i++)
            {
                for (int x = 0; x < rooms.Count; x++)
                {
                    Vector2 A = mst[i].GetPointA;
                    Vector2 B = mst[i].GetPointB;

                    // TODO: turned off for now while A* isn't running in these rooms
                    
                    // if (rooms[x].TurnedOff == true)
                    // {
                    //     if (CollisionHelper.Collision2D.LineIntersectRoomBounds(A, B, rooms[x]) == true)
                    //     {
                    //         rooms[x].TurnedOff = false;
                    //     }
                    // }
                    // else
                    // {
                        Vector2 midPoint = new Vector2(A.x, B.y);
                        corridors.Add(new Edge(A, midPoint));
                        corridors.Add(new Edge(midPoint, B));
                    // }
                }
            }

            return corridors;
        }

        private void BuildRoomGrids(List<Room> rooms)
        {
            _roomGrids.Clear(); // remove old room grids

            // TODO: remove debug
            int roomsIterated_Grid = 0;
            int roomsActive_Grid = 0;

            foreach (var r in rooms)
            {
                roomsIterated_Grid++;
                if (r.TurnedOff) continue; // only use active rooms
                
                roomsActive_Grid++;
                var grid = new RoomGrid(r);
                _roomGrids[r] = grid;
            }
            
            Debug.Log($"[BuildRoomdGrids] Rooms iterated: {roomsIterated_Grid},  active grids: {roomsActive_Grid}");
        }

        private int SortBySize(Room A, Room B)
        {
            if (A.GetTotalSize < B.GetTotalSize)
            {
                return 1;
            }

            if (A.GetTotalSize > B.GetTotalSize)
            {
                return -1;
            }

            return 0;
        }

        private void TurnOffRoomsUnderSizeLimit(List<Room> rooms)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                if (((float)i / (float)rooms.Count) > _cullingPercent)
                {
                    rooms[i].TurnedOff = true;
                }
            }
        }

        private Vector2[] FindExtents(List<Room> rooms)
        {
            Vector2[] extents = new Vector2[3];

            for (int i = 0; i < extents.Length; i++)
            {
                extents[i] = Vector2.zero;
            }

            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].Position.y > extents[0].y)
                {
                    extents[0] = rooms[i].Position;
                }

                if (rooms[i].Position.y < extents[1].y)
                {
                    if (rooms[i].Position.x < extents[1].x)
                    {
                        extents[1] = rooms[i].Position;
                    }
                }

                if (rooms[i].Position.y < extents[2].y)
                {
                    if (rooms[i].Position.x > extents[2].x)
                    {
                        extents[2] = rooms[i].Position;
                    }
                }
            }

            return extents;
        }
        
        private void EnsureRoomConnectivityAStar()
        {
            if (_lastRooms == null || _lastCorridors == null) return;

            // Build door list for each active room (one doorway per corridor segment touching it)
            _roomDoors.Clear();
            
            // TODO: remove debug variables
            int roomsIterated_Astar = 0;
            int roomsActive_Astar = 0;
            
            foreach (var r in _lastRooms)
            {
                roomsIterated_Astar++;
                if (!r.TurnedOff)
                {
                    roomsActive_Astar++;
                    _roomDoors[r] = new List<Vector2Int>();
                }
            }

            // quick lookup: world position -> room
            var roomAt = new Dictionary<Vector2, Room>();
            foreach (var r in _lastRooms) 
                if (!r.TurnedOff) roomAt[r.Position] = r;

            // for each corridor segment, attach the endpoint that equals a room Center
            foreach (var seg in _lastCorridors)
            {
                // If a corridor segment starts at a room center, carve a doorway there.
                if (roomAt.TryGetValue(seg.GetPointA, out var roomA) && _roomGrids.TryGetValue(roomA, out var gridA))
                {
                    Vector2 dir = seg.GetPointB - seg.GetPointA; // toward midpoint
                    var door = CarveDoorOnBorder(gridA, roomA, dir);
                    _roomDoors[roomA].Add(door);
                    PaintDoorDebug(gridA, door.x, door.y);
                }
                // Likewise if it ends at a room center
                if (roomAt.TryGetValue(seg.GetPointB, out var roomB) && _roomGrids.TryGetValue(roomB, out var gridB))
                {
                    Vector2 dir = seg.GetPointA - seg.GetPointB; // toward midpoint
                    var door = CarveDoorOnBorder(gridB, roomB, dir);
                    _roomDoors[roomB].Add(door);
                    PaintDoorDebug(gridB, door.x, door.y);
                }
            }
            
            // TODO: REMOVE debug stuff
            int roomsWithDoors = 0;
            foreach (var kv in _roomDoors)
                if (kv.Value != null && kv.Value.Count > 0)
                    roomsWithDoors++;
            Debug.Log($"[A*] Rooms iterated: {roomsIterated_Astar}, active:  {roomsActive_Astar}, with doors:  {roomsWithDoors}");

            // For each room, A* between every pair of its door tiles and then clear props in the way
            foreach (var kv in _roomDoors)
            {
                var room = kv.Key;
                var grid = _roomGrids[room];
                var doors = kv.Value;

                for (int i = 0; i < doors.Count; i++)
                for (int j = i + 1; j < doors.Count; j++)
                {
                    var start = doors[i];
                    var goal  = doors[j];

                    var path = FindPathAStar(grid, start, goal);
                    if (path == null) continue;

                    // TODO: DEBUG: paint the A* path
                    PaintPathDebug(grid, path);
                    
                    // Clear any props along the chosen path
                    foreach (var p in path)
                    {
                        if (!grid.InBounds(p.x, p.y)) continue;

                        if (grid.Cells[p.x, p.y] == CellType.Prop)
                        {
                            grid.Cells[p.x, p.y] = CellType.Floor;
                            var w = grid.CellToWorld(p.x, p.y);
                            _propMap.SetTile(w, null); // remove prop tile on the visual layer
                        }
                    }
                }
            }
        }

        // Chooses which border to carve based on corridor direction.
        // Returns the interior cell (one tile inside the room) that we treat as the door node for A*.
        private Vector2Int CarveDoorOnBorder(RoomGrid grid, Room room, Vector2 dir)
        {
            // Use the more dominant axis to choose a side.
            bool horizontal = Mathf.Abs(dir.x) >= Mathf.Abs(dir.y);

            if (horizontal)
            {
                bool right = dir.x > 0f;
                int gy = Mathf.Clamp(Mathf.RoundToInt(room.Position.y) - grid.Origin.y, 1, grid.Height - 2);
                int gxBorder   = right ? grid.Width - 1 : 0;
                int gxInterior = right ? grid.Width - 2 : 1;

                grid.Cells[gxBorder, gy] = CellType.Floor; // carve 1-tile doorway in the wall ring

                PaintDoorDebug(grid, gxBorder, gy);
                PaintDoorDebug(grid, gxInterior, gy);
                
                return new Vector2Int(gxInterior, gy);     // interior node to path from/to
            }
            else
            {
                bool up = dir.y > 0f;
                int gx = Mathf.Clamp(Mathf.RoundToInt(room.Position.x) - grid.Origin.x, 1, grid.Width - 2);
                int gyBorder   = up ? grid.Height - 1 : 0;
                int gyInterior = up ? grid.Height - 2 : 1;

                grid.Cells[gx, gyBorder] = CellType.Floor;
                
                PaintDoorDebug(grid, gx, gyBorder);
                PaintDoorDebug(grid, gx, gyInterior);
                
                return new Vector2Int(gx, gyInterior);
            }
        }

        private List<Vector2Int> FindPathAStar(RoomGrid grid, Vector2Int start, Vector2Int goal)
        {
            // Treat Floor and Prop as walkable; Wall is blocked.
            bool Walkable(int x, int y)
                => grid.InBounds(x, y) && grid.Cells[x, y] != CellType.Wall;

            var open = new List<Node>();
            var closed = new HashSet<Vector2Int>();
            var nodes = new Dictionary<Vector2Int, Node>();

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
                var current = open[bestIdx];
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

                    if (nodes.TryGetValue(np, out var existing))
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
                        var n = new Node(np, tentativeG, Heuristic(np, goal), current.Pos);
                        nodes[np] = n;
                        open.Add(n);
                    }
                }
            }

            return null; // no path (should be rare in our simple rooms)

            int Heuristic(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

            List<Vector2Int> Reconstruct(Node end, Dictionary<Vector2Int, Node> map)
            {
                var path = new List<Vector2Int>();
                var cur = end.Pos;
                while (true)
                {
                    path.Add(cur);
                    var n = map[cur];
                    if (n.Parent == null) break;
                    cur = n.Parent.Value;
                }

                path.Reverse();
                return path;
            }
        }

        private Vector2Int PickRandomInteriorFloor(RoomGrid g, System.Random rng, int maxTries = 64)
        {
            if (g.Width <= 2 || g.Height <= 2) return new Vector2Int(1, 1);

            for (int t = 0; t < maxTries; t++)
            {
                int gx = rng.Next(1, g.Width  - 1); // interior (excludes wall ring)
                int gy = rng.Next(1, g.Height - 1);
                if (g.Cells[gx, gy] == CellType.Floor)
                    return new Vector2Int(gx, gy);
            }

            // Fallback: first interior floor we can find
            for (int gx = 1; gx < g.Width - 1; gx++)
            for (int gy = 1; gy < g.Height - 1; gy++)
                if (g.Cells[gx, gy] == CellType.Floor)
                    return new Vector2Int(gx, gy);

            return new Vector2Int(0, 0); // degenerate
        }

        
        // local node type
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
        
        private TileBase PickWeightedProp(System.Random rng)
        {
            if (_propSet == null || _propSet.Count == 0) return null;

            // Sum positive weights for entries with a valid tile
            float total = 0f;
            for (int i = 0; i < _propSet.Count; i++)
            {
                var p = _propSet[i];
                if (p != null && p.tile != null && p.weight > 0f) total += p.weight;
            }
            if (total <= 0f) return null;

            double r = rng.NextDouble() * total;
            float acc = 0f;

            for (int i = 0; i < _propSet.Count; i++)
            {
                var p = _propSet[i];
                if (p == null || p.tile == null || p.weight <= 0f) continue;

                acc += p.weight;
                if (r <= acc) return p.tile;
            }
            return null;
        }


        private void OnDrawGizmos()
        {
            if (_gizmoRooms != null)
            {
                for (int i = 0; i < _gizmoRooms.Count; i++)
                {
                    if (_gizmoRooms[i].TurnedOff == true)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(_gizmoRooms[i].Position, _gizmoRooms[i].GetSize);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(_gizmoRooms[i].Position, _gizmoRooms[i].GetSize);
                    }

                }
            }

            if (_gizmoEdges != null)
            {
                Gizmos.color = Color.blue;

                for (int i = 0; i < _gizmoEdges.Count; i++)
                {
                    Gizmos.DrawLine(_gizmoEdges[i].GetPointA, _gizmoEdges[i].GetPointB);
                }
            }

            if (_gizmoMST != null)
            {
                Gizmos.color = Color.yellow;

                for (int i = 0; i < _gizmoMST.Count; i++)
                {
                    Gizmos.DrawLine(_gizmoMST[i].GetPointA, _gizmoMST[i].GetPointB);
                }
            }
        }
        
        private void PaintDoorDebug(RoomGrid grid, int gx, int gy)
        {
            if (_debugMap == null || _doorTile == null) return;
            var w = grid.CellToWorld(gx, gy);
            _debugMap.SetTile(w, _doorTile);
        }

        private void PaintPathDebug(RoomGrid grid, System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int> path)
        {
            if (_debugMap == null || _aStarTile == null) return;
            foreach (var p in path)
            {
                var w = grid.CellToWorld(p.x, p.y);
                _debugMap.SetTile(w, _aStarTile);
            }
        }

    }

    public class Room
    {
        private Vector2 _position;
        private Vector2 _size;
        private bool _turnedOff;

        private float _totalSize = -1;

        public Vector2 Position { get { return _position; } set { _position = value; } }
        public Vector2 GetSize { get { return _size; } }
        public bool TurnedOff { get { return _turnedOff; } set { _turnedOff = value; } }
        public int GetWidth { get { return (int)(_size.x * 0.5f); } }
        public int GetHeight { get { return (int)(_size.y * 0.5f); } }

        public float GetTotalSize
        {
            get
            {
                if (_totalSize == -1)
                {
                    _totalSize = _size.x * _size.y;
                }

                return _totalSize;
            }
        }

        public Room(uint width, uint height, Vector2 position)
        {
            _size = new Vector2(width, height);
            _position = position;
            _turnedOff = false;
        }
    }
    
    public enum CellType { Empty, Floor, Wall, Prop, SpecialStart, SpecialEnd }

    public class RoomGrid
    {
        public Room RoomRef { get; private set; }
        public Vector2Int Origin { get; private set; } // bottom-left tile of the room
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CellType[,] Cells { get; private set; }

        public RoomGrid(Room r)
        {
            RoomRef = r;

            // Room.GetWidth / GetHeight are half room extents, we want full extents 
            Width = r.GetWidth * 2;
            Height = r.GetHeight * 2;

            // get room Origin by taking room center and subtracting it's half extent dimensions
            Origin = new Vector2Int(
                (int)r.Position.x - r.GetWidth,
                (int)r.Position.y - r.GetHeight
            );
            
            Cells = new  CellType[Width, Height];
            
            // set floor tiles
            for (int gx = 0; gx < Width; gx++)
                for (int gy = 0; gy < Height; gy++)
                    Cells[gx, gy] = CellType.Floor;
            
            // set wall tiles
            for (int gx = 0; gx < Width; gx++)
            {
                Cells[gx, 0] = CellType.Wall;
                Cells[gx, Height-1] = CellType.Wall;
            }

            for (int gy = 0; gy < Height; gy++)
            {
                Cells[0, gy] = CellType.Wall;
                Cells[Width-1, gy] = CellType.Wall;
            }
        }
        
        public bool InBounds(int gx, int gy) =>
            gx >= 0 && gy >= 0 && gx < Width && gy < Height;
        
        public Vector3Int CellToWorld(int gx, int gy) =>
            new Vector3Int(Origin.x + gx, Origin.y + gy, 0);
        
        public Vector2Int WorldToCell(Vector3Int world) =>
            new Vector2Int(world.x - Origin.x, world.y - Origin.y);
    }

    [System.Serializable]
    public class WeightedProp
    {
        public TileBase tile;
        [Min(0f)] public float weight = 1f;
    }

    public interface IRoomContentGenerator
    {
        // Return a list of local grid positions to occupy with props
        List<Vector2Int> Generate(RoomGrid grid, System.Random rng);
    }
    
    // TODO: TEMP: very simple scatter routine to prove the pipe works.
    public class RandomScatterGenerator : IRoomContentGenerator
    {
        private readonly float _density;

        public RandomScatterGenerator(float density)
        {
            _density = Mathf.Clamp01(density);
        }

        public List<Vector2Int> Generate(RoomGrid grid, System.Random rng)
        {
            var results = new List<Vector2Int>();
            // if we allow for rooms this small, then they can't contain objects without blocking the walkway
            if (grid.Width <= 2 || grid.Height <= 2) return results;
            
            // ignore 1-tile of padding for the walls
            int x0 = 1, y0 = 1, x1 = grid.Width - 2, y1 = grid.Height - 2;
            for (int gx = x0; gx <= x1; gx++)
            for (int gy = y0; gy <= y1; gy++)
            {
                if (grid.Cells[gx, gy] != CellType.Floor) continue;
                // Bernoulli trial
                if (rng.NextDouble() < _density)
                    results.Add(new Vector2Int(gx, gy));
            }
            return results;
        }
    }
}