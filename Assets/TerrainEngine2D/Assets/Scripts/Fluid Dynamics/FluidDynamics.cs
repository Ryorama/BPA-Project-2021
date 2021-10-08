using UnityEngine;

// Copyright (C) 2020 Matthew Wilson
//
// This is part of a derivative work and portions of this file are licensed under the MIT
//     open source license (https://opensource.org/licenses/MIT)
// Please refer to the Third-Party Notices.txt for more information
//
// References (Refer to Third-Party Notices.txt for all Third Party Licenses):
// https://w-shadow.com/blog/2009/09/01/simple-fluid-simulation/
// http://www.jgallant.com/2d-liquid-simulator-with-cellular-automaton-in-unity/

namespace TerrainEngine2D
{
    /// <summary>
    /// Fluid physics system using cellular automata for both top-down and side scrolling type games
    /// </summary>
    public abstract class FluidDynamics : MonoBehaviourSingleton<FluidDynamics> 
    {
        protected FluidBlock[,] fluidBlocksInternal;
        /// <summary>
        /// *readonly* Reference to the fluid blocks
        /// Do not try to write to this array or a runtime exception will be thrown 
        /// </summary>
        public FluidBlock[,] FluidBlocksInternal
        {
            get { return fluidBlocksInternal; }
        }
        protected bool topDown;
        /// <summary>
        /// Whether the game has a top-down camera style for controlling fluid flow
        /// </summary>
        public bool TopDown
        {
            get { return topDown; }
        }
        protected float maxWeight = 1.0f;
        /// <summary>
        /// Maximum amount of liquid a fluid block can hold
        /// </summary>
        public float MaxWeight
        {
            get { return maxWeight; }
        }
        protected float minWeight = 0.005f;
        /// <summary>
        /// Minimum amount of liquid a fluid block can hold
        /// </summary>
        public float MinWeight
        {
            get { return minWeight; }
        }
        protected float stableAmount = 0.0001f;
        /// <summary>
        /// The minimum amount of fluid flow allowed before the block becomes stable
        /// </summary>
        public float StableAmount
        {
            get { return stableAmount; }
        }
        protected float pressureWeight = 0.2f;
        /// <summary>
        /// Fluid weight pressure factor (each fluid block can hold pressureWeight more liquid than the block above it)
        /// </summary>
        public float PressureWeight
        {
            get { return pressureWeight; } 
        }
        protected float fluidDropAmount = 0.2f;
        /// <summary>
        /// Amount of fluid added on drop
        /// </summary>
        public float FluidDropAmount
        {
            get { return fluidDropAmount; }
        }
        
        protected ChunkLoader chunkLoader;
        protected FluidRenderer fluidRenderer;
        protected WorldData worldData;
        
        protected float fluidUpdateRate;
        protected float[,] fluidDifference;
        protected float updateTimer;
        
        protected bool updateFluid;
        protected bool renderFluidTexture;
        protected bool runSimulation;
        
        protected virtual void OnEnable()
        {
            //Update the fluid simulation when chunks are loaded
            ChunkLoader.OnChunksLoaded += UpdateFluid;
        }

        protected virtual void OnDisable()
        {
            ChunkLoader.OnChunksLoaded -= UpdateFluid;
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
            
            //Set Properties
            UpdateProperties(World.WorldData);
        }
        
        protected virtual void Start()
        {
            fluidRenderer = FluidRenderer.Instance;
            chunkLoader = ChunkLoader.Instance;
            renderFluidTexture = worldData.RenderFluidAsTexture;
        }

        /// <summary>
        /// Initialize the fluid data
        /// </summary>
        /// <param name="world">Reference to the world</param>
        public void Initialize()
        {
            SetInstance();
            
            worldData = World.WorldData;
            //Allocates memory for the fluid blocks
            AllocFluidBlocks();
            
            fluidDifference = new float[worldData.WorldWidth, worldData.WorldHeight];
        }

        /// <summary>
        /// Sets the fluid properties from the world data file
        /// </summary>
        /// <param name="worldData">The data file containing the fluid properties</param>
        public virtual void UpdateProperties(WorldData worldData)
        {
            topDown = worldData.TopDown;
            runSimulation = worldData.RunFluidSimulation;
            fluidUpdateRate = worldData.FluidUpdateRate;
            maxWeight = worldData.MaxFluidWeight;
            minWeight = worldData.MinFluidWeight;
            stableAmount = worldData.StableFluidAmount;
            pressureWeight = worldData.FluidPressureWeight;
            fluidDropAmount = worldData.FluidDropAmount;
        }

        /// <summary>
        /// Sets the fluid for updating
        /// </summary>
        public void UpdateFluid()
        {
            updateFluid = true;
        }
        
        /// <summary>
        /// Sets the fluid for updating
        /// </summary>
        /// <param name="x">X-coordinate where the fluid was updated</param>>
        /// <param name="y">Y-coordinate where the fluid was updated</param>>
        public void UpdateFluid(int x, int y)
        {
            updateFluid = true;
            if(renderFluidTexture)
                fluidRenderer.UpdateTexture();
            else
                chunkLoader.UpdateChunk(x, y, true);
        }

        /// <summary>
        /// Get the fluid block at a specific coordinate
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Returns the fluid block</returns>
        public abstract FluidBlock GetFluidBlock(int x, int y);

        /// <summary>
        /// Set the instance of the fluid dynamics object
        /// </summary>
        protected abstract void SetInstance();

        /// <summary>
        /// Allocate memory for fluid blocks
        /// </summary>
        protected abstract void AllocFluidBlocks();

        protected virtual void Update()
        {
            //Checks if the simulation is running
            if (runSimulation) {
                //Checks if it is time to update the fluid
                if (updateTimer >= fluidUpdateRate)
                {
                    //Updates the fluid if it needs updating
                    if (updateFluid)
                    {
                        SimulateFluid(chunkLoader.OriginLoadedChunks, chunkLoader.EndPointLoadedChunks + new Vector2Int(chunkLoader.ChunkSize, chunkLoader.ChunkSize));
                        updateTimer = 0;
                    }
                } else
                    updateTimer += Time.deltaTime;
            }
        }

        /// <summary>
        /// Updates the fluid blocks and runs the fluid simulation
        /// </summary>
        /// <param name="startPosition">The starting position of the loaded world</param>
        /// <param name="endPosition">The end position of the loaded world</param>
        protected abstract void SimulateFluid(Vector2Int startPosition, Vector2Int endPosition);

        /// <summary>
        /// Calculates the movement of fluid in a fluid block for a top-down simulation
        /// </summary>
        /// <param name="x">X coordinate of the fluid block</param>
        /// <param name="y">Y coordinate of the fluid block</param>
        /// <param name="fluidBlock">Reference to the fluid block</param>
        protected abstract void TopDownFlow(int x, int y, FluidBlock fluidBlock);

        /// <summary>
        /// Calculates the movement of fluid in a fluid block for a down (side scrolling) simulation
        /// </summary>
        /// <param name="x">X coordinate of the fluid block</param>
        /// <param name="y">Y coordinate of the fluid block</param>
        /// <param name="fluidBlock">Reference to the fluid block</param>
        protected abstract void DownFlow(int x, int y, FluidBlock fluidBlock);
    }
}