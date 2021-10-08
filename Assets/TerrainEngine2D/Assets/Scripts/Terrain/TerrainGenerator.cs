using System.Collections.Generic;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [DisallowMultipleComponent]
    /// <summary>
    /// Base class for procedurally generating world block data
    /// </summary>
    public abstract class TerrainGenerator : MonoBehaviour
    {
        private const int IterationCap = 10000;
        protected World world;
        protected WorldData worldData;
        protected FluidDynamics fluidDynamics;

        private bool initialized;

        //Temporarily holding key sequence values for generating fluid data
        protected Vector2Int[,] fluidKey;
        //Value used for procedurally generating the block data
        protected int seed;
        //Random for randomly setting blocks
        protected System.Random random;

        /// <summary>
        /// Setup the variables used in generating the world
        /// </summary>
        /// <param name="world">Reference to the World</param>
        /// <param name="worldData">Reference to the World Data</param>
        /// <param name="fluidDynamics">Reference to the Fluid Dynamics system</param>
        public void Initialize(World world, WorldData worldData, FluidDynamics fluidDynamics)
        {
            this.world = world;
            this.worldData = worldData;
            this.fluidDynamics = fluidDynamics;
            fluidKey = new Vector2Int[world.WorldWidth, world.WorldHeight];
            seed = world.Seed;
            random = new System.Random(seed);
            initialized = true;
        }

        /// <summary>
        /// Procedurally generates world block data using random and pseudo-random functions
        /// </summary>
        /// <param name="world">Reference to the world to access block arrays</param>
        public virtual void GenerateData()
        {
            if (!initialized)
                Initialize(World.Instance, World.WorldData, FluidDynamics.Instance);
        }

        /// <summary>
        /// Perlin Noise Function for generating pseudo-random noise
        /// </summary>
        /// <param name="x">Block x coordinate</param>
        /// <param name="y">Block y coordinate</param>
        /// <param name="frequency">Controls the variability of the output (lower values produce more variable output)</param>
        /// <param name="scale">Controls the size of the output (higher value means larger terrain)</param>
        /// <returns>Returns the noise value</returns>
        protected int PerlinNoise(int x, int y, float frequency, float scale)
        {
            float noiseX = ((x + seed) / frequency);
            float noiseY = ((y + seed) / frequency);
            float noise = Mathf.PerlinNoise(noiseX, noiseY);
            return (int) (noise * scale);
        }

        /// <summary>
        /// Generate a random value using Perlin Noise to use for terrain height
        /// </summary>
        /// <param name="x">Block x coordinate</param>
        /// <param name="scale">Controls the size of the output (higher value means larger terrain)</param>
        /// <param name="baseHeight">A base value to use for the height of the terrain (the terrain
        ///                             will not go below this value)</param>
        /// <returns>Returns the noise value</returns>
        protected int RandomHeight(int x, int scale, int baseHeight)
        {
            int noise = PerlinNoise(x, 0, 7 * scale, 2 * scale);
            noise += PerlinNoise(x, 0, 4 * scale, 3 * scale);
            noise += PerlinNoise(x, 0, 1 * scale, 1 * scale);
            noise += baseHeight; 
            return noise;
        }

        /// <summary>
        /// Sets the world block data of a specified layer to a specific block
        /// </summary>
        /// <param name="x">Block x coordinate</param>
        /// <param name="y">Block y coordinate</param>
        /// <param name="layer">Layer to set the block data</param>
        /// <param name="blockType">The block type which is being set</param>
        /// <param name="probability">Percent chance of setting block (0 - 100)</param>
        /// <returns>Returns true if the block is set</returns>
        protected bool SetBlock(int x, int y, byte layer, byte blockType, float probability = 100f)
        {
            //Determine if the block should be added using the set probability
            if (!DoAddBlock(probability))
                return false;

            world.GetBlockLayer(layer).AddBlock(x, y, (byte)(blockType + BlockLayer.BLOCK_INDEX_OFFSET));
            //Set the fluid blocks to solid if working with the fluid layer
            if (layer == world.FluidLayer)
            {
                //Get the block information
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(x, y);
                if (blockInfo.MultiBlock)
                {
                    //Loop through the size of the block
                    for (int posX = x; posX < x + blockInfo.TextureWidth; posX++)
                    {
                        for (int posY = y; posY < y + blockInfo.TextureHeight; posY++)
                        {
                            //If the block is in bounds set the fluid block to solid
                            if (InBounds(posX, posY))
                            {
                                fluidDynamics.GetFluidBlock(posX, posY).SetSolid();
                            }
                        }
                    }
                }
                else
                {
                    fluidDynamics.GetFluidBlock(x, y).SetSolid();
                }
            }
            return true;
        }

        /// <summary>
        /// Checks if block should be set based on probability
        /// </summary>
        /// <param name="probability">The odds of setting the block</param>
        /// <returns>Returns true if block should be set</returns>
        protected bool DoAddBlock(double probability)
        {
            return (random.Next(100) + random.NextDouble()) <= probability;
        }

        /// <summary>
        /// Removes blocks from a specified layer and coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="layer">Layer to remove the block from</param>
        /// <returns>Returns true if the block is successfully removed</returns>
        protected bool RemoveBlock(int x, int y, byte layer)
        {
            //If there is a block at the coordinates
            if (world.GetBlockLayer(layer).IsBlockAt(x, y))
            {
                //Get the block information to get the size of the block
                BlockInfo blockInfo = world.GetBlockLayer(layer).GetBlockInfo(x, y);
                //Remove the block and get its position
                Vector2Int blockPos = world.GetBlockLayer(layer).RemoveBlock(x, y);
                //If working with the fluid layer, set all the blocks to empty
                if (layer == world.FluidLayer)
                {
                    if (blockInfo.MultiBlock)
                        RemoveFluid(blockPos.x, blockPos.y, blockInfo.TextureWidth, blockInfo.TextureHeight);
                    else
                    {
                        fluidDynamics.GetFluidBlock(x, y).SetEmpty();
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes blocks from all layers at a coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        protected void RemoveAllBlocks(int x, int y)
        {
            //Loop through all the block layers and remove the blocks
            for(int i = 0; i < world.NumBlockLayers; i++)
            {
                RemoveBlock(x, y, (byte)i);
            }
        }


        /// <summary>
        /// Checks to see if a coordinate is within world bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns true if the coordinate is within bounds</returns>
        bool InBounds(int x, int y)
        {
            if (x < 0 || x >= world.WorldWidth || y < 0 || y >= world.WorldHeight)
                return false;
            return true;
        }

        /// <summary>
        /// Checks if there is a block at a specific coordinate or area in the layer
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="layer">Layer to be checked</param>
        /// <param name="width">The width of the area (in blocks)</param>
        /// <param name="height">The height of the area (in blocks)</param>
        /// <returns>Returns true if there is a block</returns>
        protected bool IsBlockAt(int x, int y, byte layer, int width = 1, int height = 1)
        {
            bool isBlockAt = false;
            for(int i = x; i < x + width; i++)
            {
                for(int j = y; j < y + height; j++)
                {
                    if (world.GetBlockLayer(layer).IsBlockAt(i, j))
                    {
                        isBlockAt = true;
                        break;
                    }
                }
            }
            return isBlockAt;
        }

        /// <summary>
        /// Add fluid to a block at a coordinate (Basic)
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="weight">Amount of fluid to add</param>
        /// <returns>Returns true if fluid is successfully added</returns>
        protected bool AddFluid(int x, int y, float weight)
        {
            //Don't add any fluid if fluid is disabled
            if (world.FluidDisabled)
                return false;
            if (!world.BasicFluid)
                throw new System.Exception("Using the wrong function, fluid is being added to the Basic Fluid System instead of the Advanced");
            //Adds fluid to the block if it is not solid and is empty or of the same density
            FluidBlock fluidBlock = fluidDynamics.GetFluidBlock(x, y);
            if (!fluidBlock.IsSolid())
            {
                fluidDynamics.GetFluidBlock(x, y).AddWeight(weight);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add fluid to a block at a coordinate (Advanced)
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="weight">Amount of fluid to add</param>
        /// <param name="density">The density of the fluid (or type)</param>
        /// <returns>Returns true if fluid is successfully added</returns>
        protected bool AddFluid(int x, int y, float weight, byte density)
        {
            //Don't add any fluid if fluid is disabled
            if (world.FluidDisabled)
                return false;
            if (world.BasicFluid)
                throw new System.Exception("Using the wrong function, fluid is being added to the Advanced Fluid System instead of the Basic");
            //Adds fluid to the block if it is not solid and is empty or of the same density
            AdvancedFluidBlock fluidBlock = (AdvancedFluidBlock)fluidDynamics.GetFluidBlock(x, y);
            if (!fluidBlock.IsSolid() && (fluidBlock.Weight == 0 || fluidBlock.Density == density))
            { 
                fluidBlock.AddWeight(density, weight, worldData.FluidTypes[density].DefaultColor);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add fluid to a block at a coordinate (Advanced)
        /// Custom Color
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="weight">Amount of fluid to add</param>
        /// <param name="density">The density of the fluid (or type)</param>
        /// <param name="customColor">Use a custom color instead of the fluid type default</param>
        /// <returns>Returns true if fluid is successfully added</returns>
        protected bool AddFluid(int x, int y, float weight, byte density, Color32 customColor)
        {
            //Don't add any fluid if fluid is disabled
            if (world.FluidDisabled)
                return false;
            if (world.BasicFluid)
                throw new System.Exception("Using the wrong function, fluid is being added to the Advanced Fluid System instead of the Basic");
            //Adds fluid to the block if it is not solid and is empty or of the same density
            AdvancedFluidBlock fluidBlock = (AdvancedFluidBlock)fluidDynamics.GetFluidBlock(x, y);
            if (!fluidBlock.IsSolid() && (fluidBlock.Weight == 0 || fluidBlock.Density == density))
            {
                fluidBlock.AddWeight(density, weight, customColor);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove fluid from an area
        /// </summary>
        /// <param name="x">X coordinate of area</param>
        /// <param name="y">Y coordinate of area</param>
        /// <param name="width">Width of area</param>
        /// <param name="height">Height of area</param>
        protected void RemoveFluid(int x, int y, int width = 1, int height = 1)
        {
            //Loop through the area
            for (int posX = x; posX < x + width; posX++)
            {
                for (int posY = y; posY < y + height; posY++)
                {
                    //If the block is in bounds set the fluid position to empty
                    if (InBounds(posX, posY))
                    {
                        if (!fluidDynamics.GetFluidBlock(posX, posY).IsSolid())
                            fluidDynamics.GetFluidBlock(posX, posY).SetEmpty();
                    }
                }
            }
        }

        /// <summary>
        /// Generate a pool of fluid below a threshold, previously set fluid blocks will be overwritten (Basic)
        /// </summary>
        /// <param name="x">Fluid block x coordinate</param>
        /// <param name="y">Fluid block y coordinate</param>
        /// <param name="weight">Amount of fluid added to the block</param>
        /// <param name="maxY">Heighest fluid point</param>
        /// <param name="key">The starting coordinate</param>
        protected void GeneratePool(int x, int y, float weight, int maxY, Vector2Int key)
        {
            if (!world.BasicFluid)
            {
                Debug.LogError("Using the wrong function, fluid is being added to the Basic Fluid System instead of the Advanced");
                return;
            }
            GeneratePoolInternal(x, y, weight, 0, worldData.MainFluidColor, maxY, key);
        }

        /// <summary>
        /// Generate a pool of fluid below a threshold, previously set fluid blocks will be overwritten (Advanced)
        /// </summary>
        /// <param name="x">Fluid block x coordinate</param>
        /// <param name="y">Fluid block y coordinate</param>
        /// <param name="weight">Amount of fluid added to the block</param>
        /// <param name="density">The density of the fluid (or type) - used by the Advanced Fluid Dynamics</param>
        /// <param name="maxY">Heighest fluid point</param>
        /// <param name="key">The starting coordinate</param>
        protected void GeneratePool(int x, int y, float weight, byte density, int maxY, Vector2Int key) {
            if (world.BasicFluid)
            {
                Debug.LogError("Using the wrong function, fluid is being added to the Advanced Fluid System instead of the Basic");
                return;
            }
            GeneratePoolInternal(x, y, weight, density, worldData.FluidTypes[density].DefaultColor, maxY, key);
        }

        /// <summary>
        /// Generate a pool of fluid below a threshold of a custom colour, previously set fluid blocks will be overwritten (Advanced)
        /// </summary>
        /// <param name="x">Fluid block x coordinate</param>
        /// <param name="y">Fluid block y coordinate</param>
        /// <param name="weight">Amount of fluid added to the block</param>
        /// <param name="density">The density of the fluid (or type) - used by the Advanced Fluid Dynamics</param>
        /// <param name="customColor">Use a custom color instead of the fluid type default</param>
        /// <param name="maxY">Heighest fluid point</param>
        /// <param name="key">The starting coordinate</param>
        protected void GeneratePool(int x, int y, float weight, byte density, Color32 customColor, int maxY, Vector2Int key)
        {
            if (world.BasicFluid)
            {
                Debug.LogError("Using the wrong function, fluid is being added to the Advanced Fluid System instead of the Basic");
                return;
            }   
            GeneratePoolInternal(x, y, weight, density, customColor, maxY, key); 
        }

        private void GeneratePoolInternal(int x, int y, float weight, byte density, Color32 customColor, int maxY, Vector2Int key)
        {
             //Don't generate any pools if fluid is disabled
            if (world.FluidDisabled)
                return;

            //Return if the current position is outside world bounds
            if (x < 0 || x >= world.WorldWidth || y < 0 || y >= world.WorldHeight)
                return;
            
            //Don't generate a new pool if there is already fluid in the current position
            if (fluidDynamics.GetFluidBlock(x, y).Weight > 0)
                return;
            
            var fluidBlockLocations = new Queue<Vector2>();
            var fluidBlocksWeight = new Queue<float>();
            var fluidBlocks = new Queue<FluidBlock>();
            fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y));
            fluidBlockLocations.Enqueue(new Vector2(x, y));
            fluidBlocksWeight.Enqueue(weight);
            int iterationCount = 0;
            while (fluidBlocks.Count > 0 && iterationCount < IterationCap)
            {
                var fluidBlock = fluidBlocks.Dequeue();
                var fluidBlockLocation = fluidBlockLocations.Dequeue();
                var fluidWeight = fluidBlocksWeight.Dequeue();
                
                x = (int)fluidBlockLocation.x;
                y = (int)fluidBlockLocation.y;
                
                //Empty block if it is not a part of this sequence
                if (fluidKey[x, y] != key && fluidBlock.Weight > 0)
                    fluidBlock.Weight = 0;
                
                //Add fluid if block is empty
                if (fluidBlock.Weight == 0)
                {
                    //Adds fluid to the block
                    fluidBlock.Weight = fluidWeight;
                    fluidBlock.Stable = true;
                    if (!worldData.BasicFluid)
                    {
                        ((AdvancedFluidBlock)fluidBlock).Density = density;
                        ((AdvancedFluidBlock)fluidBlock).Color = customColor;
                    }

                    //Set the key for the current block
                    fluidKey[x, y] = key;
                    //Add fluid to adjacent blocks if below the threshold
                    
                    if (x - 1 >= 0)
                    {
                        fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x - 1, y));
                        fluidBlockLocations.Enqueue(new Vector2(x - 1, y));
                        fluidBlocksWeight.Enqueue(fluidWeight);
                    }
                    if (x + 1 < world.WorldWidth)
                    {
                        fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x + 1, y));
                        fluidBlockLocations.Enqueue(new Vector2(x + 1, y));
                        fluidBlocksWeight.Enqueue(fluidWeight);
                    }
                    if (y - 1 >= 0)
                    {
                        fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y - 1));
                        fluidBlockLocations.Enqueue(new Vector2(x, y - 1));
                        fluidBlocksWeight.Enqueue(fluidWeight + fluidDynamics.PressureWeight);
                    }
                    if (y + 1 < world.WorldHeight && y < maxY)
                    {
                        fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y + 1));
                        fluidBlockLocations.Enqueue(new Vector2(x, y + 1));
                        fluidBlocksWeight.Enqueue(fluidWeight);
                    }
                }
                iterationCount++;
            }
            
            if (iterationCount >= IterationCap)
                Debug.LogWarning("Reached the iteration cap");
        }

        /// <summary>
        /// Recursive function for removing fluid below a threshold
        /// </summary>
        /// <param name="x">Fluid block x coordinate</param>
        /// <param name="y">Fluid block y coordinate</param>
        /// <param name="maxY">Heighest fluid point</param>
        protected void ClearFluid(int x, int y, int maxY)
        {
            //Don't clear if fluid is disabled
            if (world.FluidDisabled)
                return;
            
            //Return if the current position is outside world bounds
            if (x < 0 || x >= world.WorldWidth || y < 0 || y >= world.WorldHeight)
                return;

            var fluidBlockLocations = new Queue<Vector2>();
            var fluidBlocks = new Queue<FluidBlock>();
            fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y));
            fluidBlockLocations.Enqueue(new Vector2(x, y));
            int iterationCount = 0;
            while (fluidBlocks.Count > 0 && iterationCount < IterationCap)
            {
                var fluidBlock = fluidBlocks.Dequeue();
                var fluidBlockLocation = fluidBlockLocations.Dequeue(); 
                if (fluidBlock.Weight <= 0)
                    continue;
                fluidBlock.Weight = 0;
                
                x = (int)fluidBlockLocation.x;
                y = (int)fluidBlockLocation.y;
                fluidKey[x, y] = Vector2Int.zero;
                ;
                //Remove fluid to adjacent blocks if below the threshold
                if (x - 1 >= 0)
                {
                    fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x - 1, y));
                    fluidBlockLocations.Enqueue(new Vector2(x - 1, y));
                }
                if (x + 1 < world.WorldWidth)
                {
                    fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x + 1, y));
                    fluidBlockLocations.Enqueue(new Vector2(x + 1, y));
                }
                if (y - 1 >= 0)
                {
                    fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y - 1));
                    fluidBlockLocations.Enqueue(new Vector2(x, y - 1));
                }
                if (y + 1 < world.WorldHeight && y < maxY)
                {
                    fluidBlocks.Enqueue(fluidDynamics.GetFluidBlock(x, y + 1));
                    fluidBlockLocations.Enqueue(new Vector2(x, y + 1));
                }

                iterationCount++;
            }
            
            if (iterationCount >= IterationCap)
                Debug.LogError("Reached the iteration cap");
        }
    }
}
