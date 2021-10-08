using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Renders the fluid simulation in a texture
    /// </summary>
    public class FluidRenderer : TexturedMesh
    {
        private static FluidRenderer instance;
        /// <summary>
        /// A singleton instance of the Fluid Renderer
        /// </summary>
        public static FluidRenderer Instance
        {
            get { return instance; }
        }

        private World world;
        private ChunkLoader chunkLoader;

        //Reference to the fluid blocks
        private FluidBlock[,] fluidBlocks;
        //The secondary fluid color
        private Color32 secondaryColor;
        //The primary fluid color
        private Color32 mainColor;
        //Clear color
        private Color32 clearColor = Color.clear;

        private void OnEnable()
        {
            ChunkLoader.OnChunksLoaded += UpdateTexture;
        }

        private void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= UpdateTexture;
        }

        protected override void Awake()
        {
            base.Awake();

            if (instance == null)
                instance = this;
            else if (instance != this)
            {
                Debug.Log("Destroying extra instance of " + gameObject.name);
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            world = World.Instance;
            chunkLoader = ChunkLoader.Instance;
            
            fluidBlocks = FluidDynamics.Instance.FluidBlocksInternal;
            if (world.BasicFluid)
            {
                secondaryColor = ((BasicFluidDynamics)FluidDynamics.Instance).SecondaryColor;
                mainColor = ((BasicFluidDynamics)FluidDynamics.Instance).MainColor;
            }
            
            base.Initialize(chunkLoader.LoadedWorldWidth, chunkLoader.LoadedWorldHeight);

            //Set the z position/render order for the fluid texture
            transform.position = new Vector3(transform.position.x, transform.position.y, world.GetBlockLayer(world.FluidLayer).ZLayerOrder + world.ZBlockDistance / 4f);

            UpdateTexture();
        }

        protected override void LateUpdate()
        {
            if (update && !world.FluidDisabled)
            {
                transform.position = new Vector3(chunkLoader.OriginLoadedChunks.x, chunkLoader.OriginLoadedChunks.y, transform.position.z);
                base.LateUpdate();
            }
        }

        protected override Color32[] GetPixelData()
        {
            //Current position of the fluid texture
            int posX = (int)transform.position.x;
            int posY = (int)transform.position.y;

            int index = 0;
            
            //Grab the a section of the fluid data from the current position of the loaded world
            for (int y = posY; y < posY + height; y++)
            {
                for (int x = posX; x < posX + width; x++)
                {
                    FluidBlock fluidBlock = fluidBlocks[x, y];
                    //If the fluid block contains liquid
                    if (fluidBlock.Weight > 0)
                    {
                        if (world.BasicFluid)
                            //Sets the color of the fluid block to an interpolated value between the main and secondary color based on the amount of fluid in that block
                            tempPixelData[index] = Color32.Lerp(secondaryColor, mainColor, fluidBlock.Weight / 4f);
                        else
                            //Sets the color of the fluid block
                            tempPixelData[index] = ((AdvancedFluidBlock)fluidBlock).Color;
                        
                        //Set the alpha based on the amount of fluid in the block
                        byte alpha = (byte)Mathf.Min(255, fluidBlock.Weight * 255);
                        byte maxAlpha = 255;
                        if (world.BasicFluid)
                            maxAlpha = mainColor.a;
                        else
                            maxAlpha = ((AdvancedFluidBlock)fluidBlock).Color.a;
                            
                        //Ensure the alpha can not be more than that of the main color
                        if (alpha < maxAlpha)
                            tempPixelData[index].a = alpha;
                    }
                    else
                        tempPixelData[index] = clearColor;
                    index++;
                }
            }
            return tempPixelData;
        }
    }
}