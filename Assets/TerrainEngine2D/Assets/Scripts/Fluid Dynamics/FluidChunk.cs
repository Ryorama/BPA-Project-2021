using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    /// <summary>
    /// A chunk of fluid blocks for rendering
    /// </summary>
    public class FluidChunk : MonoBehaviour
    {
        private World world;
        [SerializeField]
        private Chunk chunk;
        private FluidDynamics fluidDynamics;
        //Reference to the fluid block array
        private FluidBlock[,] fluidBlocks;

        //Holds mesh information for rendering the chunk
        private BlockGridMesh blockGridMesh;
        //The secondary color for fluid 
        private Color32 secondaryColor;
        //The primary color for fluid
        private Color32 mainColor;

        private bool update;
        /// <summary>
        /// Used to update the mesh when fluid blocks change
        /// </summary>
        public bool Update
        {
            set { update = value; }
        }

        private void Awake()
        {
            gameObject.layer = LayerMask.NameToLayer("Terrain");
        }

        void Start()
        {
            world = World.Instance;
            fluidDynamics = FluidDynamics.Instance;
            
            fluidBlocks = fluidDynamics.FluidBlocksInternal;
            if (world.BasicFluid)
            {
                //Get the fluid colors
                mainColor = ((BasicFluidDynamics)fluidDynamics).MainColor;
                secondaryColor = ((BasicFluidDynamics)fluidDynamics).SecondaryColor;
            } 
            //Initialize the block grid mesh
            blockGridMesh = new BlockGridMesh(GetComponent<MeshFilter>().mesh, chunk.ChunkSize, world.ZBlockDistance, true, 1, true);
            BuildChunk();
        }

        void LateUpdate()
        {
            //Rebuild the fluid chunk if it needs to be updated
            if (update)
            {
                BuildChunk();
                update = false;
            }
        }

        /// <summary>
        /// Build the chunk mesh
        /// </summary>
        public void BuildChunk()
        {
            //Loop through the grid of chunks
            for (int x = 0; x < chunk.ChunkSize; x++)
            {
                for (int y = 0; y < chunk.ChunkSize; y++)
                {
                    //Get the current fluid block
                    FluidBlock fluidBlock = fluidBlocks[x + chunk.ChunkX, y + chunk.ChunkY];
                    float minWeight = fluidDynamics.MinWeight;
                    //Create a fluid block if its weight is above the minimum threshold
                    if (fluidBlock.Weight > minWeight)
                    {
                        //Calculate the z-order for the fluid (renders just behind the fluid layer)
                        float zOrder = world.GetBlockLayer(world.FluidLayer).ZLayerOrder + world.ZBlockDistance / 4f;
                        Color32 color;
                        if (world.BasicFluid)
                        //Calculate the color of the mesh based on the fluid weight (higher weight means darker color)
                            color = Color32.Lerp(secondaryColor, mainColor, fluidBlock.Weight / 4f);
                        else
                            color = ((AdvancedFluidBlock)fluidBlock).Color;
                        bool topDown = fluidDynamics.TopDown;
                        float height = !topDown ? fluidBlock.GetHeight() : 1;
                        //Add the fluid block to the mesh
                        blockGridMesh.CreateBlock(x, y, zOrder, new Vector2(0, 0), 0, 1, 1, 1, height, color);
                    }
                }
            }
        
            //Update the mesh
            blockGridMesh.UpdateMesh();
        }
    }
}