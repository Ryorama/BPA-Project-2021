using UnityEngine;
using System;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [Serializable]
    /// <summary>
    /// Information of each block type
    /// </summary>
    public class BlockInfo
    {
        [SerializeField]
        [Tooltip("Name of the block type")]
        private string name;
        /// <summary>
        /// Name of the block type
        /// </summary>
        public string Name
        {
            get { return name; }
        }
        [SerializeField]
        [Tooltip("X position of the block's texture in the texture map (in block units)")]
        private float texturePositionX;
        /// <summary>
        /// X position of the block's texture in the texture map (in block units)
        /// </summary>
        public float TexturePositionX
        {
            get { return texturePositionX; }
        }
        private float textureXRelativePosition;
        /// <summary>
        /// The relative x position of the texture dependant on the width of the entire texture map
        /// </summary>
        public float TextureXRelativePosition
        {
            get { return textureXRelativePosition; }
        }
        [SerializeField]
        [Tooltip("Y position of the block's texture in the texture map (in block units)")]
        private float texturePositionY;
        /// <summary>
        /// Y position of the block's texture in the texture map (in block units)
        /// </summary>
        public float TexturePositionY
        {
            get { return texturePositionY; }
        }
        private float textureYRelativePosition;
        /// <summary>
        /// The relative y position of the texture dependant on the width of the entire texture map
        /// </summary>
        public float TextureYRelativePosition
        {
            get { return textureYRelativePosition; }
        }
        [SerializeField]
        [Tooltip("Width of the block's texture (in block units)")]
        private int textureWidth;
        /// <summary>
        /// Width of the block's texture (in block units)
        /// </summary>
        public int TextureWidth
        {
            get { return textureWidth; }
        }
        [SerializeField]
        [Tooltip("Height of the block's texture (in block units)")]
        private int textureHeight;
        /// <summary>
        /// Height of the block's texture (in block units)
        /// </summary>
        public int TextureHeight
        {
            get { return textureHeight; }
        }
        private float tileUnitX;
        /// <summary>
        /// Ratio of block's width to that of the entire texture (for UV mapping)
        /// </summary>
        public float TileUnitX
        {
            get { return tileUnitX; }
        }
        private float tileUnitY;
        /// <summary>
        /// Ratio of block's height to that of the entire texture (for UV mapping)
        /// </summary>
        public float TileUnitY
        {
            get { return tileUnitY; }
        }
        private bool multiBlock;
        /// <summary>
        /// Whether block has a multi-tile texture
        /// </summary>
        public bool MultiBlock
        {
            get { return multiBlock; }
        }
        [SerializeField]
        [Tooltip("Whether block is an Overlap block")]
        private bool overlapBlock;
        /// <summary>
        /// Whether block is an Overlap block
        /// </summary>
        public bool OverlapBlock
        {
            get { return overlapBlock; }
        }
        [SerializeField]
        [Tooltip("The number of different block variations (default is 1)")]
        private int numVariations;
        /// <summary>
        /// The number of different block variations (default is 1)
        /// </summary>
        public int NumVariations
        {
            get { return numVariations; }
        }
        [SerializeField]
        [Tooltip("Whether the block contains any transparent pixels")]
        private bool transparent;
        /// <summary>
        /// Whether the block has transparent pixels (this does not include the edges/corners of Overlap Blocks)
        /// Used for optimizing mesh generation
        /// </summary>
        public bool Transparent
        {
            get { return transparent; }
        }
        [SerializeField]
        [Tooltip("Whether this block falls with gravity")]
        private bool fallingBlock;
        /// <summary>
        /// Whether this block falls with gravity (if there is no block underneith it of the same layer it will fall down)
        /// </summary>
        public bool FallingBlock
        {
            get { return fallingBlock; }
        }

        /// <summary>
        /// Sets up the texture positioning and tile units for mesh building
        /// </summary>
        /// <param name="textureMapWidth">The width of the texture map containing the block (in pixels)</param>
        /// <param name="textureMapHeight">The height of the texture map containing the block (in pixels)</param>
        /// <param name="pixelsPerBlock">The number of pixels per block ratio</param>
        public void SetupBlockInfo(int textureMapWidth, int textureMapHeight, int pixelsPerBlock)
        {
            //Converts the block's texture position so it becomes a ratio of its size
            textureXRelativePosition = texturePositionX / textureWidth;
            textureYRelativePosition = texturePositionY / textureHeight;
            //Gets the blocks tile unit by finding the ratio of the blocks size to that of the entire texturemap
            tileUnitX = textureWidth / (textureMapWidth / (float)pixelsPerBlock);
            tileUnitY = textureHeight / (textureMapHeight / (float)pixelsPerBlock);
            //Sets whether this is a multiblock or not
            if ((textureWidth > 1 || textureHeight > 1) && !overlapBlock)
                multiBlock = true;
        }
    }
}