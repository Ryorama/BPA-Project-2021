using UnityEngine;
using TerrainEngine2D;

// Copyright (C) 2020 Matthew Wilson

namespace Game
{
    public class OverwoldTerrainGenerator : TerrainGenerator
    {
        /// <summary>
        /// Procedurally generates world block data using random and pseudo-random functions
        /// </summary>
        /// 
        public enum WorldLayers { Main }

        public enum MainLayer { Dirt, Stone, Grass }
        //Fluid types
        public enum FluidType { Water, Lava }

        public override void GenerateData()
        {
            base.GenerateData();
            //Pass 1
            for (int x = 0; x < world.WorldWidth; x++)
            {
                //-----Add height variables here-----
                int groundLevel = RandomHeight(x, 10, world.WorldHeight / 2);
                int stoneLevel = RandomHeight(x, 15, world.WorldHeight / 4);
                //----------
                for (int y = 0; y < world.WorldHeight; y++)
                {
                    //-----Set block data here-----
                    if (y <= groundLevel)
                    {
                        //Start with a layer of dirt covering the whole ground level

                        if (y == groundLevel)
                        {
                            SetBlock(x, y, (byte)WorldLayers.Main, (byte)MainLayer.Grass);
                        } else
                        {
                            SetBlock(x, y, (byte)WorldLayers.Main, (byte)MainLayer.Dirt);
                        }
                        //Set blocks below a random level (5 to 19 blocks below ground level) to rock/hard dirt and add ore
                        if (y < groundLevel - random.Next(5, 20))
                        {
                            //Place large rock chunks in pseudo random positions and add clumps of ore
                            if (PerlinNoise(x * 4, y, 5, 6) > 1)
                            {
                                SetBlock(x, y, (byte)WorldLayers.Main, (byte)MainLayer.Stone);
                                //Place ore in pseudo-random positions in rocks
                            }
                            //Add hard dirt clumps in pseudo random positions
                            if (PerlinNoise(x, y, 15, 16) > 8)
                            {
                                SetBlock(x, y, (byte)WorldLayers.Main, (byte)MainLayer.Stone);
                            }
                        }
                        //Remove clumps of main and ore to create caves
                        if (PerlinNoise(x, y, 10, 10) > 5)
                        {
                            RemoveBlock(x, y, (byte)WorldLayers.Main);
                        }
                    }
                }
            }

            //Pass 2 ...
            for (int x = 0; x < world.WorldWidth; x++)
            {
                //-----Add height variables here-----
                int groundLevel = RandomHeight(x, 10, world.WorldHeight / 2);
                //----------
                for (int y = 0; y < world.WorldHeight; y++)
                {
                    //-----Set block data here-----
                    if (y < groundLevel)
                    {
                        //Add water to caves
                        if (!IsBlockAt(x, y, world.FluidLayer))
                        {
                            byte density = (byte)FluidType.Water;
                            if (DoAddBlock(15))
                                density = (byte)FluidType.Lava;

                            //Generate a pool of water with 0.5% probability
                            if (DoAddBlock(0.5f))
                                GeneratePool(x, y, fluidDynamics.MaxWeight, density, y, new Vector2Int(x, y));
                        }
                    }
                }
            }
        }
    }
}

