using UnityEngine;
using System.Collections.Generic;


namespace Delaunay
{
    using MapGeneration;
    public class DelaunayCalculator
    {
        public static List<Triangle> BowyerWatson(List<Triangle> triangles, Room room)
        {
            List<Triangle> badTriangles = new List<Triangle>();

            for (int x = 0; x < triangles.Count; x++)
            {
                if (triangles[x].IsPointInsideCircumCircle(room.Position) == true)
                {
                    badTriangles.Add(triangles[x]);
                    triangles[x].IsBad = true;
                }
            }

            List<Edge> polygon = new List<Edge>();

            for (int x = 0; x < badTriangles.Count; x++)
            {
                for (int e = 0; e < badTriangles[x].GetEdges.Length; e++)
                {
                    bool shares = false;

                    for (int y = 0; y < badTriangles.Count; y++)
                    {
                        if (x == y)
                        {
                            continue;
                        }

                        if (badTriangles[x].GetEdges[e].SharesEdge(badTriangles[y]) == true)
                        {
                            shares = true;
                            break;
                        }
                    }

                    if (shares == false)
                    {
                        polygon.Add(badTriangles[x].GetEdges[e]);
                    }
                }
            }

            for (int x = triangles.Count - 1; x >= 0; x--)
            {
                if (triangles[x].IsBad == true)
                {
                    triangles.RemoveAt(x);
                }
            }

            for (int x = 0; x < polygon.Count; x++)
            {
                triangles.Add(new Triangle(room.Position, polygon[x].GetPointA, polygon[x].GetPointB));
            }

            return triangles;
        }

        public static List<Edge> CalculateEdges(List<Triangle> triangles, Vector2[] extents)
        {
            List<Edge> edges = new List<Edge>();

            for (int i = triangles.Count - 1; i >= 0; i--)
            {
                if (triangles[i].IsBad ||
                    triangles[i].GetA == extents[0] || triangles[i].GetA == extents[1] || triangles[i].GetA == extents[2] ||
                    triangles[i].GetB == extents[0] || triangles[i].GetB == extents[1] || triangles[i].GetB == extents[2] ||
                    triangles[i].GetC == extents[0] || triangles[i].GetC == extents[1] || triangles[i].GetC == extents[2])
                {
                    triangles.RemoveAt(i);
                }
                else
                {
                    for (int e = 0; e < triangles[i].GetEdges.Length; e++)
                    {
                        edges.Add(triangles[i].GetEdges[e]);
                    }
                }
            }

            return edges;
        }

        public static List<Edge> CollectAllEdgesConnectedToPoint(List<Edge> edges, Vector2 point)
        {
            List<Edge> edgesFound = new List<Edge>();

            for (int e = 0; e < edges.Count; e++)
            {
                if (edges[e].EdgeIncludesPoint(point))
                {
                    edgesFound.Add(edges[e]);
                }
            }

            return edgesFound;
        }
    }

    public class Edge
    {
        private Vector2 _pointA;
        private Vector2 _pointB;

        private float _distance;

        public Vector2 GetPointA { get { return _pointA; } }
        public Vector2 GetPointB { get { return _pointB; } }
        public float GetDistance { get { return _distance; } }

        public Edge(Vector2 A, Vector2 B)
        {
            _pointA = A;
            _pointB = B;

            _distance = Vector3.Distance(A, B);
        }

        public bool EdgeIncludesPoint(Vector2 P)
        {
            if (_pointA == P || _pointB == P)
            {
                return true;
            }

            return false;
        }

        public bool SharesEdge(Vector2 A, Vector2 B)
        {
            if ((_pointA == A || _pointA == B) && (_pointB == A || _pointB == B))
            {
                return true;
            }

            return false;
        }

        public bool SharesEdge(Triangle compare)
        {
            for (int i = 0; i < compare.GetEdges.Length; i++)
            {
                if (SharesEdge(compare.GetEdges[i].GetPointA, compare.GetEdges[i].GetPointB) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class Triangle
    {
        private Vector2 _A;
        private Vector2 _B;
        private Vector2 _C;

        private Vector2 _circumCentre;
        private float _radius;

        private Edge[] _edges;

        //is the triangle good or bad? if bad, discard
        private bool _isBad = false;

        public Vector2 GetA { get { return _A; } }
        public Vector2 GetB { get { return _B; } }
        public Vector2 GetC { get { return _C; } }
        public Vector2 GetCircumCentre { get { return _circumCentre; } }
        public float GetRadius { get { return _radius; } }
        public Edge[] GetEdges { get { return _edges; } }
        public bool IsBad { get { return _isBad; } set { _isBad = value; } }

        public Triangle(Vector2 A, Vector2 B, Vector2 C)
        {
            _A = A;
            _B = B;
            _C = C;

            _edges = new Edge[3];
            _edges[0] = new Edge(_A, _B);
            _edges[1] = new Edge(_B, _C);
            _edges[2] = new Edge(_C, _A);

            CalculateCircumCircle();
        }

        private void CalculateCircumCircle()
        {
            float ab = _A.sqrMagnitude;
            float cd = _B.sqrMagnitude;
            float ef = _C.sqrMagnitude;

            float circumX =
                (ab * (_C.y - _B.y) + cd * (_A.y - _C.y) + ef * (_B.y - _A.y)) /
                (_A.x * (_C.y - _B.y) + _B.x * (_A.y - _C.y) + _C.x * (_B.y - _A.y));

            float circumY =
                (ab * (_C.x - _B.x) + cd * (_A.x - _C.x) + ef * (_B.x - _A.x)) /
                (_A.y * (_C.x - _B.x) + _B.y * (_A.x - _C.x) + _C.y * (_B.x - _A.x));

            _circumCentre = new Vector2(circumX * 0.5f, circumY * 0.5f);
            _radius = Vector3.Distance(_circumCentre, _A);
        }

        public bool IsPointInsideCircumCircle(Vector2 P)
        {
            return Vector3.Distance(_circumCentre, P) <= _radius;
        }

        public bool CompareEdges(Triangle t)
        {
            if (t == this)
            {
                return false;
            }

            for (int e = 0; e < _edges.Length; e++)
            {
                if (_edges[e].SharesEdge(t))
                {
                    return true;
                }
            }

            return false;
        }
    }
}