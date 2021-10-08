using UnityEngine;
using System;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [Serializable]
    /// <summary>
    /// A layer holding block type info and block data for the world
    /// </summary>
    public class BlockLayer
    {
        /// <summary>
        /// Offset for getting block info using the block type as index
        /// </summary>
        public const int BLOCK_INDEX_OFFSET = 1;
        /// <summary>
        /// The value representing the block type of a air/null block
        /// </summary>
        public const int AIR_BLOCK = 0;

        [SerializeField]
        [Tooltip("Colliders will be generated for this layer")]
        private bool colliderLayer;
        /// <summary>
        /// Colliders will be generated for this layer
        /// </summary>
        public bool ColliderLayer
        {
            get { return colliderLayer; }
        }
        [SerializeField]
        [Tooltip("Name of the layer")]
        private string name;
        /// <summary>
        /// Name of the layer
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        [SerializeField]
        [Tooltip("Tileset material for the layer")]
        private Material material;
        /// <summary>
        /// Tileset material for the layer
        /// </summary>
        public Material Material
        {
            get { return material; }
            set { material = value; }
        }

        private float zLayerOrder;
        /// <summary>
        /// The z position origin of the layer (for render order)
        /// </summary>
        public float ZLayerOrder
        {
            get { return zLayerOrder; }
        }

        private float zLayerOrderEnd;
        /// <summary>
        /// The z position of the final block of that layer (for render order)
        /// </summary>
        public float ZLayerOrderEnd
        {
            get { return zLayerOrderEnd; }
        }

        [SerializeField]
        //All the different block types of this layer
        private BlockInfo[] blockInfo;

        //The size of the world
        private int worldWidth;
        private int worldHeight;
        //The bitmask at each grid position (used for generating overlap blocks)
        private byte[,] bitmask;
        //The variation at each grid position
        private byte[,] variation;
        private byte[,] blockType;
        /// <summary>
        /// The block type at each grid position
        /// </summary>
        public byte[,] BlockType
        {
            get { return blockType; }
            set { blockType = value; }
        }
        private bool[,] renderBlock;
        /// <summary>
        /// Whether block should be rendered (for multi-tile blocks)
        /// </summary>
        public bool[,] RenderBlock
        {
            get { return renderBlock; }
            set { renderBlock = value; }
        }

        /// <summary>
        /// Sets up the block layer info and allocates storage for data
        /// </summary>
        /// <param name="worldWidth">Width of the world</param>
        /// <param name="worldHeight">Height of the world</param>
        /// <param name="pixelsPerBlock">Number of pixels per block (unit)</param>
        /// <param name="zBlockDistance">The z distance between blocks</param>
        /// <param name="zLayerFactor">The z distance factor between layers</param>
        /// <param name="prevLayer">The previous layer that was initialized</param>
        public void InitializeBlockLayer(int worldWidth, int worldHeight, int pixelsPerBlock, float zBlockDistance, int zLayerFactor, BlockLayer prevLayer)
        {
            this.worldWidth = worldWidth;
            this.worldHeight = worldHeight;
            //Allocates block data
            blockType = new byte[worldWidth, worldHeight];
            bitmask = new byte[worldWidth, worldHeight];
            variation = new byte[worldWidth, worldHeight];
            renderBlock = new bool[worldWidth, worldHeight];

            //Set the Z-Order of this layer to the previous layers Z-Order plus the z distance of all the block types in that layer
            //Unless the previous layer is null, then the Z-Order is simply zero since it is the first layer
            if(prevLayer != null)
                zLayerOrder = prevLayer.zLayerOrderEnd - zBlockDistance * zLayerFactor;
            //Set the end of the z layer order position to the z position of the final block
            zLayerOrderEnd = zLayerOrder - blockInfo.Length * zBlockDistance;

            //Sets up block info for each block type
            for (int i = 0; i < blockInfo.Length; i++)
            {
                blockInfo[i].SetupBlockInfo(material.mainTexture.width, material.mainTexture.height, pixelsPerBlock);
            }
        }
        /// <summary>
        /// Get the number of block types
        /// </summary>
        /// <returns>Returns the number of block types</returns>
        public int GetNumBlockTypes()
        {
            return blockInfo.Length;
        }
        /// <summary>
        /// Get the info for a block by blockType
        /// </summary>
        /// <param name="blockType">The type of block</param>
        /// <returns>Returns the block info</returns>
        public BlockInfo GetBlockInfo(int blockType)
        {
            if (blockType > 0)
                return blockInfo[blockType - BLOCK_INDEX_OFFSET];
            else
                return null;
        }
        /// <summary>
        /// Get the info for a block at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the block info</returns>
        public BlockInfo GetBlockInfo(int x, int y)
        {
            return GetBlockInfo(blockType[x, y]);
        }
        /// <summary>
        /// Sets the bitmask at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="bitmask">The bitmask value to be set</param>
        public void SetBitmask(int x, int y, byte bitmask)
        {
            this.bitmask[x, y] = bitmask;
        }
        /// <summary>
        /// Get the bitmask at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the bitmask</returns>
        public byte GetBitmask(int x, int y)
        {
            return bitmask[x, y];
        }
        /// <summary>
        /// Set the block variation at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="variation">The variation to be set</param>
        public void SetVariation(int x, int y, byte variation)
        {
            this.variation[x, y] = variation;
        }
        /// <summary>
        /// Get the block variation at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>The block variation</returns>
        public byte GetVariation(int x, int y)
        {
            return variation[x, y];
        }
        /// <summary>
        /// Determines if the block at the coordinate should be rendered
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns if the block should be rendered</returns>
        public bool IsRenderblock(int x, int y)
        {
            return renderBlock[x, y];
        }
        /// <summary>
        /// Get the block type at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the block type</returns>
        public byte GetBlockType(int x, int y)
        {
            if(InBounds(x, y))
                return blockType[x, y];
            return AIR_BLOCK;
        }
        /// <summary>
        /// Adds a block to a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="blockType">The block type to be set</param>
        /// <returns>Returns true if the block was added</returns>
        public bool AddBlock(int x, int y, byte blockType)
        {
            //Don't add block if the position is out of bounds or if the blockType is air
            if (!InBounds(x, y) || blockType == AIR_BLOCK)
                return false;
            BlockInfo blockInfo = GetBlockInfo(blockType);
            //If the block is not a multi-tile block, add the block
            if (!blockInfo.MultiBlock)
            {
                //---Single-tile block---
                this.blockType[x, y] = blockType;
                renderBlock[x, y] = true;
                return true;
            }
            else
            {
                //---Multi-tile block---
                int width = blockInfo.TextureWidth;
                int height = blockInfo.TextureHeight;
                //Check if there is enough free space to place the block
                if(IsFreeSpace(x, y, width, height))
                {
                    //Block renders from the selected position
                    renderBlock[x, y] = true;
                    //Set all the tiles taken up by the block to the blockType
                    for (int tileX = x; tileX < x + width; tileX++)
                    {
                        for (int tileY = y; tileY < y + height; tileY++)
                        {
                            this.blockType[tileX, tileY] = blockType;
                        }
                    }
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Adds a block to a specific coordinate
        /// *No safety checks
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="blockType">The block type to be set</param>
        public void AddBlockRaw(int x, int y, byte blockType)
        {
            BlockInfo blockInfo = GetBlockInfo(blockType);
            //Add the block
            if (!blockInfo.MultiBlock)
            {
                //---Single-tile block---
                this.blockType[x, y] = blockType;
                renderBlock[x, y] = true;
            }
            else
            {
                //---Multi-tile block---
                int width = blockInfo.TextureWidth;
                int height = blockInfo.TextureHeight;

                //Block renders from the selected position
                renderBlock[x, y] = true;
                //Set all the tiles taken up by the block to the blockType
                for (int tileX = x; tileX < x + width; tileX++)
                {
                    for (int tileY = y; tileY < y + height; tileY++)
                    {
                        this.blockType[tileX, tileY] = blockType;
                    }
                }
            }
        }

        /// <summary>
        /// Determine whether you can add a block at a specified coordinate
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <param name="blockType">The type of block to be added</param>
        /// <returns>Returns true if the block can be added</returns>
        public bool CanAddBlock(int x, int y, byte blockType)
        {
            //You can't add the block if the position is out of bounds or if the blockType is air
            if (!InBounds(x, y) || blockType == AIR_BLOCK)
                return false;
            BlockInfo blockInfo = GetBlockInfo(blockType);
            //If the block is not a multi-tile block, then it can be added
            if (!blockInfo.MultiBlock)
                return true;
            else
            {
                //---Multi-tile block---
                int width = blockInfo.TextureWidth;
                int height = blockInfo.TextureHeight;
                //Check if there is enough free space to place the block
                return IsFreeSpace(x, y, width, height);
            }
        }


        /// <summary>
        /// Removes a block from a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the render position</returns>
        public Vector2Int RemoveBlock(int x, int y)
        {
            //Get the info for the block
            BlockInfo blockInfo = GetBlockInfo(x, y);
            //Check if the block takes up multiple grid spaces
            if (!blockInfo.MultiBlock)
            {
                //---Single-tile block---
                renderBlock[x, y] = false;
                blockType[x, y] = 0;
                return new Vector2Int(x, y);
            } else
            {
                //---Multi-tile block---
                int width = blockInfo.TextureWidth;
                int height = blockInfo.TextureHeight;
                //Find the block's render position (bottom left point)
                Vector2Int renderPos = FindRenderPosition(x, y, width, height, blockType[x, y]);
                //Remove the block from the array for rendering
                renderBlock[renderPos.x, renderPos.y] = false;
                //Set all the blockTypes within the blocks area to air
                for (int posX = renderPos.x; posX < renderPos.x + width; posX++)
                {
                    for (int posY = renderPos.y; posY < renderPos.y + height; posY++)
                    {
                        blockType[posX, posY] = 0;
                    }
                }
                return renderPos;
            }
        }

        /// <summary>
        /// Searches for the render position of a multiTile block using a coordinate within the block's area
        /// </summary>
        /// <param name="x">X coordinate within the block's area</param>
        /// <param name="y">Y coordinate within the block's area</param>
        /// <param name="width">Width of the block</param>
        /// <param name="height">Height of the block</param>
        /// <returns>Return the coordinate of the render position</returns>
        Vector2Int FindRenderPosition(int x, int y, int width, int height, byte blockType)
        {
            //Iterate to the left and down untill the render position is found for the block
            for(int posX = x; posX > x - width; posX--)
            {
                for(int posY = y; posY > y - height; posY--)
                {
                    //If this point has the same blockType and is a render block
                    if (GetBlockType(posX, posY) == blockType && renderBlock[posX, posY])
                        //Return the render position
                        return new Vector2Int(posX, posY);
                }
            }
            throw new Exception("Could not find the render block");
        }
        

        /// <summary>
        /// Check if there is free space in specific area
        /// </summary>
        /// <param name="x">X position of area (Left)</param>
        /// <param name="y">Y position of area (Bottom)</param>
        /// <param name="width">Width of area</param>
        /// <param name="height">Height of area</param>
        /// <returns>Returns true if there is free space</returns>
        bool IsFreeSpace(int x, int y, int width, int height)
        {
            //Loop through all blocks in the area
            for (int posX = x; posX < x + width; posX++)
            {
                for (int posY = y; posY < y + height; posY++)
                {
                    //If the current position is out of bounds or there is a block there
                    if (!InBounds(posX, posY) || blockType[posX, posY] != AIR_BLOCK)
                        //There is not enough free space
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks to see if there is a block at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns true if a block exists</returns>
        public bool IsBlockAt(int x, int y)
        {
            if (InBounds(x, y))
                return blockType[x, y] != AIR_BLOCK;
            return false;
        }
        /// <summary>
        /// Checks to see if a coordinate is within world bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns true if the coordinate is within bounds</returns>
        bool InBounds(int x, int y)
        {
            if (x < 0 || x >= worldWidth || y < 0 || y >= worldHeight)
                return false;
            return true;
        }
    }
}