using System.Collections.Generic;
using System.Text;
using Game;
using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Controls modification of the World.Instance
    /// </summary>
    /// 
    public static class WorldModifier
    {

        public static List<OverwoldTerrainGenerator.MainLayer> stoneMaterials = new List<OverwoldTerrainGenerator.MainLayer>();
        public static List<OverwoldTerrainGenerator.MainLayer> groundMaterials = new List<OverwoldTerrainGenerator.MainLayer>();

        /// <summary>
        /// Places fluid at a specific location in the world
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="amount">Amount of fluid to place</param>
        /// <param name="radius">How many surrounding blocks get fluid</param>
        /// <param name="density">The density of the fluid (or type) - used by the Advanced Fluid Dynamics</param>
        public static void PlaceFluid(int x, int y, float amount, int radius = 0, byte density = 0, Color32 color = new Color32())
        {
            //Place fluid as long as it isn't disabled
            if (!World.Instance.FluidDisabled)
            {
                //Loop through all surrounding blocks according to the radius
                for (int posX = x - radius; posX <= x + radius; posX++)
                {
                    for (int posY = y - radius; posY <= y + radius; posY++)
                    {
                        if (World.Instance.BasicFluid)
                        {
                            //Add fluid to the block if it is in bounds and isn't a solid block
                            if (InBounds(x, y) && !BasicFluidDynamics.Instance.GetFluidBlock(posX, posY).IsSolid())
                            {
                                FluidDynamics.Instance.GetFluidBlock(posX, posY).AddWeight(amount);
                                FluidDynamics.Instance.UpdateFluid(posX, posY);
                            }
                        } else
                        {
                            //Add fluid to the block if it is in bounds and isn't a solid block
                            AdvancedFluidBlock fluidBlock = (AdvancedFluidBlock)FluidDynamics.Instance.GetFluidBlock(posX, posY);
                            if (InBounds(posX, posY) && !fluidBlock.IsSolid() && (fluidBlock.Weight == 0 || fluidBlock.Density == density))
                            {
                                fluidBlock.AddWeight(density, amount, color);
                                FluidDynamics.Instance.UpdateFluid(posX, posY);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes fluid from a specific location
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="radius">How many surround blocks to remove fluid</param>
        public static void RemoveFluid(int x, int y, int radius = 0)
        {
            if (!World.Instance.FluidDisabled)
            {
                for (int posX = x - radius; posX <= x + radius; posX++)
                {
                    for (int posY = y - radius; posY <= y + radius; posY++)
                    {
                        //Remove fluid from a block as long as it is in bounds and isn't solid
                        if (InBounds(x, y) && !FluidDynamics.Instance.GetFluidBlock(posX, posY).IsSolid())
                        {
                            FluidDynamics.Instance.GetFluidBlock(posX, posY).SetEmpty();
                            FluidDynamics.Instance.UpdateFluid(posX, posY);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the block at a specific location of a specific layer
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="replace">Whether to replace the current block</param>
        /// <param name="layer">Layer to modify</param>
        /// <param name="blockType">Block type to place</param>
        /// <param name="radius">Radius making up the area in which blocks will be placed</param>
        public static void SetBlock(int x, int y, bool replace, byte layer, byte blockType, int radius = 0)
        {
            for (int posX = x - radius; posX <= x + radius; posX++)
            {
                for (int posY = y - radius; posY <= y + radius; posY++)
                {
                    //Determines if there is a block at the current position
                    bool isBlock = World.Instance.GetBlockLayer(layer).IsBlockAt(posX, posY);
                    //Set the block at the current position if it is replacing the current block or if there is no block there
                    if (!isBlock || isBlock && replace)
                        World.Instance.AddBlock(posX, posY, blockType, layer);
                }
            }
        }

        /// <summary>
        /// Removes blocks from a specified location in specified layers
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="layers">Layers to remove blocks from</param>
        /// <param name="radius">How many surrounding blocks to remove</param>
        public static void RemoveBlock(int x, int y, List<byte> layers, int radius = 0)
        {
            for (int posX = x - radius; posX <= x + radius; posX++)
            {
                for (int posY = y - radius; posY <= y + radius; posY++)
                {
                    //Loops through all the layers
                    foreach (byte layer in layers)
                    {
                        for (int gm = 0; gm < groundMaterials.Count; gm++) {
                            if (ItemWheel.itemSlots[ItemWheel.selectedSlot].itemType == ItemType.Shovel && World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Main).GetBlockType(posX, posX) == (byte)groundMaterials[gm])
                            {
                                if (World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Background).IsBlockAt(posX, posY + 1))
                                {
                                    if (World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Background).GetBlockType(posX, posY + 1) == (byte)OverwoldTerrainGenerator.BackgroundLayer.Stump)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Removes block from layer if there is one
                                    if (World.Instance.GetBlockLayer(layer).IsBlockAt(posX, posY))
                                    {
                                        World.Instance.RemoveBlock(posX, posY, layer);
                                    }
                                }
                            }
                        }

                        for (int sm = 0; sm < stoneMaterials.Count; sm++)
                        {
                            if (ItemWheel.itemSlots[ItemWheel.selectedSlot].itemType == ItemType.Pickaxe && World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Main).GetBlockType(posX, posX) == (byte)stoneMaterials[sm])
                            {
                                if (World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Background).IsBlockAt(posX, posY + 1))
                                {
                                    if (World.Instance.GetBlockLayer((byte)OverwoldTerrainGenerator.WorldLayers.Background).GetBlockType(posX, posY + 1) == (byte)OverwoldTerrainGenerator.BackgroundLayer.Stump)
                                    {
                                        break;
                                    }
                                }
                                else
                                {
                                    //Removes block from layer if there is one
                                    if (World.Instance.GetBlockLayer(layer).IsBlockAt(posX, posY))
                                    {
                                        World.Instance.RemoveBlock(posX, posY, layer);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Check if a specific coordinate is within World.Instance bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns true if that position is within bounds</returns>
        static bool InBounds(int x, int y)
        {
            if (x < 0 || x >= World.Instance.WorldWidth || y < 0 || y >= World.Instance.WorldHeight)
                return false;
            return true;
        }
    }
}