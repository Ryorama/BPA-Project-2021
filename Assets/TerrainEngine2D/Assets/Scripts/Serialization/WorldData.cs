using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// All data used to generate the world
    /// </summary>
    public class WorldData : ScriptableObject
    {
        #region Serialization
        public string GUID;
        #endregion

        #region Editor
        public bool SavePlayModeChanges;
        #endregion

        #region Objects
        [Header("Objects")]
        public bool ToggleOSD = true;
        public float OSDUpdateRate = 0.05f;
        public float OSDUIScale = 1.0f;
        public bool ToggleCursor = true;
        #endregion

        #region Terrain
        [Header("Terrain")]
        public bool SaveWorld;
        public bool LoadWorld;
        public string Name = "Unassigned";
        public int WorldWidth = 1024;
        public int WorldHeight = 128;
        public int Seed = 12345678;
        public string WorldDirectory;
        #endregion

        #region Chunks
        [Header("Chunks")]
        public int ChunkSize = 16;
        public float ChunkLoadRate = 0.1f;
        public int ChunkLoadDistance = 64;
        #endregion

        #region Modification
        [Header("Modification")]
        public bool EnableInputHandler = true;
        public int MaxModifyRadius = 3;
        #endregion

        #region Fluid
        [Header("Fluid")]
        public bool FluidDisabled;
        public byte FluidLayer;
        public bool RenderFluidAsTexture;
        public bool BasicFluid;
        //Simulation
        public bool TopDown;
        public bool RunFluidSimulation = true;
        public float FluidUpdateRate;
        //Physics Properties
        public float MaxFluidWeight = 1.0f;
        public float MinFluidWeight = 0.005f;
        public float StableFluidAmount = 0.0001f;
        public float FluidPressureWeight = 0.2f;
        //Modification
        public float FluidDropAmount = 0.2f;
        //Basic Fluid
        public Color32 MainFluidColor = new Color(0.176f, 0.431f, 0.557f, 0.8f);
        public Color32 SecondaryFluidColor = new Color(0.275f, 0.686f, 0.894f, 0.8f);
        //Advanced Fluid
        public FluidType[] FluidTypes;
        public bool AllowSurfaceFilling = true;
        public float FluidMixingFactor = 0.1f;
        #endregion

        #region Lighting
        [Header("Lighting")]
        public bool LightingDisabled;
        public bool BasicLighting;
        //Basic Lighting
        public int LightBleedAmount = 3;
        //Block Lighting
        public byte LightLayer;
        public int IntensityFactor = 18;
        public int TransmissionFactor = 5;
        //Ambient Lighting
        public bool AmbientLightDisabled;
        public byte AmbientLightLayer;
        public int AmbientIntensityFactor = 18;
        public int AmbientTransmissionFactor = 5;
        public bool UseHeightMap;
        //Day Cycle
        public bool PauseTime = true;
        public int TimeFactor = 100;
        public Color32 DaylightColor = new Color32(255, 255, 255, 255);
        public Color32 NightColor = new Color32(65, 65, 65, 255);
        public float SunriseTime = 7;
        public float SunsetTime = 19;
        //Post Processing
        public int DownScale = 2;
        public int NumberBlurPasses = 3;
        #endregion

        #region Falling Blocks
        [Header("Falling Blocks")]
        public bool FallingBlocksDisabled;
        public byte FallingBlockLayer;
        public float FallingBlocksUpdateRate = 0.05f;
        #endregion

        #region Optimization
        [Header("Optimization")]
        public bool OverlapBlendSquares;
        public bool CullHiddenBlocks = true;
        #endregion

        #region Blocks
        [Header("Blocks")]
        public int PixelsPerBlock = 8;
        public float ZBlockDistance = 1f;
        public int ZLayerFactor = 1;
        public BlockLayer[] BlockLayers;
        #endregion


        public WorldData DeepCopy()
        {
            WorldData worldDataCopy = CreateInstance<WorldData>();
            worldDataCopy.GUID = GUID;
            worldDataCopy.ToggleOSD = ToggleOSD;
            worldDataCopy.OSDUpdateRate = OSDUpdateRate;
            worldDataCopy.OSDUIScale = OSDUIScale;
            worldDataCopy.ToggleCursor = ToggleCursor;
            worldDataCopy.SaveWorld = SaveWorld;
            worldDataCopy.LoadWorld = LoadWorld;
            worldDataCopy.Name = Name;
            worldDataCopy.WorldWidth = WorldWidth;
            worldDataCopy.WorldHeight = WorldHeight;
            worldDataCopy.Seed = Seed;
            worldDataCopy.WorldDirectory = WorldDirectory;
            worldDataCopy.ChunkSize = ChunkSize;
            worldDataCopy.ChunkLoadRate = ChunkLoadRate;
            worldDataCopy.ChunkLoadDistance = ChunkLoadDistance;
            worldDataCopy.EnableInputHandler = EnableInputHandler;
            worldDataCopy.MaxModifyRadius = MaxModifyRadius;
            worldDataCopy.FluidDisabled = FluidDisabled;
            worldDataCopy.FluidLayer = FluidLayer;
            worldDataCopy.RenderFluidAsTexture = RenderFluidAsTexture;
            worldDataCopy.BasicFluid = BasicFluid;
            worldDataCopy.TopDown = TopDown;
            worldDataCopy.RunFluidSimulation = RunFluidSimulation;
            worldDataCopy.FluidUpdateRate = FluidUpdateRate;
            worldDataCopy.MaxFluidWeight = MaxFluidWeight;
            worldDataCopy.MinFluidWeight = MinFluidWeight;
            worldDataCopy.StableFluidAmount = StableFluidAmount;
            worldDataCopy.FluidPressureWeight = FluidPressureWeight;
            worldDataCopy.FluidDropAmount = FluidDropAmount;
            worldDataCopy.MainFluidColor = MainFluidColor;
            worldDataCopy.SecondaryFluidColor = SecondaryFluidColor;
            worldDataCopy.LightingDisabled = LightingDisabled;
            worldDataCopy.BasicLighting = BasicLighting;
            worldDataCopy.LightBleedAmount = LightBleedAmount;
            worldDataCopy.LightLayer = LightLayer;
            worldDataCopy.IntensityFactor = IntensityFactor;
            worldDataCopy.TransmissionFactor = TransmissionFactor;
            worldDataCopy.AmbientLightDisabled = AmbientLightDisabled;
            worldDataCopy.AmbientLightLayer = AmbientLightLayer;
            worldDataCopy.AmbientIntensityFactor = AmbientIntensityFactor;
            worldDataCopy.AmbientTransmissionFactor = AmbientTransmissionFactor;
            worldDataCopy.UseHeightMap = UseHeightMap;
            worldDataCopy.PauseTime = PauseTime;
            worldDataCopy.TimeFactor = TimeFactor;
            worldDataCopy.DaylightColor = DaylightColor;
            worldDataCopy.NightColor = NightColor;
            worldDataCopy.SunriseTime = SunriseTime;
            worldDataCopy.SunsetTime = SunsetTime;
            worldDataCopy.DownScale = DownScale;
            worldDataCopy.NumberBlurPasses = NumberBlurPasses;
            worldDataCopy.FallingBlocksDisabled = FallingBlocksDisabled;
            worldDataCopy.FallingBlockLayer = FallingBlockLayer;
            worldDataCopy.FallingBlocksUpdateRate = FallingBlocksUpdateRate;
            worldDataCopy.OverlapBlendSquares = OverlapBlendSquares;
            worldDataCopy.CullHiddenBlocks = CullHiddenBlocks;
            worldDataCopy.PixelsPerBlock = PixelsPerBlock;
            worldDataCopy.ZBlockDistance = ZBlockDistance;
            worldDataCopy.ZLayerFactor = ZLayerFactor;
            worldDataCopy.BlockLayers = BlockLayers;
            worldDataCopy.FluidTypes = FluidTypes;
            worldDataCopy.AllowSurfaceFilling = AllowSurfaceFilling;
            worldDataCopy.FluidMixingFactor = FluidMixingFactor;

            return worldDataCopy;
        }

        public void CopyData(WorldData worldDataCopy)
        {
            GUID = worldDataCopy.GUID;
            ToggleOSD = worldDataCopy.ToggleOSD;
            OSDUpdateRate = worldDataCopy.OSDUpdateRate;
            OSDUIScale = worldDataCopy.OSDUIScale;
            ToggleCursor = worldDataCopy.ToggleCursor;
            SaveWorld = worldDataCopy.SaveWorld;
            LoadWorld = worldDataCopy.LoadWorld;
            Name = worldDataCopy.Name;
            WorldWidth = worldDataCopy.WorldWidth;
            WorldHeight = worldDataCopy.WorldHeight;
            Seed = worldDataCopy.Seed;
            WorldDirectory = worldDataCopy.WorldDirectory;
            ChunkSize = worldDataCopy.ChunkSize;
            ChunkLoadRate = worldDataCopy.ChunkLoadRate;
            ChunkLoadDistance = worldDataCopy.ChunkLoadDistance;
            EnableInputHandler = worldDataCopy.EnableInputHandler;
            MaxModifyRadius = worldDataCopy.MaxModifyRadius;
            FluidDisabled = worldDataCopy.FluidDisabled;
            FluidLayer = worldDataCopy.FluidLayer;
            RenderFluidAsTexture = worldDataCopy.RenderFluidAsTexture;
            BasicFluid = worldDataCopy.BasicFluid;
            TopDown = worldDataCopy.TopDown;
            RunFluidSimulation = worldDataCopy.RunFluidSimulation;
            FluidUpdateRate = worldDataCopy.FluidUpdateRate;
            MaxFluidWeight = worldDataCopy.MaxFluidWeight;
            MinFluidWeight = worldDataCopy.MinFluidWeight;
            StableFluidAmount = worldDataCopy.StableFluidAmount;
            FluidPressureWeight = worldDataCopy.FluidPressureWeight;
            FluidDropAmount = worldDataCopy.FluidDropAmount;
            MainFluidColor = worldDataCopy.MainFluidColor;
            SecondaryFluidColor = worldDataCopy.SecondaryFluidColor;
            LightingDisabled = worldDataCopy.LightingDisabled;
            BasicLighting = worldDataCopy.BasicLighting;
            LightBleedAmount = worldDataCopy.LightBleedAmount;
            LightLayer = worldDataCopy.LightLayer;
            IntensityFactor = worldDataCopy.IntensityFactor;
            TransmissionFactor = worldDataCopy.TransmissionFactor;
            AmbientLightDisabled = worldDataCopy.AmbientLightDisabled;
            AmbientLightLayer = worldDataCopy.AmbientLightLayer;
            AmbientIntensityFactor = worldDataCopy.AmbientIntensityFactor;
            AmbientTransmissionFactor = worldDataCopy.AmbientTransmissionFactor;
            UseHeightMap = worldDataCopy.UseHeightMap;
            PauseTime = worldDataCopy.PauseTime;
            TimeFactor = worldDataCopy.TimeFactor;
            DaylightColor = worldDataCopy.DaylightColor;
            NightColor = worldDataCopy.NightColor;
            SunriseTime = worldDataCopy.SunriseTime;
            SunsetTime = worldDataCopy.SunsetTime;
            DownScale = worldDataCopy.DownScale;
            NumberBlurPasses = worldDataCopy.NumberBlurPasses;
            FallingBlocksDisabled = worldDataCopy.FallingBlocksDisabled;
            FallingBlockLayer = worldDataCopy.FallingBlockLayer;
            FallingBlocksUpdateRate = worldDataCopy.FallingBlocksUpdateRate;
            OverlapBlendSquares = worldDataCopy.OverlapBlendSquares;
            CullHiddenBlocks = worldDataCopy.CullHiddenBlocks;
            PixelsPerBlock = worldDataCopy.PixelsPerBlock;
            ZBlockDistance = worldDataCopy.ZBlockDistance;
            ZLayerFactor = worldDataCopy.ZLayerFactor;
            BlockLayers = worldDataCopy.BlockLayers;
            FluidTypes = worldDataCopy.FluidTypes;
            AllowSurfaceFilling = worldDataCopy.AllowSurfaceFilling;
            FluidMixingFactor = worldDataCopy.FluidMixingFactor;
        }
    }
}
