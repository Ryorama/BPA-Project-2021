using System.Collections.Generic;
using UnityEngine;
using System;

// Copyright (C) 2020 Matthew Wilson

//NOTICE:  This class produces garbage, if you wish, there is a workaround to eliminate all source of garbage at the sacrifice of system memory (RAM). Contact the dev - contact@activegamedev.com

namespace TerrainEngine2D
{
    /// <summary>
    /// Generates collider paths for the PolygonCollider2D on the chunk
    /// </summary>
    public class ColliderGenerator : MonoBehaviour
    {
        //Value given when a point has been checked and should be skipped
        private const int COLLIDER_POINT_CHECKED = 2;
        //Collider point bitmask values which can be checked twice
        private const int DOUBLE_POINT_BITMASK1 = 5;
        private const int DOUBLE_POINT_BITMASK2 = 10;
        //Adjacent collider point types
        private enum RelativeColliderPoints { Top, Right, Left, Bottom };
        private World world;
        private Chunk chunk;
        //The side length in blocks of the chunk
        private int chunkSize;
        private PolygonCollider2D polygonCollider2D;
        //Lists to hold the PolygonCollider2D paths
        private List<List<Vector2>> colliderPaths;
        //Array holds the values for each checked collider point
        private byte[,] checkedColliderPoints;
        //Max number of points which a path could possibly contain
        private int maxNumColliderPoints;
        //---Collider point bitmask connections---
        //Represents all possible connections a collider point can have
        private static byte[][] colliderPointConnections = new byte[16][] {
            //Bitmask of 0 has no connections (air)
            new byte[]{ },
            //Ex. A collider point with a bitmask of 1 can connect to a collider point with a bitmask of 2, 3, 8, 9, 10, 11
            new byte[]{ 2, 3, 8, 9, 10, 11 }, //Bitmask: 1
		    new byte[]{ 1, 3, 4, 5, 6, 7 }, //2
		    new byte[]{ 1, 2, 3, 5, 7, 10, 11 }, //3 
            new byte[]{ 2, 6, 8, 10, 12, 14 }, //4
		    new byte[]{ 2, 3, 6, 8, 9, 10, 11, 12, 14 }, //5 (Double Point - A point with this bitmask can be used by 2 seperate paths)
		    new byte[]{ 2, 4, 5, 6, 7, 10, 14 }, //6
            new byte[]{ 2, 3, 6, 10, 11, 14 }, //7
		    new byte[]{ 1, 4, 5, 9, 12, 13 }, //8
		    new byte[]{ 1, 5, 8, 9, 10, 11, 13 }, //9
            new byte[]{ 1, 3, 4, 5, 6, 7, 9, 12, 13 }, //10 (Double Point)
		    new byte[]{ 1, 3, 5, 7, 9, 13 }, //11
		    new byte[]{ 4, 5, 8, 10, 12, 13, 14 }, //12
            new byte[]{ 8, 9, 10, 11, 12, 14 }, //13
		    new byte[]{ 4, 5, 6, 7, 12, 13 }, //14
            //Bitmask of 15 has no connections (no edges)
            new byte[]{ }
        };

        private void Awake()
        {
            chunk = GetComponent<Chunk>();
            polygonCollider2D = GetComponent<PolygonCollider2D>();
        }

        /// <summary>
        /// Initializes the collider path lists and arrays with the size of the chunk
        /// </summary>
        public void Initialize(int chunkSize)
        {
            world = World.Instance;
            this.chunkSize = chunkSize;
            //Sets the max number of collider paths to half the number of blocks in the chunk
            int maxNumColliderPaths = Mathf.CeilToInt(chunkSize * chunkSize / 2f);
            //Sets the max number of collider points to the number of points in the chunk
            maxNumColliderPoints = chunkSize * chunkSize + 2 * chunkSize;
            //Allocates the collider paths with the max number of collider paths possible (reduces garbage)
            colliderPaths = new List<List<Vector2>>(maxNumColliderPaths);
            checkedColliderPoints = new byte[chunkSize + 1, chunkSize + 1];
        }

