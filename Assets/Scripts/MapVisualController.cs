using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGeneration
{
    public class MapVisualController : MonoBehaviour
    {
        public static void RectFill(Vector2 position, int width, int height,
            TileBase tile, Tilemap tilemap, bool overrideTile = false)
        {
            for (int x = -width; x < width; x++)
            {
                for (int y = -height; y < height; y++)
                {
                    Vector3Int curPos = new Vector3Int((int)position.x + x,
                        (int)position.y + y, 0);
                    if (tilemap.GetTile(curPos) == null || overrideTile == true)
                    {
                        tilemap.SetTile(curPos, tile);
                    }
                }
            }
        }

        public static void RectFill(Vector2 positionA, Vector2 positionB, int radius,
            TileBase tile, Tilemap tilemap, bool overrideTile = false)
        {
            for (int x = -radius; x < radius; x++)
            {
                for (int y = -radius; y < radius; y++)
                {
                    bool complete = false;

                    Vector3Int curPos = new Vector3Int((int)positionA.x + x,
                        (int)positionA.y + y, 0);
                    Vector3Int endPos = new Vector3Int((int)positionB.x + x,
                        (int)positionB.y + y, 0);

                    while (complete == false)
                    {
                        if (tilemap.GetTile(curPos) == null || overrideTile == true)
                        {
                            tilemap.SetTile(curPos, tile);
                        }

                        if (curPos == endPos)
                        {
                            complete = true;
                        }

                        if (curPos.x < endPos.x)
                        {
                            curPos.x++;
                        }

                        if (curPos.x > endPos.x)
                        {
                            curPos.x--;
                        }

                        if (curPos.y < endPos.y)
                        {
                            curPos.y++;
                        }

                        if (curPos.y > endPos.y)
                        {
                            curPos.y--;
                        }
                    }
                }
            }
        }
    }
}