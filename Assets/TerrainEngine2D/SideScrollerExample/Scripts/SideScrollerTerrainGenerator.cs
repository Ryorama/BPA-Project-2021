using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.SideScrollerDemo
{
    //Layer types
    public enum Layers { Background, Tree, Main, Ore, Foreground }
    //Block types
    public enum BackgroundLayer { Dirt, Stone, Wood }
    public enum TreeLayer { Stump, Trunk, Crown }
    public enum MainLayer { PurpleDirt, PurpleRock, DarkRock, GreenRock, BedRock, Gravel }
    public enum OreLayer { Gems, Coal, Silver }
    public enum ForegroundLayer { Grass, Flower, VineEnd, VineMiddle, VineStart }
    //Fluid types
    public enum FluidType { Water, Poison, Lava }

    public class SideScrollerTerrainGenerator : TerrainGenerator
    {
        /// <summary>
        /// Procedurally generates world block data using random and pseudo-random functions
        /// </summary>
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
                        SetBlock(x, y, (byte)Layers.Background, (byte)BackgroundLayer.Dirt);
                        SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.PurpleRock);
                        SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.PurpleDirt, 20);
                        //Cover the background with stone if below the stone level
                        if (y <= stoneLevel)
                            SetBlock(x, y, (byte)Layers.Background, (byte)BackgroundLayer.Stone);
                        //Set blocks below a random level (5 to 19 blocks below ground level) to rock/hard dirt and add ore
                        if (y < groundLevel - random.Next(5, 20))
                        {
                            //Place large rock chunks in pseudo random positions and add clumps of ore
                            if (PerlinNoise(x * 4, y, 5, 6) > 1)
                            {
                                SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.DarkRock);
                                //Place ore in pseudo-random positions in rocks
                                if (PerlinNoise(x, y * 2, 9, 12) > 8)
                                    SetBlock(x, y, (byte)Layers.Ore, (byte)OreLayer.Coal);
                                if (PerlinNoise(x, y * 2, 10, 12) > 8)
                                    SetBlock(x, y, (byte)Layers.Ore, (byte)OreLayer.Silver);
                                //Place gems randomly in rocks (about 1% chance to place a gem)
                                SetBlock(x, y, (byte)Layers.Ore, (byte)OreLayer.Gems, 1);
                            }
                            //Add hard dirt clumps in pseudo random positions
                            if (PerlinNoise(x, y, 15, 16) > 8)
                            {
                                SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.GreenRock);
                                RemoveBlock(x, y, (byte)Layers.Ore);
                            }
                        }
                        //Remove clumps of main and ore to create caves
                        if (PerlinNoise(x, y, 10, 10) > 5)
                        {
                            RemoveBlock(x, y, (byte)Layers.Main);
                            RemoveBlock(x, y, (byte)Layers.Ore);
                        }

                        //Add bedrock to the bottom of the world
                        if (y < PerlinNoise(x, y, 13, 16) + 3)
                        {
                            SetBlock(x, y, (byte)Layers.Main, (byte)MainLayer.BedRock);
                            RemoveBlock(x, y, (byte)Layers.Ore);
                        }

                        //Add foreground elements and trees to the top of the ground level if there is a main block there
                        if (y == groundLevel && IsBlockAt(x, y, (byte)Layers.Main))
                        {
                            //Add grass with a 25% probability 
                            SetBlock(x, y + 1, (byte)Layers.Foreground, (byte)ForegroundLayer.Grass, 80);
                            //Add flowers with a 25% probability
                            SetBlock(x, y + 1, (byte)Layers.Foreground, (byte)ForegroundLayer.Flower, 10);


                            //Places a tree with 10% probability
                            if (DoAddBlock(10))
                            {
                                bool genTree = true;
                                //Check for enough room to place a stump
                                if (IsBlockAt(x - 1, y + 1, (byte)Layers.Main))
                                    genTree = false;
                                //Check for a tree within 7 blocks away
                                for (int xTree = x - 6; xTree < x; xTree++)
                                {
                                    //Check for blocks of trees at a few different vertical positions (since tree sizes and positions vary)
                                    if (IsBlockAt(xTree, y + 2, (byte)Layers.Tree) || IsBlockAt(xTree, y + 5, (byte)Layers.Tree) || IsBlockAt(xTree, y + 8, (byte)Layers.Tree))
                                    {
                                        genTree = false;
                                        break;
                                    }
                                }
                                //Generate a tree if there are none nearby
                                if (genTree)
                                {
                                    //Set stump at a horizontal offset since it is 3 blocks wide
                                    SetBlock(x - 1, y + 1, (byte)Layers.Tree, (byte)TreeLayer.Stump);
                                    //Randomly generate a tree height between 5 and 12 blocks high
                                    int treeHeight = random.Next(2, 5);
                                    //Loop up the height of the tree
                                    for (int yTree = y + 2; yTree < y + treeHeight; yTree++)
                                    {
                                        SetBlock(x, yTree, (byte)Layers.Tree, (byte)TreeLayer.Trunk);
                                    }
                                    //Finish the tree with the crown 
                                    SetBlock(x - 3, y + treeHeight, (byte)Layers.Tree, (byte)TreeLayer.Crown);
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
                    if (y < groundLevel)
                    {
                        //Add water to caves
                        if (!IsBlockAt(x, y, world.FluidLayer))
                        {
                            byte density = (byte)FluidType.Water;
                            if (DoAddBlock(10))
                                density = (byte)FluidType.Poison;
                            else if (DoAddBlock(15))
                                density = (byte)FluidType.Lava;

                            //Generate a pool of water with 0.5% probability
                            if (DoAddBlock(0.5f))
                                GeneratePool(x, y, fluidDynamics.MaxWeight, density, y, new Vector2Int(x, y));
                        }
                    }
                    //Place a vine if there is no main block at this positon, but there is one above it at a 10% probability
                    if (DoAddBlock(10) && !IsBlockAt(x, y, (byte)Layers.Main) && IsBlockAt(x, y + 1, (byte)Layers.Main))
                    {
                        SetBlock(x, y, (byte)Layers.Foreground, (byte)ForegroundLayer.VineStart);
                        //Generate the length of the vine (between 2 to 6 blocks
                        int vineBottom = y - random.Next(2, 6);
                        //Loop down the length of the vine
                        for (int yVine = y - 1; yVine > vineBottom; yVine--)
                        {
                            //Place middle vine blocks unless it reaches a main block
                            if (!IsBlockAt(x, yVine, (byte)Layers.Main))
                                SetBlock(x, yVine, (byte)Layers.Foreground, (byte)ForegroundLayer.VineMiddle);
                            else
                                break;
                        }
                        //As long as there is no main block at the bottom of the vine, place the end vine block
                        if (!IsBlockAt(x, vineBottom, (byte)Layers.Main))
                            SetBlock(x, vineBottom, (byte)Layers.Foreground, (byte)ForegroundLayer.VineEnd);
                    }
                    //----------
                }
            }

        }
    }
}

