using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Simulates the falling of blocks under gravity
    /// </summary>
    public class FallingBlockSimulation : MonoBehaviourSingleton<FallingBlockSimulation>
    {
        private World world;
        //Information about each falling block in the map
        //0 = air
        //1 = solid
        //2 = falling block
        private byte[,] fallingBlocks;

        private byte fallingBlockLayerIndex;
        //Reference to the layer which contains falling blocks
        private BlockLayer fallingBlockLayer;

        //Reference to the width and height of the world
        private int worldWidth, worldHeight;

        //whether to update the falling block simulation
        private bool updateFallingBlocks;

        //The rate at which the simulation will update
        protected float updateTimer;
        private float updateRate = 0.05f;
        public float UpdateRate
        {
            get { return updateRate; }
        }

        /// <summary>
        /// Actions invoked when a block falls
        /// </summary>
        public delegate void BlockFallAction(int x, int y, byte layer);
        /// <summary>
        /// Event called when a block falls
        /// </summary>
        public static event BlockFallAction OnBlockFall;

        private void OnEnable()
        {
            //Update the falling block simulation when chunks are loaded or blocks are placed/removed
            ChunkLoader.OnChunksLoaded += UpdateFallingBlocks;
            World.OnBlockPlaced += OnBlockAdded;
            World.OnBlockRemoved += OnBlockRemoved;
        }

        private void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= UpdateFallingBlocks;
            World.OnBlockPlaced -= OnBlockAdded;
            World.OnBlockRemoved -= OnBlockRemoved;
        }

        protected override void Awake()
        {
            base.Awake();
            //Set Properties
            updateRate = World.WorldData.FallingBlocksUpdateRate;
        }

        private void Start()
        {
            world = World.Instance;
            fallingBlockLayer = world.GetBlockLayer(world.FallingBlockLayer);
            fallingBlockLayerIndex = world.FallingBlockLayer;
            worldWidth = world.WorldWidth;
            worldHeight = world.WorldHeight;

            //Setup the falling blocks array to map the world terrain
            fallingBlocks = new byte[worldWidth, worldHeight];
            for (int x = 0; x < worldWidth; x++)
            {
                for (int y = 0; y < worldHeight; y++)
                {
                    if (fallingBlockLayer.IsBlockAt(x, y))
                    {
                        BlockInfo blockInfo = fallingBlockLayer.GetBlockInfo(x, y);
                        if (blockInfo.FallingBlock)
                            fallingBlocks[x, y] = 2; //Falling Block
                        else
                            fallingBlocks[x, y] = 1; //Solid Block
                    }
                    else
                        fallingBlocks[x, y] = 0; //Air Block
                }
            }
            UpdateFallingBlocks();
        }

        private void Update()
        {
            //Checks if it is time to update the fluid
            if (updateTimer >= updateRate)
            {
                //Updates the fluid if it needs updating
                if (updateFallingBlocks)
                {
                    Simulate();
                    updateTimer = 0;
                }
            } else
                updateTimer += Time.deltaTime;
        }

        /// <summary>
        /// Simulate the falling blocks
        /// </summary>
        private void Simulate()
        {
            if (!updateFallingBlocks)
                return;
            updateFallingBlocks = false;

            Vector2Int origin = ChunkLoader.Instance.OriginLoadedChunks;
            Vector2Int endPoint = ChunkLoader.Instance.EndPointLoadedChunks + new Vector2Int(ChunkLoader.Instance.ChunkSize, ChunkLoader.Instance.ChunkSize);

            //Loop through all the blocks of the falling block layer of the loaded chunks
            for(int x = origin.x; x < endPoint.x; x++)
            {
                for(int y = origin.y; y < endPoint.y; y++)
                {
                    //Check if the block is a falling block
                    if (fallingBlocks[x, y] != 2)
                        continue;
                    //Skip block if it is at the bottom of the world
                    if (y == 0)
                        continue;
                    //Check if there is no block under the falling block
                    if(fallingBlocks[x, y - 1] == 0)
                    {
                        byte blockType = fallingBlockLayer.BlockType[x, y];
                        if(OnBlockFall != null)
                            OnBlockFall(x, y, fallingBlockLayerIndex);
                        world.RemoveBlock(x, y, fallingBlockLayerIndex);
                        world.AddBlock(x, y - 1, blockType, fallingBlockLayerIndex);
                        updateFallingBlocks = true;
                    }

                }
            }
        }

        /// <summary>
        /// Update the falling block simulation
        /// </summary>
        public void UpdateFallingBlocks()
        {
            updateFallingBlocks = true;
        }

        /// <summary>
        /// Update the simulation if a falling block is added
        /// </summary>
        /// <param name="x">X coordinate of the added block</param>
        /// <param name="y">Y coordinate of the added block</param>
        /// <param name="layer">The layer the block was added to</param>
        /// <param name="blockType">The type of block added</param>
        private void OnBlockAdded(int x, int y, byte layer, byte blockType)
        {
            if (layer != world.FallingBlockLayer)
                return;
            BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
            if (blockInfo.FallingBlock)
            {
                fallingBlocks[x, y] = 2;
                UpdateFallingBlocks();
            }
            else
            {
                if (blockInfo.MultiBlock) {
                    for (int i = x; i < x + blockInfo.TextureWidth; i++)
                    {
                        for(int j = y; j < y + blockInfo.TextureHeight; j++)
                        {
                            fallingBlocks[i, j] = 1;
                        }
                    }
                } else
                    fallingBlocks[x, y] = 1;

            }
        }

        /// <summary>
        /// Update the simulation if a block is removed below a falling block
        /// </summary>
        /// <param name="x">X coordinate of the removed block</param>
        /// <param name="y">Y coordinate of the removed block</param>
        /// <param name="layer">The layer the block was removed from</param>
        /// <param name="blockType">The type of block removed</param>
        private void OnBlockRemoved(int x, int y, byte layer, byte blockType)
        {
            if (layer != world.FallingBlockLayer)
                return;
            BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(blockType);
            if (blockInfo.MultiBlock)
            {
                for (int i = x; i < x + blockInfo.TextureWidth; i++)
                {
                    for (int j = y; j < y + blockInfo.TextureHeight; j++)
                    {
                        fallingBlocks[i, j] = 0;
                        if (fallingBlocks[i, j + 1] == 2)
                            UpdateFallingBlocks();
                    }
                }
            }
            else
            {
                fallingBlocks[x, y] = 0;
                if (fallingBlocks[x, y + 1] == 2)
                    UpdateFallingBlocks();
            }
        }
    }
}