using System;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [Serializable]
    /// <summary>
    /// Block data to be saved to file
    /// </summary>
    public class BlockData : SaveData
    {
        [SerializeField]
        private byte[][,] blockType;
        /// <summary>
        /// Storage for all the blockType data
        /// </summary>
        public byte[][,] BlockType
        {
            get { return blockType; }
        }
        [SerializeField]
        private bool[][,] renderBlock;
        /// <summary>
        /// Storage for all the renderBlock data
        /// </summary>
        public bool[][,] RenderBlock
        {
            get { return renderBlock; }
        }

        /// <summary>
        /// Default constructor for BlockData
        /// Sets all the block data for saving
        /// </summary>
        public BlockData() : base()
        {
            blockType = new byte[worldData.BlockLayers.Length][,];
            renderBlock = new bool[worldData.BlockLayers.Length][,];
            for (int i = 0; i < worldData.BlockLayers.Length; i++)
            {
                blockType[i] = worldData.BlockLayers[i].BlockType;
                renderBlock[i] = worldData.BlockLayers[i].RenderBlock;
            }
        }
    }
}