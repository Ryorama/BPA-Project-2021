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
        public enum WorldLayers { Main, Background }

        public enum MainLayer { Dirt, Stone, Grass }

        public enum BackgroundLayer { Stump, Trunk, Top }

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

                        //Add foreground elements and trees to the top of the ground level if there is a main block there
                        if (y == groundLevel && IsBlockAt(x, y, (byte)WorldLayers.Main))
                        {
                            //Places a tree with 10% probability
                            if (DoAddBlock(10))
                            {
                                bool genTree = true;
                                //Check for enough room to place a stump
                                if (IsBlockAt(x - 1, y + 1, (byte)WorldLayers.Main))
                                    genTree = false;
                                //Check for a tree within 7 blocks away
                                for (int xTree = x - 6; xTree < x; xTree++)
                                {
                                    //Check for blocks of trees at a few different vertical positions (since tree sizes and positions vary)
                                    if (IsBlockAt(xTree, y + 2, (byte)WorldLayers.Background) || IsBlockAt(xTree, y + 5, (byte)WorldLayers.Background) || IsBlockAt(xTree, y + 8, (byte)WorldLayers.Background))
                                    {
                                        genTree = false;
                                        break;
                                    }
                                }
                                //Generate a tree if there are none nearby
                                if (genTree)
                                {
                                    //Set stump at a horizontal offset since it is 3 blocks wide
                                    SetBlock(x - 1, y + 1, (byte)WorldLayers.Background, (byte)BackgroundLayer.Stump);
                                    //Randomly generate a tree height between 5 and 12 blocks high
                                    int treeHeight = random.Next(2, 5);
                                    //Loop up the height of the tree
                                    for (int yTree = y + 2; yTree < y + treeHeight; yTree++)
                                    {
                                        SetBlock(x, yTree, (byte)WorldLayers.Background, (byte)BackgroundLayer.Trunk);
                                    }
                                    //Finish the tree with the crown 
                                    SetBlock(x - 3, y + treeHeight, (byte)WorldLayers.Background, (byte)BackgroundLayer.Top);
                                }
                            }
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
                    if (y < groundLevel - 20)
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

