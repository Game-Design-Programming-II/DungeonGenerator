using UnityEngine;
using MathsHelper;
using System;
using System.Collections.Generic;
using System.Collections;
using Delaunay;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using DungeonGenerator.Character;

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
        [SerializeField] private TilemapCollider2D _wallCollider;
        [SerializeField, Tooltip("Optional composite collider used with the wall tilemap.")]
        private CompositeCollider2D _wallComposite;
        
        [Header("Single-Sprite Tiles")]
        [SerializeField] private TileBase _floorTile;
        [SerializeField] private TileBase _wallTile;
        [SerializeField] private TileBase _endTile;

        [Header("Prop content (placeholder until WFC implemented)")] 
        [SerializeField, Range(0f, 1f)] private float _propScatterDensity = 0.08f;
        [SerializeField] private List<WeightedProp> _propSet = new List<WeightedProp>();
        
        [Header("Runtime Content Spawning")]
        [SerializeField] private Transform _runtimeContentParent;
        [SerializeField] private List<EnemySpawnConfig> _enemyTypes = new List<EnemySpawnConfig>();
        [SerializeField] private Vector2Int _fallbackEnemyCount = new Vector2Int(1, 3);
        [SerializeField, Range(0f, 1f)] private float _pickupScatterDensity = 0.1f;
        [SerializeField] private List<GameObject> _pickupPrefabs = new List<GameObject>();
        [SerializeField, Tooltip("Default player count to use for pressure plate puzzles if no runtime data is available.")]
        private int _expectedPlayerCount = 1;
        [SerializeField] private GameObject _spikeTrapPrefab;
        [SerializeField, Range(0f, 1f)] private float _spikeTrapDensity = 0.25f;
        [SerializeField] private GameObject _pressurePlatePrefab;
        
        [Header("Test Variables, remove later")]
        [SerializeField] private TileBase _doorTile;
        [SerializeField] private TileBase _aStarTile;
        [SerializeField] private Tilemap _debugMap;

        // start and end position stuff
        private Room _startRoom, _endRoom;
        private Vector2Int _startLocal, _endLocal; // local coords inside their rooms
        private Vector3Int _startWorld, _endWorld; // world tile positions
        private bool _hasPlayerSpawn;
        private Vector3 _playerSpawnWorldPosition;

        public bool HasPlayerSpawn => _hasPlayerSpawn;
        public Vector3 PlayerSpawnWorldPosition => _playerSpawnWorldPosition;
        public event Action<Vector3> PlayerSpawnPointUpdated;
        
        // A*
        private Dictionary<Room, List<Vector2Int>> _roomDoors = new();
        
        private List<Edge> _lastCorridors;
        private List<Room> _lastRooms;

        // Grid cache for WFC / A*
        private Dictionary<Room, RoomGrid> _roomGrids = new Dictionary<Room, RoomGrid>();
        private List<Room> _gizmoRooms = new List<Room>();
        private List<Edge> _gizmoEdges = new List<Edge>();
        private List<Edge> _gizmoMST = new List<Edge>();
        
        private enum RoomContentRole
        {
            Combat,
            Start,
            End,
            StartEnd,
            SpikePuzzle,
            PressurePuzzle
        }

        private struct RoomAssignment
        {
            public Room room;
            public RoomGrid grid;
            public RoomContentRole role;
        }

        private void Start()
        {
            Generate();
        }

        // Paint decorative props onto the prop tilemap and reserve those tiles.
        private void ScatterProps(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked)
        {
            if (_propSet == null || _propSet.Count == 0 || _propScatterDensity <= 0f) return;

            IRoomContentGenerator generator = new RandomScatterGenerator(_propScatterDensity);
            List<Vector2Int> placements = generator.Generate(grid, rng);

            foreach (Vector2Int cell in placements)
            {
                if (!grid.InBounds(cell.x, cell.y)) continue;
                if (grid.Cells[cell.x, cell.y] != CellType.Floor) continue;

                grid.Cells[cell.x, cell.y] = CellType.Prop;
                blocked?.Add(cell);

                Vector3Int world = grid.CellToWorld(cell.x, cell.y);
                TileBase chosen = PickWeightedProp(rng);
                if (chosen != null)
                {
                    _propMap.SetTile(world, chosen);
                }
            }
        }

        // Drop a single enemy type into the room using the configured count range.
        private void SpawnEnemiesInRoom(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked)
        {
            if (_enemyTypes == null || _enemyTypes.Count == 0) return;

            EnemySpawnConfig config = _enemyTypes[rng.Next(_enemyTypes.Count)];
            if (config == null || config.prefab == null)
            {
                Debug.LogWarning("[EnemySpawn] Missing enemy prefab configuration.");
                return;
            }

            int spawnCount = SampleCount(config.countRange, _fallbackEnemyCount, rng);
            if (spawnCount <= 0) return;

            for (int i = 0; i < spawnCount; i++)
            {
                if (!TryGetRandomFloorCell(grid, rng, blocked, out Vector2Int cell))
                {
                    Debug.LogWarning("[EnemySpawn] Unable to find a free floor cell for enemy.");
                    break;
                }

                SpawnPrefabAtCell(config.prefab, grid, cell);
                blocked?.Add(cell);
            }
        }

        // Distribute pick-up prefabs across floor tiles using a Bernoulli trial.
        private void ScatterPickups(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked)
        {
            if (_pickupPrefabs == null || _pickupPrefabs.Count == 0 || _pickupScatterDensity <= 0f) return;

            int xMin = 1;
            int xMax = grid.Width - 2;
            int yMin = 1;
            int yMax = grid.Height - 2;

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!grid.InBounds(x, y)) continue;
                    if (grid.Cells[x, y] != CellType.Floor) continue;
                    if (blocked != null && blocked.Contains(cell)) continue;
                    if (rng.NextDouble() > _pickupScatterDensity) continue;

                    GameObject prefab = _pickupPrefabs[rng.Next(_pickupPrefabs.Count)];
                    if (prefab == null) continue;

                    SpawnPrefabAtCell(prefab, grid, cell);
                    blocked?.Add(cell);
                }
            }
        }

        // Fill the room with spike hazards according to the configured density.
        private void SpawnSpikeTraps(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked)
        {
            if (_spikeTrapPrefab == null || _spikeTrapDensity <= 0f)
            {
                Debug.LogWarning("[SpikePuzzle] Spike trap prefab or density is missing.");
                return;
            }

            int interiorWidth = Mathf.Max(0, grid.Width - 2);
            int interiorHeight = Mathf.Max(0, grid.Height - 2);
            int targetCount = Mathf.Max(1, Mathf.RoundToInt(interiorWidth * interiorHeight * _spikeTrapDensity));

            for (int i = 0; i < targetCount; i++)
            {
                if (!TryGetRandomFloorCell(grid, rng, blocked, out Vector2Int cell))
                {
                    Debug.LogWarning("[SpikePuzzle] Unable to place all spike traps.");
                    break;
                }

                grid.Cells[cell.x, cell.y] = CellType.Prop;
                SpawnPrefabAtCell(_spikeTrapPrefab, grid, cell);
                blocked?.Add(cell);
            }
        }

        // Spawn one pressure plate per player (real or expected) in this room.
        private void SpawnPressurePlates(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked)
        {
            if (_pressurePlatePrefab == null)
            {
                Debug.LogWarning("[PressurePuzzle] Pressure plate prefab not assigned.");
                return;
            }

            int plateCount = Mathf.Max(1, GetPlayerCountEstimate());

            for (int i = 0; i < plateCount; i++)
            {
                if (!TryGetRandomFloorCell(grid, rng, blocked, out Vector2Int cell))
                {
                    Debug.LogWarning("[PressurePuzzle] Unable to place all pressure plates.");
                    break;
                }

                grid.Cells[cell.x, cell.y] = CellType.Prop;
                SpawnPrefabAtCell(_pressurePlatePrefab, grid, cell);
                blocked?.Add(cell);
            }
        }

        // Try to derive a live player count from the spawn controller, otherwise fallback.
        private int GetPlayerCountEstimate()
        {
            PlayerSpawnController spawner = FindObjectOfType<PlayerSpawnController>();
            if (spawner != null && spawner.SpawnedPlayers.Count > 0)
            {
                return spawner.SpawnedPlayers.Count;
            }

            return Mathf.Max(1, _expectedPlayerCount);
        }

        // Sample an inclusive count from the supplied range, clamping to fallback when invalid.
        private int SampleCount(Vector2Int range, Vector2Int fallback, System.Random rng)
        {
            int min = range.x;
            int max = range.y;
            if (max < min)
            {
                min = fallback.x;
                max = fallback.y;
            }

            min = Mathf.Max(0, min);
            max = Mathf.Max(min, max);

            return rng.Next(min, max + 1);
        }

        // Grab a random floor tile inside the room that is not reserved by previous placements.
        private bool TryGetRandomFloorCell(RoomGrid grid, System.Random rng, HashSet<Vector2Int> blocked, out Vector2Int result)
        {
            result = default;
            int attempts = Mathf.Max(64, grid.Width * grid.Height);

            for (int i = 0; i < attempts; i++)
            {
                int x = rng.Next(1, Mathf.Max(2, grid.Width - 1));
                int y = rng.Next(1, Mathf.Max(2, grid.Height - 1));
                Vector2Int candidate = new Vector2Int(x, y);

                if (!grid.InBounds(x, y)) continue;
                if (grid.Cells[x, y] != CellType.Floor) continue;
                if (blocked != null && blocked.Contains(candidate)) continue;

                result = candidate;
                return true;
            }

            return false;
        }

        // Instantiate a prefab at the centre of the requested room cell.
        private void SpawnPrefabAtCell(GameObject prefab, RoomGrid grid, Vector2Int cell)
        {
            if (prefab == null) return;

            Vector3 worldPos = GetCellCenterWorld(grid, cell);
            Transform parent = _runtimeContentParent != null ? _runtimeContentParent : transform;
            Instantiate(prefab, worldPos, Quaternion.identity, parent);
        }

        private Vector3 GetCellCenterWorld(RoomGrid grid, Vector2Int cell)
        {
            Vector3Int worldCell = grid.CellToWorld(cell.x, cell.y);
            if (_floorMap != null)
            {
                return _floorMap.GetCellCenterWorld(worldCell);
            }

            return new Vector3(worldCell.x + 0.5f, worldCell.y + 0.5f, 0f);
        }

        public void Generate()
        {
            // clear all tiles in case we generate again after start
            if (_floorMap != null) _floorMap.ClearAllTiles();
            if (_wallMap != null) _wallMap.ClearAllTiles();
            if (_propMap != null) _propMap.ClearAllTiles();
            if (_debugMap != null) _debugMap.ClearAllTiles();

            ClearPlayerSpawnPoint();
            RefreshWallColliders();
            
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

            /* Draw corridors with clean floormap/wallmap split:
             * - Floor path on _floorMap
             * - Clear any walls along the path on _wallMap
             * - Add walls to _wallMap adjacent to the path, but never over existing floors (rooms/corridors)
             */
            for (int i = 0; i < corridors.Count; i++)
            {
                Edge curEdge = corridors[i];
                PaintCorridorSegment(curEdge.GetPointA, curEdge.GetPointB);
                
                iteration++;

                if (iteration >= _refreshCounterMax)
                {
                    iteration = 0;
                    yield return null;
                }
            }

            GenerateRoomContent();
            EnsureRoomConnectivityAStar(); // guarantee paths and clear blocking
            RefreshWallColliders();
        }

        // Paint a 1-tile wide corridor floor from A to B on _floorMap,
        // clear any walls along the path on _wallMap, and add adjacent walls on _wallMap
        // without overwriting existing floors (prevents walls inside rooms).
        private void PaintCorridorSegment(Vector2 A, Vector2 B)
        {
            if (_floorMap == null) return;

            foreach (Vector3Int tilePos in EnumerateLineTiles(A, B))
            {
                // Clear walls along the path to avoid wall-over-floor
                if (_wallMap != null)
                {
                    _wallMap.SetTile(tilePos, null);
                }

                // Lay floor
                if (_floorTile != null)
                {
                    _floorMap.SetTile(tilePos, _floorTile);
                }

                // Add simple wall border on 4-neighbors where there is no floor
                if (_wallMap != null && _wallTile != null)
                {
                    TryPlaceWall(tilePos + Vector3Int.up);
                    TryPlaceWall(tilePos + Vector3Int.down);
                    TryPlaceWall(tilePos + Vector3Int.left);
                    TryPlaceWall(tilePos + Vector3Int.right);
                }
            }

            void TryPlaceWall(Vector3Int pos)
            {
                // never place a wall where a floor already exists (room/corridor)
                if (_floorMap.GetTile(pos) != null) return;
                if (_wallMap.GetTile(pos) == null)
                {
                    _wallMap.SetTile(pos, _wallTile);
                }
            }
        }

        // Enumerate integer tile positions along a straight line between two points.
        // Matches the stepping behavior used in MapVisualController.RectFill for line drawing.
        private IEnumerable<Vector3Int> EnumerateLineTiles(Vector2 from, Vector2 to)
        {
            Vector3Int cur = new Vector3Int(Mathf.RoundToInt(from.x), Mathf.RoundToInt(from.y), 0);
            Vector3Int end = new Vector3Int(Mathf.RoundToInt(to.x), Mathf.RoundToInt(to.y), 0);

            yield return cur;
            while (cur != end)
            {
                if (cur.x < end.x) cur.x++;
                else if (cur.x > end.x) cur.x--;

                if (cur.y < end.y) cur.y++;
                else if (cur.y > end.y) cur.y--;

                yield return cur;
            }
        }

        private void GenerateRoomContent()
        {
            if (_propMap != null) _propMap.ClearAllTiles();

            System.Random rng = new System.Random();

            List<RoomAssignment> activeAssignments = new List<RoomAssignment>();
            foreach (KeyValuePair<Room, RoomGrid> kv in _roomGrids)
            {
                Room room = kv.Key;
                if (room == null || room.TurnedOff) continue;
                activeAssignments.Add(new RoomAssignment
                {
                    room = room,
                    grid = kv.Value,
                    role = RoomContentRole.Combat
                });
            }

            if (activeAssignments.Count == 0)
            {
                _startRoom = _endRoom = null;
                ClearPlayerSpawnPoint();
                Debug.LogWarning("[Content] No active rooms available for content generation.");
                return;
            }

            List<RoomAssignment> assignments = BuildRoomAssignments(activeAssignments, rng);

            foreach (RoomAssignment assignment in assignments)
            {
                switch (assignment.role)
                {
                    case RoomContentRole.Start:
                        GenerateStartRoom(assignment, rng);
                        break;
                    case RoomContentRole.StartEnd:
                        GenerateStartRoom(assignment, rng);
                        GenerateEndRoom(assignment, rng);
                        break;
                    case RoomContentRole.End:
                        GenerateEndRoom(assignment, rng);
                        break;
                    case RoomContentRole.SpikePuzzle:
                        GenerateSpikeTrapRoom(assignment, rng);
                        break;
                    case RoomContentRole.PressurePuzzle:
                        GeneratePressurePlateRoom(assignment, rng);
                        break;
                    default:
                        GenerateCombatRoom(assignment, rng);
                        break;
                }
            }

            if (_startRoom != null && _endRoom != null)
            {
                Debug.Log($"[Content] Start at world {_startWorld} in room centered {_startRoom.Position}, End at world {_endWorld} in room centered {_endRoom.Position}");
            }
        }

        private void ClearPlayerSpawnPoint()
        {
            _hasPlayerSpawn = false;
            _playerSpawnWorldPosition = Vector3.zero;
        }

        private void UpdatePlayerSpawnPoint(Vector3Int cellPosition)
        {
            Vector3 worldPosition;

            if (_floorMap != null)
            {
                worldPosition = _floorMap.GetCellCenterWorld(cellPosition);
            }
            else
            {
                worldPosition = new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, cellPosition.z);
            }

            _playerSpawnWorldPosition = worldPosition;
            _hasPlayerSpawn = true;
            PlayerSpawnPointUpdated?.Invoke(_playerSpawnWorldPosition);
        }

        private void RefreshWallColliders()
        {
            if (_wallCollider != null)
            {
                _wallCollider.ProcessTilemapChanges();
            }

            if (_wallComposite != null)
            {
                _wallComposite.GenerateGeometry();
            }
        }

        // Decide which rooms become start/end/puzzle/combat for this generation pass.
        private List<RoomAssignment> BuildRoomAssignments(List<RoomAssignment> activeAssignments, System.Random rng)
        {
            List<RoomAssignment> assignments = new List<RoomAssignment>(activeAssignments);
            int count = assignments.Count;

            int startIndex = rng.Next(count);
            RoomAssignment start = assignments[startIndex];
            start.role = RoomContentRole.Start;
            assignments[startIndex] = start;

            List<int> availableIndices = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (i != startIndex)
                {
                    availableIndices.Add(i);
                }
            }

            if (availableIndices.Count > 0)
            {
                int endSelection = availableIndices[rng.Next(availableIndices.Count)];
                RoomAssignment end = assignments[endSelection];
                end.role = RoomContentRole.End;
                assignments[endSelection] = end;
                availableIndices.Remove(endSelection);
            }
            else
            {
                Debug.LogWarning("[Content] Only one room available; start and end will share the same space.");
                RoomAssignment combined = assignments[startIndex];
                combined.role = RoomContentRole.StartEnd;
                assignments[startIndex] = combined;
            }

            List<RoomContentRole> puzzleQueue = new List<RoomContentRole>
            {
                RoomContentRole.SpikePuzzle,
                RoomContentRole.PressurePuzzle
            };

            foreach (RoomContentRole puzzle in puzzleQueue)
            {
                if (availableIndices.Count == 0)
                {
                    Debug.LogWarning($"[Content] Not enough rooms to place puzzle type {puzzle}; skipping.");
                    continue;
                }

                int pick = rng.Next(availableIndices.Count);
                int roomIndex = availableIndices[pick];
                RoomAssignment puzzleAssignment = assignments[roomIndex];
                puzzleAssignment.role = puzzle;
                assignments[roomIndex] = puzzleAssignment;
                availableIndices.RemoveAt(pick);
            }

            return assignments;
        }

        // Place the player spawn point and any starter pickups.
        private void GenerateStartRoom(RoomAssignment assignment, System.Random rng)
        {
            _startRoom = assignment.room;
            RoomGrid grid = assignment.grid;

            _startLocal = PickRandomInteriorFloor(grid, rng);
            _startWorld = grid.CellToWorld(_startLocal.x, _startLocal.y);
            grid.Cells[_startLocal.x, _startLocal.y] = CellType.SpecialStart;
            UpdatePlayerSpawnPoint(_startWorld);

            HashSet<Vector2Int> blocked = new HashSet<Vector2Int> { _startLocal };
            ScatterPickups(grid, rng, blocked);
        }

        // Place the dungeon exit marker and optional pickups.
        private void GenerateEndRoom(RoomAssignment assignment, System.Random rng)
        {
            _endRoom = assignment.room;
            RoomGrid grid = assignment.grid;

            _endLocal = PickRandomInteriorFloor(grid, rng);
            _endWorld = grid.CellToWorld(_endLocal.x, _endLocal.y);
            grid.Cells[_endLocal.x, _endLocal.y] = CellType.SpecialEnd;

            if (_endTile != null)
            {
                _propMap.SetTile(_endWorld, _endTile);
            }
            else
            {
                Debug.LogWarning("[EndRoom] _endTile not assigned; end marker will be invisible.");
            }

            HashSet<Vector2Int> blocked = new HashSet<Vector2Int> { _endLocal };
            ScatterPickups(grid, rng, blocked);
        }

        // Default room: props, one enemy type, and a sprinkle of pickups.
        private void GenerateCombatRoom(RoomAssignment assignment, System.Random rng)
        {
            RoomGrid grid = assignment.grid;
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

            ScatterProps(grid, rng, blocked);
            SpawnEnemiesInRoom(grid, rng, blocked);
            ScatterPickups(grid, rng, blocked);
        }

        // Puzzle room filled with spike traps; behaves differently from combat rooms.
        private void GenerateSpikeTrapRoom(RoomAssignment assignment, System.Random rng)
        {
            RoomGrid grid = assignment.grid;
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

            SpawnSpikeTraps(grid, rng, blocked);
            ScatterPickups(grid, rng, blocked);
        }

        // Puzzle room that spawns a pressure plate per player.
        private void GeneratePressurePlateRoom(RoomAssignment assignment, System.Random rng)
        {
            RoomGrid grid = assignment.grid;
            HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();

            SpawnPressurePlates(grid, rng, blocked);
            ScatterPickups(grid, rng, blocked);
        }


        private void AddRandomEdges(List<Edge> edges, List<Edge> mst)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                float rng = UnityEngine.Random.Range(0f, 1f);

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
                Vector2 A = mst[i].GetPointA;
                Vector2 B = mst[i].GetPointB;

                Vector2 midPoint = new Vector2(A.x, B.y);

                // Avoid adding zero-length segments in straight-line cases
                if (midPoint != A)
                {
                    corridors.Add(new Edge(A, midPoint));
                }
                if (midPoint != B)
                {
                    corridors.Add(new Edge(midPoint, B));
                }
            }

            return corridors;
        }

        private void BuildRoomGrids(List<Room> rooms)
        {
            _roomGrids.Clear(); // remove old room grids

            // // TODO: remove debug
            // int roomsIterated_Grid = 0;
            // int roomsActive_Grid = 0;

            foreach (Room r in rooms)
            {
                // roomsIterated_Grid++;
                if (r.TurnedOff) continue; // only use active rooms
                
                // roomsActive_Grid++;
                RoomGrid grid = new RoomGrid(r);
                _roomGrids[r] = grid;
            }
            
            // Debug.Log($"[BuildRoomdGrids] Rooms iterated: {roomsIterated_Grid},  active grids: {roomsActive_Grid}");
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
            
            foreach (Room r in _lastRooms)
            {
                roomsIterated_Astar++;
                if (!r.TurnedOff)
                {
                    roomsActive_Astar++;
                    _roomDoors[r] = new List<Vector2Int>();
                }
            }

            // quick lookup: world position -> room
            Dictionary<Vector2, Room> roomAt = new Dictionary<Vector2, Room>();
            foreach (Room r in _lastRooms) 
                if (!r.TurnedOff) roomAt[r.Position] = r;

            // for each corridor segment, attach the endpoint that equals a room Center
            foreach (Edge seg in _lastCorridors)
            {
                // If a corridor segment starts at a room center, carve a doorway there.
                if (roomAt.TryGetValue(seg.GetPointA, out Room roomA) && _roomGrids.TryGetValue(roomA, out RoomGrid gridA))
                {
                    Vector2 dir = seg.GetPointB - seg.GetPointA; // toward midpoint
                    Vector2Int door = CarveDoorOnBorder(gridA, roomA, dir);
                    _roomDoors[roomA].Add(door);
                    PaintDoorDebug(gridA, door.x, door.y);
                }
                // Likewise if it ends at a room center
                if (roomAt.TryGetValue(seg.GetPointB, out Room roomB) && _roomGrids.TryGetValue(roomB, out RoomGrid gridB))
                {
                    Vector2 dir = seg.GetPointA - seg.GetPointB; // toward midpoint
                    Vector2Int door = CarveDoorOnBorder(gridB, roomB, dir);
                    _roomDoors[roomB].Add(door);
                    PaintDoorDebug(gridB, door.x, door.y);
                }
            }
            
            // TODO: REMOVE debug stuff
            int roomsWithDoors = 0;
            foreach (KeyValuePair<Room, List<Vector2Int>> kv in _roomDoors)
                if (kv.Value != null && kv.Value.Count > 0)
                    roomsWithDoors++;
            Debug.Log($"[A*] Rooms iterated: {roomsIterated_Astar}, active:  {roomsActive_Astar}, with doors:  {roomsWithDoors}");

            // For each room, A* between every pair of its door tiles and then clear props in the way
            foreach (KeyValuePair<Room, List<Vector2Int>> kv in _roomDoors)
            {
                Room room = kv.Key;
                RoomGrid grid = _roomGrids[room];
                List<Vector2Int> doors = kv.Value;

                for (int i = 0; i < doors.Count; i++)
                for (int j = i + 1; j < doors.Count; j++)
                {
                    Vector2Int start = doors[i];
                    Vector2Int goal  = doors[j];

                    List<Vector2Int> path = AStarPathfinder.FindPath(grid, start, goal);
                    if (path == null) continue;

                    // TODO: DEBUG: paint the A* path
                    PaintPathDebug(grid, path);
                    
                    // Clear any props along the chosen path
                    foreach (Vector2Int p in path)
                    {
                        if (!grid.InBounds(p.x, p.y)) continue;

                        if (grid.Cells[p.x, p.y] == CellType.Prop)
                        {
                            grid.Cells[p.x, p.y] = CellType.Floor;
                            Vector3Int w = grid.CellToWorld(p.x, p.y);
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
                int doorWorldX = Mathf.RoundToInt(room.Position.x) + (right ? room.GetWidth : -room.GetWidth);
                int doorWorldY = Mathf.RoundToInt(room.Position.y);

                Vector2Int doorBorder = grid.WorldToCell(new Vector3Int(doorWorldX, doorWorldY, 0));
                Vector2Int doorInterior = new Vector2Int(
                    Mathf.Clamp(doorBorder.x + (right ? -1 : 1), 0, grid.Width - 1),
                    Mathf.Clamp(doorBorder.y, 0, grid.Height - 1));

                if (grid.InBounds(doorBorder.x, doorBorder.y))
                {
                    grid.Cells[doorBorder.x, doorBorder.y] = CellType.Floor; // carve doorway in wall ring
                    PaintDoorDebug(grid, doorBorder.x, doorBorder.y);
                }

                if (grid.InBounds(doorInterior.x, doorInterior.y))
                {
                    grid.Cells[doorInterior.x, doorInterior.y] = CellType.Floor;
                    PaintDoorDebug(grid, doorInterior.x, doorInterior.y);
                }

                return doorInterior;     // interior node to path from/to
            }
            else
            {
                bool up = dir.y > 0f;
                int doorWorldY = Mathf.RoundToInt(room.Position.y) + (up ? room.GetHeight : -room.GetHeight);
                int doorWorldX = Mathf.RoundToInt(room.Position.x);

                Vector2Int doorBorder = grid.WorldToCell(new Vector3Int(doorWorldX, doorWorldY, 0));
                Vector2Int doorInterior = new Vector2Int(
                    Mathf.Clamp(doorBorder.x, 0, grid.Width - 1),
                    Mathf.Clamp(doorBorder.y + (up ? -1 : 1), 0, grid.Height - 1));

                if (grid.InBounds(doorBorder.x, doorBorder.y))
                {
                    grid.Cells[doorBorder.x, doorBorder.y] = CellType.Floor;
                    PaintDoorDebug(grid, doorBorder.x, doorBorder.y);
                }

                if (grid.InBounds(doorInterior.x, doorInterior.y))
                {
                    grid.Cells[doorInterior.x, doorInterior.y] = CellType.Floor;
                    PaintDoorDebug(grid, doorInterior.x, doorInterior.y);
                }
                
                return doorInterior;
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

        
        // Node type moved to AStarPathfinder
        
        private TileBase PickWeightedProp(System.Random rng)
        {
            if (_propSet == null || _propSet.Count == 0) return null;

            // Sum positive weights for entries with a valid tile
            float total = 0f;
            for (int i = 0; i < _propSet.Count; i++)
            {
                WeightedProp prop = _propSet[i];
                if (prop != null && prop.tile != null && prop.weight > 0f) total += prop.weight;
            }
            if (total <= 0f) return null;

            double randomDouble = rng.NextDouble() * total;
            float acc = 0f;

            for (int i = 0; i < _propSet.Count; i++)
            {
                WeightedProp prop = _propSet[i];
                if (prop == null || prop.tile == null || prop.weight <= 0f) continue;

                acc += prop.weight;
                if (randomDouble <= acc) return prop.tile;
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
            Vector3Int w = grid.CellToWorld(gx, gy);
            _debugMap.SetTile(w, _doorTile);
        }

        private void PaintPathDebug(RoomGrid grid, System.Collections.Generic.IEnumerable<UnityEngine.Vector2Int> path)
        {
            if (_debugMap == null || _aStarTile == null) return;
            foreach (Vector2Int p in path)
            {
                Vector3Int w = grid.CellToWorld(p.x, p.y);
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
    public class EnemySpawnConfig
    {
        [Tooltip("Friendly identifier used by designers to recognise this enemy entry.")]
        public string id = "Enemy";
        [Tooltip("Prefab that will be instantiated when this entry is picked.")]
        public GameObject prefab;
        [Tooltip("Inclusive range for how many instances of this enemy to spawn in a room.")]
        public Vector2Int countRange = new Vector2Int(1, 3);
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
            List<Vector2Int> results = new List<Vector2Int>();
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
