using System.Collections.Generic;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// A grid based mesh for rendering the world blocks
    /// </summary>
    public class BlockGridMesh
    {
        //Values for setting list sizes
        private const int MAX_NUM_SQUARES_PER_BLOCK = 5;
        private const int NUM_VERTICES_PER_BLOCK = 4;
        private const int NUM_UVS_PER_BLOCK = 4;
        private const int NUM_TRIANGLES_PER_BLOCK = 6;
        private const int NUM_COLORS_PER_BLOCK = 4;
        //Mesh properties
        private Mesh mesh;
        private List<Vector3> vertices;
        private List<Vector2> uv;
        private List<int>[] triangles;
        private List<Color32> colors;
        //Reference to the current triangle index 
        private int triangleOffset;
        //Whether this mesh uses colors
        private bool useColors;
        //The z distance between blocks of the same layer
        private float zBlockDistance;
        //Stores wether each layer requires a material (used for setting chunk materials)
        private bool[] materialLayer;

        /// <summary>
        /// Constructor for setting up the Block Grid Mesh
        /// </summary>
        /// <param name="mesh">The mesh to use for this block grid</param>
        /// <param name="gridSize">The size of the mesh (1 side length)/param>
        /// <param name="zBlockDistance">The z distance between blocks</param>
        /// <param name="dynamic">Whether the mesh will be constantly changing</param>
        /// <param name="numLayers">Number of layers in the mesh</param>
        /// <param name="useColors">Whether colors are used</param>
        public BlockGridMesh(Mesh mesh, int gridSize, float zBlockDistance, bool dynamic, int numLayers, bool useColors = false)
        {
            this.mesh = mesh;
            this.useColors = useColors;
            this.zBlockDistance = zBlockDistance;
            if (dynamic)
                this.mesh.MarkDynamic();
            materialLayer = new bool[numLayers];
            //Calculate the maximum size of each list
            int maxNumVertices = MAX_NUM_SQUARES_PER_BLOCK * NUM_VERTICES_PER_BLOCK * gridSize * gridSize * numLayers;
            int maxNumUVs = MAX_NUM_SQUARES_PER_BLOCK * NUM_UVS_PER_BLOCK * gridSize * gridSize * numLayers;
            int maxNumTriangles = MAX_NUM_SQUARES_PER_BLOCK * NUM_TRIANGLES_PER_BLOCK * gridSize * gridSize * numLayers;
            //Allocate all the mesh lists
            vertices = new List<Vector3>(maxNumVertices);
            uv = new List<Vector2>(maxNumUVs);
            triangles = new List<int>[numLayers];
            for(int i = 0; i < numLayers; i++)
            {
                triangles[i] = new List<int>(maxNumTriangles);
            }
            if (useColors)
            {
                int maxNumColors = NUM_COLORS_PER_BLOCK * gridSize * gridSize * numLayers;
                colors = new List<Color32>(maxNumColors);
            }
        }

        /// <summary>
        /// Constructor for setting up the Block Grid Mesh
        /// </summary>
        /// <param name="mesh">The mesh to use for this block grid</param>
        /// <param name="width">The width of the mesh/param>
        /// <param name="height">The height of the mesh/param>
        /// <param name="zBlockDistance">The z distance between blocks</param>
        /// <param name="dynamic">Whether the mesh will be constantly changing</param>
        /// <param name="numLayers">Number of layers in the mesh</param>
        /// <param name="useColors">Whether colors are used</param>
        public BlockGridMesh(Mesh mesh, int width, int height, float zBlockDistance, bool dynamic, int numLayers, bool useColors = false)
        {
            this.mesh = mesh;
            this.useColors = useColors;
            this.zBlockDistance = zBlockDistance;
            if (dynamic)
                this.mesh.MarkDynamic();
            materialLayer = new bool[numLayers];
            //Calculate the maximum size of each list
            int maxNumVertices = MAX_NUM_SQUARES_PER_BLOCK * NUM_VERTICES_PER_BLOCK * width * height * numLayers;
            int maxNumUVs = MAX_NUM_SQUARES_PER_BLOCK * NUM_UVS_PER_BLOCK * width * height * numLayers;
            int maxNumTriangles = MAX_NUM_SQUARES_PER_BLOCK * NUM_TRIANGLES_PER_BLOCK * width * height * numLayers;
            //Allocate all the mesh lists
            vertices = new List<Vector3>(maxNumVertices);
            uv = new List<Vector2>(maxNumUVs);
            triangles = new List<int>[numLayers];
            for (int i = 0; i < numLayers; i++)
            {
                triangles[i] = new List<int>(maxNumTriangles);
            }
            if (useColors)
            {
                int maxNumColors = NUM_COLORS_PER_BLOCK * width * height * numLayers;
                colors = new List<Color32>(maxNumColors);
            }
        }

        /// <summary>
        /// Create a default block
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z order</param>
        /// <param name="texturePosition">Position of the texture</param>
        /// <param name="layer">The layer of the block</param>
        /// <param name="tileUnitX">Ratio of the block width to total texture width</param>
        /// <param name="tileUnitY">Ratio of the block height to total texture height</param>
        /// <param name="width">Width of the block (in block units)</param>
        /// <param name="height">Height of the block (in block units)</param>
        public void CreateBlock(int x, int y, float z, Vector2 texturePosition, byte layer, float tileUnitX, float tileUnitY, float width, float height)
        {
            //Add the four corners of the block
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x, y + height, z));
            vertices.Add(new Vector3(x + width, y + height, z));
            vertices.Add(new Vector3(x + width, y, z));
            //Create the triangles for the block's vertices
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 2);
            triangles[layer].Add(triangleOffset + 3);
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 1);
            triangles[layer].Add(triangleOffset + 2);

            //Fix line issues due to floating point imperfections - can cause pixel distortion if too big
            float epsilon = 0.0001f;
            //Add uv data for the vertices
            uv.Add(new Vector2(tileUnitX * texturePosition.x + epsilon, tileUnitY * texturePosition.y + epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + epsilon, tileUnitY * texturePosition.y + tileUnitY - epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + tileUnitX - epsilon, tileUnitY * texturePosition.y + tileUnitY - epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + tileUnitX - epsilon, tileUnitY * texturePosition.y + epsilon));

            triangleOffset += 4;
        }

        /// <summary>
        /// Create a default block with color
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z order</param>
        /// <param name="texturePosition">Position of the texture</param>
        /// <param name="layer">The layer of the block</param>
        /// <param name="tileUnitX">Ratio of the block width to total texture width</param>
        /// <param name="tileUnitY">Ratio of the block height to total texture height</param>
        /// <param name="width">Width of the block (in block units)</param>
        /// <param name="height">Height of the block (in block units)</param>
        /// <param name="color">Color of the block</param>
        public void CreateBlock(int x, int y, float z, Vector2 texturePosition, byte layer, float tileUnitX, float tileUnitY, float width, float height, Color32 color)
        {
            //Add the four corners of the block
            vertices.Add(new Vector3(x, y, z));
            vertices.Add(new Vector3(x, y + height, z));
            vertices.Add(new Vector3(x + width, y + height, z));
            vertices.Add(new Vector3(x + width, y, z));
            //Create the triangles for the block's vertices
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 2);
            triangles[layer].Add(triangleOffset + 3);
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 1);
            triangles[layer].Add(triangleOffset + 2);
            //Add color data
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            //Fix line issues due to floating point imperfections - can cause pixel distortion if too big
            float epsilon = 0.0001f;
            //Add uv data for the vertices
            uv.Add(new Vector2(tileUnitX * texturePosition.x + epsilon, tileUnitY * texturePosition.y + epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + epsilon, tileUnitY * texturePosition.y + tileUnitY - epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + tileUnitX - epsilon, tileUnitY * texturePosition.y + tileUnitY - epsilon));
            uv.Add(new Vector2(tileUnitX * texturePosition.x + tileUnitX - epsilon, tileUnitY * texturePosition.y + epsilon));

            triangleOffset += 4;
        }

        /// <summary>
        /// Create a overlap block
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z order</param>
        /// <param name="bitmask">Bitmask of the block</param>
        /// <param name="texturePosition">Position of the texture</param>
        /// <param name="layer">The layer of the block</param>
        /// <param name="tileUnitX">Ratio of the block width to total texture width</param>
        /// <param name="tileUnitY">Ratio of the block height to total texture height</param>
        ///<param name="blendSquaresOverlap">The blend squares will overlap the edge textures instead of replace them (optimization - much less vertices)</param>
        public void CreateOverlapBlock(int x, int y, float z, byte bitmask, Vector2 texturePosition, byte layer, float tileUnitX, float tileUnitY, bool blendSquaresOverlap)
        {
            //Whether the blend squares will overlap over the edges (faster) or replace them (slower)
            if (blendSquaresOverlap)
            {
                //Default vertex positions for the block (only the center square)
                float leftXVertice = x;
                float rightXVertice = x + 1;
                float bottomYVertice = y;
                float topYVertice = y + 1;
                //Default UV values for the block
                float leftXUV = 1 / 4f;
                float rightXUV = 3 / 4f;
                float bottomYUV = 1 / 6f;
                float topYUV = 1 / 2f;

                //Checks bitmask to see if a top edge is required (there is no block above it)
                if (IsBitSet(bitmask, 4))
                {
                    //Sets new vertex and uv data to include the top edge of the block
                    topYVertice += 0.5f;
                    topYUV = 2 / 3f;
                }
                //Checks right edge
                if (IsBitSet(bitmask, 5))
                {
                    rightXVertice += 0.5f;
                    rightXUV = 1f;
                }
                //Checks bottom edge
                if (IsBitSet(bitmask, 6))
                {
                    bottomYVertice -= 0.5f;
                    bottomYUV = 0;
                }
                //Checks left edge
                if (IsBitSet(bitmask, 7))
                {
                    leftXVertice -= 0.5f;
                    leftXUV = 0;
                }
                //Adds the square block info to the list for generating the mesh
                AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);

                //-----Check for corner blocks-----
                //If corner blocks are found, generate the smaller overlapping squares

                //Check bitmask to see if there is a block at the bottom left
                if (IsBitSet(bitmask, 2))
                {
                    //Check bitmask if there is no block below it
                    if (IsBitSet(bitmask, 6))
                    {
                        //Set properties for the blending square overlay
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y - 0.5f;
                        topYVertice = y;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 5 / 6f;
                        topYUV = 1f;
                        //Add the blend square overlay info to the mesh lists
                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z - zBlockDistance / 2f, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                    //Check bitmask if there is no block to the left
                    if (IsBitSet(bitmask, 7))
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y;
                        topYVertice = y + 0.5f;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 2 / 3f;
                        topYUV = 5 / 6f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z - zBlockDistance / 2f, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                }
                //Check for a block at the top left
                if (IsBitSet(bitmask, 3))
                {
                    //If no block above, add the blend overlay
                    if (IsBitSet(bitmask, 4))
                    {
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y + 1f;
                        topYVertice = y + 1.5f;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 2 / 3f;
                        topYUV = 5 / 6f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z - zBlockDistance / 2f, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                    //If no left block, add blend overlay
                    if (IsBitSet(bitmask, 7))
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y + 0.5f;
                        topYVertice = y + 1f;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 5 / 6f;
                        topYUV = 1f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z - zBlockDistance / 2f, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                }
            }
            else
            {
                //Default vertex positions for the block (only the center square)
                float leftXVertice = x;
                float rightXVertice = x + 1;
                float bottomYVertice = y;
                float topYVertice = y + 1;
                //Default UV values for the block
                float leftXUV = 1 / 4f;
                float rightXUV = 3 / 4f;
                float bottomYUV = 1 / 6f;
                float topYUV = 1 / 2f;
                //Adds the square block info to the list for generating the mesh
                AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                //Check bitmask for adjacent blocks
                bool topRightBlockExists = IsBitSet(bitmask, 0);
                bool bottomRightBlockExists = IsBitSet(bitmask, 1);
                bool bottomLeftBlockExists = IsBitSet(bitmask, 2);
                bool topLeftBlockExists = IsBitSet(bitmask, 3);
                bool noTopBlock = IsBitSet(bitmask, 4);
                bool noRightBlock = IsBitSet(bitmask, 5);
                bool noBottomBlock = IsBitSet(bitmask, 6);
                bool noLeftBlock = IsBitSet(bitmask, 7);

                //Add top edge texture if there is no adjacent block above and both top corner blocks do not coexist
                if (noTopBlock && !(topRightBlockExists && topLeftBlockExists))
                {
                    //If the top right corner block exists, set the vertex and uv data
                    if (topRightBlockExists)
                    {
                        //Only include the left half of the top edge
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y + 1;
                        topYVertice = y + 1.5f;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 1 / 2f;
                        topYUV = 2 / 3f;
                    }
                    else if (topLeftBlockExists)
                    {
                        //Only include the right half of the top edge
                        leftXVertice = x + 0.5f;
                        rightXVertice = x + 1;
                        bottomYVertice = y + 1;
                        topYVertice = y + 1.5f;
                        leftXUV = 0.5f;
                        rightXUV = 0.75f;
                        bottomYUV = 1 / 2f;
                        topYUV = 2 / 3f;
                    }
                    else
                    {
                        //Include the whole top edge
                        leftXVertice = x;
                        rightXVertice = x + 1;
                        bottomYVertice = y + 1;
                        topYVertice = y + 1.5f;
                        leftXUV = 0.25f;
                        rightXUV = 0.75f;
                        bottomYUV = 1 / 2f;
                        topYUV = 2 / 3f;
                    }
                    //Add a new square for the top edge
                    AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                }
                //Checks right edge
                if (noRightBlock && !(topRightBlockExists && bottomRightBlockExists))
                {
                    rightXVertice += 0.5f;
                    rightXUV = 1f;
                    if (topRightBlockExists)
                    {
                        leftXVertice = x + 1;
                        rightXVertice = x + 1.5f;
                        bottomYVertice = y;
                        topYVertice = y + 0.5f;
                        leftXUV = 0.75f;
                        rightXUV = 1f;
                        bottomYUV = 1 / 6f;
                        topYUV = 1 / 3f;
                    }
                    else if (bottomRightBlockExists)
                    {
                        leftXVertice = x + 1;
                        rightXVertice = x + 1.5f;
                        bottomYVertice = y + 0.5f;
                        topYVertice = y + 1;
                        leftXUV = 0.75f;
                        rightXUV = 1f;
                        bottomYUV = 1 / 3f;
                        topYUV = 1 / 2f;
                    }
                    else
                    {
                        leftXVertice = x + 1;
                        rightXVertice = x + 1.5f;
                        bottomYVertice = y;
                        topYVertice = y + 1;
                        leftXUV = 0.75f;
                        rightXUV = 1f;
                        bottomYUV = 1 / 6f;
                        topYUV = 1 / 2f;
                    }
                    AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                }
                //Checks bottom edge
                if (noBottomBlock && !(bottomRightBlockExists && bottomLeftBlockExists))
                {
                    if (bottomRightBlockExists)
                    {
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y - 0.5f;
                        topYVertice = y;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 0;
                        topYUV = 1 / 6f;
                    }
                    else if (bottomLeftBlockExists)
                    {
                        leftXVertice = x + 0.5f;
                        rightXVertice = x + 1;
                        bottomYVertice = y - 0.5f;
                        topYVertice = y;
                        leftXUV = 0.5f;
                        rightXUV = 0.75f;
                        bottomYUV = 0;
                        topYUV = 1 / 6f;
                    }
                    else
                    {
                        leftXVertice = x;
                        rightXVertice = x + 1;
                        bottomYVertice = y - 0.5f;
                        topYVertice = y;
                        leftXUV = 0.25f;
                        rightXUV = 0.75f;
                        bottomYUV = 0;
                        topYUV = 1 / 6f;
                    }
                    AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                }
                //Checks left edge
                if (noLeftBlock && !(topLeftBlockExists && bottomLeftBlockExists))
                {
                    leftXVertice -= 0.5f;
                    leftXUV = 0;
                    if (topLeftBlockExists)
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y;
                        topYVertice = y + 0.5f;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 1 / 6f;
                        topYUV = 1 / 3f;
                    }
                    else if (bottomLeftBlockExists)
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y + 0.5f;
                        topYVertice = y + 1;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 1 / 3f;
                        topYUV = 1 / 2f;
                    }
                    else
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y;
                        topYVertice = y + 1;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 1 / 6f;
                        topYUV = 1 / 2f;
                    }
                    AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                }

                //-----Check for corner blocks-----
                //If corner blocks are found, generate the smaller blending squares

                if (bottomLeftBlockExists)
                {
                    if (noBottomBlock)
                    {
                        //Set properties for the blending square
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y - 0.5f;
                        topYVertice = y;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 5 / 6f;
                        topYUV = 1f;
                        //Add the blend square info to the mesh lists
                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }

                    if (noLeftBlock)
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y;
                        topYVertice = y + 0.5f;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 2 / 3f;
                        topYUV = 5 / 6f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                }
                //Check for a block at the top left
                if (topLeftBlockExists)
                {
                    //If no block above, add the blend square
                    if (noTopBlock)
                    {
                        leftXVertice = x;
                        rightXVertice = x + 0.5f;
                        bottomYVertice = y + 1f;
                        topYVertice = y + 1.5f;
                        leftXUV = 0;
                        rightXUV = 0.25f;
                        bottomYUV = 2 / 3f;
                        topYUV = 5 / 6f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                    //If no left block, add blend square
                    if (noLeftBlock)
                    {
                        leftXVertice = x - 0.5f;
                        rightXVertice = x;
                        bottomYVertice = y + 0.5f;
                        topYVertice = y + 1f;
                        leftXUV = 0.25f;
                        rightXUV = 0.5f;
                        bottomYUV = 5 / 6f;
                        topYUV = 1f;

                        AddSquare(texturePosition, leftXVertice, rightXVertice, bottomYVertice, topYVertice, z, leftXUV, rightXUV, bottomYUV, topYUV, layer, tileUnitX, tileUnitY);
                    }
                }
            }
        }

        /// <summary>
        /// Adds all the data to generate a square to the mesh property lists
        /// </summary>
        /// <param name="texturePosition">Position of the block texture</param>
        /// <param name="vertx1">Left x vertice</param>
        /// <param name="vertx2">Right x vertice</param>
        /// <param name="verty1">Bottom y vertice</param>
        /// <param name="verty2">Top y vertice</param>
        /// <param name="z">Z order</param>
        /// <param name="uvx1">Left uv x position</param>
        /// <param name="uvx2">Right uv x position</param>
        /// <param name="uvy1">Bottom uv y position</param>
        /// <param name="uvy2">Top uv y position</param>
        /// <param name="layer">The layer (submesh) which the traingles are added to</param>
        /// <param name="tileUnitX">Ratio of the block width to total texture width</param>
        /// <param name="tileUnitY">Ratio of the block height to total texture height</param>
        void AddSquare(Vector2 texturePosition, float vertx1, float vertx2, float verty1, float verty2, float z, float uvx1, float uvx2, float uvy1, float uvy2, int layer, float tileUnitX, float tileUnitY)
        {
            float epsilon = 0.0001f;

            vertices.Add(new Vector3(vertx1, verty1, z));
            vertices.Add(new Vector3(vertx1, verty2, z));
            vertices.Add(new Vector3(vertx2, verty2, z));
            vertices.Add(new Vector3(vertx2, verty1, z));
            
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 2);
            triangles[layer].Add(triangleOffset + 3);
            triangles[layer].Add(triangleOffset + 0);
            triangles[layer].Add(triangleOffset + 1);
            triangles[layer].Add(triangleOffset + 2);
            
            uv.Add(new Vector2(tileUnitX * (texturePosition.x + uvx1) + epsilon, tileUnitY * (texturePosition.y + uvy1) + epsilon));
            uv.Add(new Vector2(tileUnitX * (texturePosition.x + uvx1) + epsilon, tileUnitY * (texturePosition.y + uvy2) - epsilon));
            uv.Add(new Vector2(tileUnitX * (texturePosition.x + uvx2) - epsilon, tileUnitY * (texturePosition.y + uvy2) - epsilon));
            uv.Add(new Vector2(tileUnitX * (texturePosition.x + uvx2) - epsilon, tileUnitY * (texturePosition.y + uvy1) + epsilon));

            triangleOffset += 4;
        }

        /// <summary>
        /// Check if a bit is set in a bitmask
        /// </summary>
        /// <param name="bitmask">The bitmask to check</param>
        /// <param name="position">The position of the bit in the bitmask</param>
        /// <returns>Return true if the bit is set</returns>
        bool IsBitSet(byte bitmask, int position)
        {
            return (bitmask & (1 << position)) != 0;
        }

        /// <summary>
        /// Updates the mesh with the new mesh properties
        /// </summary>
        public void UpdateMesh()
        {
            //Reset the mesh
            mesh.Clear();
            mesh.subMeshCount = 0;
            //Add the data from the mesh property lists to the mesh
            mesh.SetVertices(vertices);
            vertices.Clear();
            mesh.SetUVs(0, uv);
            uv.Clear();
            if (useColors)
            {
                mesh.SetColors(colors);
                colors.Clear();
            }
            //Loop through all the triangle lists
            for (int i = 0; i < triangles.Length; i++)
            {
                //Check if the triangle list is not empty
                if (triangles[i].Count > 0)
                {
                    mesh.subMeshCount++;
                    //Add the triangles to the mesh
                    mesh.SetTriangles(triangles[i], mesh.subMeshCount - 1);
                    //If a layer contains triangles then it requires a material for its submesh
                    materialLayer[i] = true;
                    triangles[i].Clear();
                }
                else
                    materialLayer[i] = false;
            }
            //Clear all lists to be used again
            triangleOffset = 0;
        }

        /// <summary>
        /// Gets the block grid mesh
        /// </summary>
        /// <returns>Returns the mesh</returns>
        public Mesh GetMesh()
        {
            return mesh;
        }

        /// <summary>
        /// Check if a layer requires a material by index
        /// </summary>
        /// <param name="layer">The layer index</param>
        /// <returns>Returns true if the layer requires a material</returns>
        public bool IsMaterialLayer(int layer)
        {
            return materialLayer[layer];
        }

    }
}
