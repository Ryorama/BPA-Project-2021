using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Serializes terrain data for saving and loading
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Name of the folder containing the World files
        /// </summary>
        public static string SaveFolderName = "Worlds";
        /// <summary>
        /// Name of the file holding the world data
        /// </summary>
        public static string BaseDataFileName = "BaseData.json";
        /// <summary>
        /// Name of the file holding the block data
        /// </summary>
        public static string TerrainDataFileName = "TerrainData.bin";
        /// <summary>
        /// The default save location for builds
        /// </summary>
        public static string DefaultSaveLocation = Application.persistentDataPath + "/" + SaveFolderName + "/";
        /// <summary>
        /// The default save location for testing in the editor
        /// </summary>
        public static string EditorSaveLocation = Application.streamingAssetsPath + "/" + SaveFolderName + "/";
        /// <summary>
        /// Whether world data has been loaded
        /// </summary>
        public static bool BaseDataLoaded = false;

        public static string GetSaveDirectory()
        {
#if UNITY_EDITOR
            return EditorSaveLocation;
#else
            return DefaultSaveLocation;
#endif
        }

        /// <summary>
        /// Get the save location for the voxel world files
        /// </summary>
        /// <param name="worldName">Name of the world</param>
        /// <returns>Returns the save location</returns>
        public static string GetSavePath(string worldName)
        {
            if (World.WorldData.LoadWorld)
                return World.WorldData.WorldDirectory;
            else
            {
              string saveLocation = GetSaveDirectory() + worldName + "/";
                if (!Directory.Exists(saveLocation))
                  Directory.CreateDirectory(saveLocation);
              return saveLocation;
            }
        }

        /// <summary>
        /// Save the world data to file (JSON)
        /// </summary>
        public static void SaveBaseData()
        {
            string saveFilePath = GetSavePath(World.WorldData.Name) + BaseDataFileName;
            string baseDataJson = JsonUtility.ToJson(new BaseData(World.WorldData));
            File.WriteAllText(saveFilePath, baseDataJson);
            Debug.Log("World Base Data has been saved to file at: " + saveFilePath);
        }

        /// <summary>
        /// Load the base data from file
        /// </summary>
        /// <param name="worldData">Reference to the World Data to set loaded base data</param>
        /// <param name="saveFolderPath">File path of the base data folder</param>
        /// <returns>Returns true if the data was successfully loaded</returns>
        public static bool LoadBaseData(WorldData worldData, string saveFolderPath)
        {
            string saveFilePath = saveFolderPath + BaseDataFileName;

            if (File.Exists(saveFilePath))
            {
                string baseDataJson = File.ReadAllText(saveFilePath);
                BaseData baseData = JsonUtility.FromJson<BaseData>(baseDataJson);
                worldData.Name = baseData.Name;
                worldData.WorldWidth = baseData.Width;
                worldData.WorldHeight = baseData.Height;
                worldData.Seed = baseData.Seed;
                BaseDataLoaded = true;
                return true;
            } else
            {
                Debug.LogWarning("Could not find BaseData file");
            }
            return false;
        }

        /// <summary>
        /// Get the serialization ID of the save file
        /// This is the same as the GUID of the World Data Object used to serialize the world
        /// </summary>
        /// <param name="saveFolderPath">File path of the base data folder</param>
        /// <param name="SID">The output serialization ID</param>
        /// <returns>Returns the true if the data was successfully loaded</returns>
        public static bool GetSID(string saveFolderPath, out string SID)
        {
            string saveFilePath = saveFolderPath + BaseDataFileName;

            if (File.Exists(saveFilePath))
            {
                string baseDataJson = File.ReadAllText(saveFilePath);
                BaseData baseData = JsonUtility.FromJson<BaseData>(baseDataJson);
                SID = baseData.SID;
                return true;
            }
            else
            {
                SID = null;
                Debug.LogWarning("Could not find BaseData file");
            }
            return false;
        }

        /// <summary>
        /// Save the terrain data to file
        /// </summary>
        public static void SaveTerrainData()
        {
            string saveFilePath = GetSavePath(World.WorldData.Name) + TerrainDataFileName;
            FileStream fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryWriter binaryWriter = new BinaryWriter(fileStream);
            BlockData blockData = new BlockData();
            binaryWriter.Write(SerializationHelper.ConvertJaggedByteArrayToArray(blockData.BlockType));
            binaryWriter.Write(SerializationHelper.ConvertJaggedBoolArrayToByteArray(blockData.RenderBlock));
            if (World.WorldData.BasicFluid)
            {
                FluidData fluidData = new FluidData(((BasicFluidDynamics)FluidDynamics.Instance));
                float[,] fluidWeightData = fluidData.FluidWeight;
                for (int i = 0; i < fluidWeightData.GetLength(0); i++)
                {
                    for (int j = 0; j < fluidWeightData.GetLength(1); j++)
                    {
                        binaryWriter.Write(fluidWeightData[i, j]);
                    }
                }
            } else
            {
                AdvancedFluidData advancedFluidData = new AdvancedFluidData(((AdvancedFluidDynamics)FluidDynamics.Instance));
                float[,] fluidWeightData = advancedFluidData.FluidWeight;
                for (int i = 0; i < fluidWeightData.GetLength(0); i++)
                {
                    for (int j = 0; j < fluidWeightData.GetLength(1); j++)
                    {
                        binaryWriter.Write(fluidWeightData[i, j]);
                    }
                }
                byte[,] fluidDensityData = advancedFluidData.FluidDensity;
                for (int i = 0; i < fluidDensityData.GetLength(0); i++)
                {
                    for (int j = 0; j < fluidDensityData.GetLength(1); j++)
                    {
                        binaryWriter.Write(fluidDensityData[i, j]);
                    }
                }
                Color32[,] fluidColorData = advancedFluidData.FluidColor;
                for (int i = 0; i < fluidColorData.GetLength(0); i++)
                {
                    for (int j = 0; j < fluidColorData.GetLength(1); j++)
                    {
                        binaryWriter.Write(fluidColorData[i, j].r);
                        binaryWriter.Write(fluidColorData[i, j].g);
                        binaryWriter.Write(fluidColorData[i, j].b);
                        binaryWriter.Write(fluidColorData[i, j].a);
                    }
                }
            }
            binaryWriter.Close();
            Debug.Log("Terrain Data has been saved to file at: " + saveFilePath);
        }

        /// <summary>
        /// Loads the saved data to the world
        /// </summary>
        /// <param name="saveFolderPath">File path of the world data folder</param>
        /// <returns>Returns true if the data is successfully loaded</returns>
        public static bool LoadTerrainData(string saveFolderPath)
        {
             string saveFilePath = saveFolderPath + TerrainDataFileName;
            if (File.Exists(saveFilePath))
            {
                FileStream fileStream = new FileStream(saveFilePath, FileMode.Open);
                BinaryReader binaryReader = new BinaryReader(fileStream);
                int numBlockLayers = World.Instance.NumBlockLayers;
                int worldWidth = World.Instance.WorldWidth;
                int worldHeight = World.Instance.WorldHeight;
                byte[] blockTypeByteData = binaryReader.ReadBytes(numBlockLayers * worldWidth * worldHeight);
                byte[][,] blockType = SerializationHelper.ConvertByteArrayToJaggedArray(blockTypeByteData, numBlockLayers, worldWidth, worldHeight);
                byte[] renderBlockByteData = binaryReader.ReadBytes(numBlockLayers * worldWidth * worldHeight);
                bool[][,] renderBlock = SerializationHelper.ConvertByteArrayToJaggedBoolArray(renderBlockByteData, numBlockLayers, worldWidth, worldHeight);

                for (int i = 0; i < World.Instance.NumBlockLayers; i++)
                {
                    BlockLayer blockLayer = World.Instance.GetBlockLayer(i);
                    blockLayer.BlockType = blockType[i];
                    blockLayer.RenderBlock = renderBlock[i];
                }
                if (World.Instance.BasicFluid)
                {
                    BasicFluidDynamics basicFluidDynamics = ((BasicFluidDynamics) FluidDynamics.Instance);
                    for (int x = 0; x < basicFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
                    {
                        for (int y = 0; y < basicFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                        {
                            basicFluidDynamics.FluidBlocksInternal[x, y].Weight = binaryReader.ReadSingle();
                        }
                    }
                } else
                {
                    AdvancedFluidDynamics advancedFluidDynamics = ((AdvancedFluidDynamics) FluidDynamics.Instance);
                    for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
                    {
                        for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                        {
                            advancedFluidDynamics.FluidBlocksInternal[x, y].Weight = binaryReader.ReadSingle();
                        }
                    }
                    for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
                    {
                        for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                        {
                            ((AdvancedFluidBlock)advancedFluidDynamics.FluidBlocksInternal[x, y]).Density = binaryReader.ReadByte();
                        }
                    }
                    for (int x = 0; x < advancedFluidDynamics.FluidBlocksInternal.GetLength(0); x++)
                    {
                        for (int y = 0; y < advancedFluidDynamics.FluidBlocksInternal.GetLength(1); y++)
                        {
                            var fluidBlock = ((AdvancedFluidBlock) advancedFluidDynamics.FluidBlocksInternal[x, y]);
                            fluidBlock.Color.r = binaryReader.ReadByte();
                            fluidBlock.Color.g = binaryReader.ReadByte();
                            fluidBlock.Color.b = binaryReader.ReadByte();
                            fluidBlock.Color.a = binaryReader.ReadByte();
                        }
                    }
                }
                binaryReader.Close();
                return true;
            } else
            {
                Debug.LogError("Could not find TerrainData file");
            }
            return false;
        }

        /// <summary>
        /// Save all game data
        /// </summary>
        public static void Save()
        {
            SaveBaseData();
            SaveTerrainData();
        }

        /// <summary>
        /// Check to see if the directory contains BaseData and TerrainData files
        /// </summary>
        /// <param name="pathToDirectory">The path to the directory</param>
        /// <returns>Return true if it is a valid directory</returns>
        public static bool IsValidWorldDirectory(string pathToDirectory)
        {
            bool containsWorldData = false, containsBlockData = false;

            DirectoryInfo saveDirectory = new DirectoryInfo(pathToDirectory);
            FileInfo[] directoryFiles = saveDirectory.GetFiles();
            for(int i = 0; i < directoryFiles.Length; i++)
            {
                if (directoryFiles[i].Name == BaseDataFileName)
                    containsWorldData = true;
                else if (directoryFiles[i].Name == TerrainDataFileName)
                    containsBlockData = true;
            }
            return containsWorldData && containsBlockData;
        }

        /// <summary>
        /// Check if a directory of a certain name exists
        /// </summary>
        /// <param name="worldName">The name of the directory</param>
        /// <returns>Returns true if that directory exists</returns>
        public static bool DoesWorldDirectoryExist(string worldName)
        {
            return Directory.Exists(GetSaveDirectory() + worldName);
        }

        /// <summary>
        /// Check if a directory exists
        /// </summary>
        /// <param name="path">The path to the directory</param>
        /// <returns>Returns true if that directory exists</returns>
        public static bool DoesDirectoryExist(string path)
        {
            if (path == null || path == "")
                return false;
            return Directory.Exists(path);
        }
    }

}