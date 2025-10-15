using UnityEngine;
using MapGeneration;
using MathsHelper;
using System.Collections.Generic;

namespace CollisionHelper
{
    public class Collision2D
    {
        public static bool LineIntersectRoomBounds(Vector2 A, Vector2 B, Room room)
        {
            Vector2 midPoint = new Vector2(A.x, B.y);

            //Horizontal at the bottom
            if (LineCalculator.IsIntersectingLine2D(
                A,
                midPoint,
                new Vector2(room.Position.x - room.GetWidth, room.Position.y - room.GetHeight),
                new Vector2(room.Position.x + room.GetWidth, room.Position.y - room.GetHeight)
                ))
            {
                return true;
            }
            //Horizontal at the top
            if (LineCalculator.IsIntersectingLine2D(
                A,
                midPoint,
                new Vector2(room.Position.x - room.GetWidth, room.Position.y + room.GetHeight),
                new Vector2(room.Position.x + room.GetWidth, room.Position.y + room.GetHeight)
                ))
            {
                return true;
            }

            //Vertical 
            if (LineCalculator.IsIntersectingLine2D(
                midPoint,
                B,
                new Vector2(room.Position.x - room.GetWidth, room.Position.y - room.GetHeight),
                new Vector2(room.Position.x - room.GetWidth, room.Position.y + room.GetHeight)
                ))
            {
                return true;
            }
            //Vertical
            if (LineCalculator.IsIntersectingLine2D(
                A,
                midPoint,
                new Vector2(room.Position.x + room.GetWidth, room.Position.y - room.GetHeight),
                new Vector2(room.Position.x + room.GetWidth, room.Position.y + room.GetHeight)
                ))
            {
                return true;
            }

            return false;
        }

        public static bool DetectRoomCollision(List<Room> rooms, UIntRange separationRange)
        {
            bool collisionFound = false;

            for (int x = 0; x < rooms.Count; x++)
            {
                for (int y = 0; y < rooms.Count; y++)
                {
                    if (x == y)
                    {
                        //looking at the same room, so skip
                        continue;
                    }

                    if (SeparateRooms(rooms[x], rooms[y], separationRange) == true)
                    {
                        collisionFound = true;
                    }
                }
            }

            return collisionFound;
        }

        public static bool SeparateRooms(Room A, Room B, UIntRange separationRange)
        {
            if (A == B)
            {
                return false;
            }

            if (A.Position.x - A.GetWidth < B.Position.x + B.GetWidth &&
                A.Position.x + A.GetWidth > B.Position.x - B.GetWidth &&
                A.Position.y - A.GetHeight < B.Position.y + B.GetHeight &&
                A.Position.y + A.GetHeight > B.Position.y - B.GetHeight)
            {
                int rng = separationRange.GetRandomIntValue;

                //checking for horizontal positioning/movement
                if (B.Position.x - A.Position.x > B.Position.y - A.Position.y)
                {
                    if (A.Position.x > B.Position.x) //if A is further to the right
                    {
                        //move A to the right, B to the left
                        A.Position += new Vector2(rng, 0);
                        B.Position += new Vector2(-rng, 0);
                    }
                    else
                    {
                        //move A to the left, B to the right
                        A.Position += new Vector2(-rng, 0);
                        B.Position += new Vector2(rng, 0);
                    }
                }
                //checking for vertical position/movement
                else
                {
                    if (A.Position.y > B.Position.y) //if A is further up
                    {
                        //move A up, B down
                        A.Position += new Vector2(0, rng);
                        B.Position += new Vector2(0, -rng);
                    }
                    else
                    {
                        //move A down, B up
                        A.Position += new Vector2(0, -rng);
                        B.Position += new Vector2(0, rng);
                    }
                }

                return true; //there was a collision
            }

            return false; //no collision
        }
    }
    // Moved into MapGeneration folder to group generation logic.
}