        /// <summary>
        /// Recursively generates collider paths by traversing along the edges of the blocks in the chunk. Uses bitmasking and edge detection to determine the proper route
        /// </summary>
        /// <param name="x">X coordinate of the collider point</param>
        /// <param name="y">Y coordinate of the collider point</param>
        /// <param name="newPath">Set true if generating a new path</param>
        /// <param name="prevPoint">The point previously checked</param>
        public void GenColliderPaths(int x, int y, bool newPath, Vector2Int prevPoint)
        {
            //Return if point has already been checked
            if (checkedColliderPoints[x, y] == COLLIDER_POINT_CHECKED)
                return;

            //Calculate collider point bitmask
            byte bitmask = CalculateColliderBitmask(x, y);

            //Only half checks points which can be checked twice
            if (bitmask == DOUBLE_POINT_BITMASK1 || bitmask == DOUBLE_POINT_BITMASK2)
                checkedColliderPoints[x, y] += 1;
            else
                checkedColliderPoints[x, y] = COLLIDER_POINT_CHECKED;

            //Gets possible bitmask connections for the collider point
            byte[] pointConections = colliderPointConnections[bitmask];

            //If there are no possible connections it is not a collider point, so return
            if (pointConections.Length == 0)
                return;

            //Create a new path (Source of garbage - Refer to top of page)
            if (newPath)
                colliderPaths.Add(new List<Vector2>(maxNumColliderPoints));
            //Add the current point to the collider path
            colliderPaths[colliderPaths.Count - 1].Add(new Vector2Int(x, y));

            //-----Determining the next collider point-----
            //If the current point has a double point bitmask only check for one possible route
            if (!newPath && (bitmask == DOUBLE_POINT_BITMASK1 || bitmask == DOUBLE_POINT_BITMASK2))
            {
                //Get the position of the previous point relative to the current
                RelativeColliderPoints prevPointPosition;
                int diffX = x - (int)prevPoint.x;
                int diffY = y - (int)prevPoint.y;
                if (diffX > 0)
                    prevPointPosition = RelativeColliderPoints.Left;
                else if (diffX < 0)
                    prevPointPosition = RelativeColliderPoints.Right;
                else if (diffY > 0)
                    prevPointPosition = RelativeColliderPoints.Bottom;
                else
                    prevPointPosition = RelativeColliderPoints.Top;
                //First possible double bitmask type
                if (bitmask == DOUBLE_POINT_BITMASK1)
                {
                    //Check for next possible collider point (determined by the position of the previous point)
                    switch (prevPointPosition)
                    {
                        //If the previous point was left of the current point, check to see if the bottom point is a possible match
                        case RelativeColliderPoints.Left:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Bottom);
                            return;
                        case RelativeColliderPoints.Right:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Top);
                            return;
                        case RelativeColliderPoints.Bottom:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Left);
                            return;
                        case RelativeColliderPoints.Top:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Right);
                            return;
                    }
                }
                //Second possible double bitmask type
                else
                {
                    switch (prevPointPosition)
                    {
                        case RelativeColliderPoints.Left:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Top);
                            return;
                        case RelativeColliderPoints.Right:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Bottom);
                            return;
                        case RelativeColliderPoints.Bottom:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Right);
                            return;
                        case RelativeColliderPoints.Top:
                            CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Left);
                            return;
                    }
                }
            }
            //Check all adjacent points for the next collider point
            else
            {
                //Order is important, if a point is found checking stops (there is no branching of paths)
                if (CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Top))
                    return;
                if (CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Right))
                    return;
                if (CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Bottom))
                    return;
                if (CheckNextColliderPoint(x, y, pointConections, prevPoint, RelativeColliderPoints.Left))
                    return;
            }

        }

        /// <summary>
        /// Checks to see if an adjacent point is a match for the next collider point
        /// </summary>
        /// <param name="x">X coordinate of the current point</param>
        /// <param name="y">Y coordinate of the current point</param>
        /// <param name="pointConections">Compatible bitmask connections for the current point</param>
        /// <param name="prevPoint">The previous points position</param>
        /// <param name="relativeColliderPoint">The adjacent point type which is being checked</param>
        /// <returns>Returns true if there is a match and the next collider point is added</returns>
        bool CheckNextColliderPoint(int x, int y, byte[] pointConections, Vector2Int prevPoint, RelativeColliderPoints relativeColliderPoint)
        {
            //Set the x and y coordinates of the collider point
            int pointX = 0;
            int pointY = 0;
            switch (relativeColliderPoint)
            {
                case RelativeColliderPoints.Top:
                    pointX = x;
                    pointY = y + 1;
                    break;
                case RelativeColliderPoints.Right:
                    pointX = x + 1;
                    pointY = y;
                    break;
                case RelativeColliderPoints.Left:
                    pointX = x - 1;
                    pointY = y;
                    break;
                case RelativeColliderPoints.Bottom:
                    pointX = x;
                    pointY = y - 1;
                    break;
            }
            //Return if point is out of bounds
            if (pointX > chunkSize || pointX < 0 || pointY > chunkSize || pointY < 0)
                return false;

            //Return if this point is the same as the previous one
            Vector2Int nextPoint = new Vector2Int(pointX, pointY);
            if (nextPoint == prevPoint)
                return false;

            //Return if the collider point has already been checked or there is no possible edge for the collider path
            if (checkedColliderPoints[pointX, pointY] == COLLIDER_POINT_CHECKED || !PossibleEdgeForColliderPath(x, y, relativeColliderPoint))
                return false;

            //Get the bitmask of the collider point
            byte bitmask = CalculateColliderBitmask(pointX, pointY);
            //Loop through all the possible point connections and see if there is a match
            for (int i = 0; i < pointConections.Length; i++)
            {
                //If there is a match, continue to generate the collider path with the next point
                if (pointConections[i] == bitmask)
                {
                    GenColliderPaths(pointX, pointY, false, new Vector2Int(x, y));
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see if there is a possible edge between a point and the adjacent point
        /// </summary>
        /// <param name="x">X coordinate for the point</param>
        /// <param name="y">Y coordinate for the point</param>
        /// <param name="relativeColliderPoint">Adjacent point</param>
        /// <returns>Returns true if there is an edge between the two points</returns>
        bool PossibleEdgeForColliderPath(int x, int y, RelativeColliderPoints relativeColliderPoint)
        {
            bool isBlockTopLeft, isBlockTopRight, isBlockBottomLeft, isBlockBottomRight;
            switch (relativeColliderPoint)
            {
                //The adjacent point's position relative to the current
                case RelativeColliderPoints.Top:
                    //Check for blocks between the two points
                    isBlockTopLeft = IsBlockAt(x - 1, y);
                    isBlockTopRight = IsBlockAt(x, y);
                    //If there is one and only one block between the two points then there is a possible edge for the collider
                    return (isBlockTopLeft && !isBlockTopRight) || (!isBlockTopLeft && isBlockTopRight);
                case RelativeColliderPoints.Right:
                    isBlockTopRight = IsBlockAt(x, y);
                    isBlockBottomRight = IsBlockAt(x, y - 1);
                    return (isBlockTopRight && !isBlockBottomRight) || (!isBlockTopRight && isBlockBottomRight);
                case RelativeColliderPoints.Left:
                    isBlockTopLeft = IsBlockAt(x - 1, y);
                    isBlockBottomLeft = IsBlockAt(x - 1, y - 1);
                    return (isBlockTopLeft && !isBlockBottomLeft) || (!isBlockTopLeft && isBlockBottomLeft);
                case RelativeColliderPoints.Bottom:
                    isBlockBottomLeft = IsBlockAt(x - 1, y - 1);
                    isBlockBottomRight = IsBlockAt(x, y - 1);
                    return (isBlockBottomLeft && !isBlockBottomRight) || (!isBlockBottomLeft && isBlockBottomRight);
            }
            return false;
        }

        /// <summary>
        /// Calculates the bitmask at a specified coordinate based on surrounding blocks
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the calculated bitmask</returns>
        byte CalculateColliderBitmask(int x, int y)
        {
            //Returns zero if coordinates are out of bounds
            if (x > chunkSize || x < 0 || y > chunkSize || y < 0)
                return 0;

            byte bitmask = 0;
            //If there is a block at an adjacent position turn on the bit representing that block
            if (IsBlockAt(x, y))
                bitmask |= 1;
            if (IsBlockAt(x, y - 1))
                bitmask |= 2;
            if (IsBlockAt(x - 1, y - 1))
                bitmask |= 4;
            if (IsBlockAt(x - 1, y))
                bitmask |= 8;
            return bitmask;
        }
        //For more information on bitmasking refer to the documentation at activegamedev.com/TerrainEngine2D

        /// <summary>
        /// Checks to see if there is a block at a specified coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns true if a block exists at that coordinate</returns>
        bool IsBlockAt(int x, int y)
        {
            //Block coordinate outside the bounds of the chunk
            if (x >= chunkSize || x < 0 || y >= chunkSize || y < 0)
                return false;
            //Loop through all the collider layers
            for (int i = 0; i < world.ColliderLayers.Length; i++)
            {
                //If a block exists in the current layer, return true
                if (world.GetBlockLayer(world.ColliderLayers[i]).IsBlockAt(x + chunk.ChunkX, y + chunk.ChunkY))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Updates the PolygonCollider2D with the generated path list
        /// </summary>
        public void UpdateCollider()
        {
            //resets the PolygonCollider2D paths
            polygonCollider2D.pathCount = 0;
            //Loops through all the collider paths in the list
            for (int i = 0; i < colliderPaths.Count; i++)
            {
                //Adds the path to the PolygonCollider2D (Source of garbage - Refer to top of page)
                polygonCollider2D.pathCount++;
                polygonCollider2D.SetPath(polygonCollider2D.pathCount - 1, colliderPaths[i].ToArray());
                //Clears the current path list
                colliderPaths[i].Clear();
            }
            //Clears the whole path list
            colliderPaths.Clear();
            //Clears the the array holding checked point values
            Array.Clear(checkedColliderPoints, 0, checkedColliderPoints.Length);
        }

    }
}