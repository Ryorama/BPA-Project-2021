using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using TerrainEngine2D.Lighting;

// Copyright (C) 2018 Matthew K Wilson

namespace TerrainEngine2D.Editor
{
    /// <summary>
    /// Custom inspector for the World
    /// </summary>
	[CustomEditor (typeof(World))]
    [CanEditMultipleObjects]
	public class WorldCustomInspector : UnityEditor.Editor
	{
        private World world;
        private WorldData worldData;
        private FluidRenderer fluidRenderer;
        private OSDController osdController;
        private FallingBlockSimulation fallingBlockSimulation;
        private FluidDynamics fluidDynamics;
        private LightSystem lightSystem;
        private AdvancedLightSystem advancedLightSystem;
        private LightRenderer lightRenderer;
        private AmbientLight ambientLight;
        private ChunkLoader chunkLoader;
        private GameObject fluidGO;
        private GameObject lightCameraGO;
        private GameObject overlayCameraGO;
        private WorldInputHandler worldInputHandler;
        //Reorderable list for setting up the block layers
        private ReorderableList layerList;
        //Reorderable lists for setting up the blocks of each layer
		private List<ReorderableList> blockLists;
        //Position of the vertical scroll bar for block setup
		private Vector2 scrollPos;
        //The current selected tab
		private int selectedTab;
        //The current selected layer
		private int selectedLayer;
        //Toggle the layer list
		private bool layerListToggle = true;
        //If the osd controller is in the scene
        private bool usingOSD;

        private SerializedObject worldDataSO;
        private SerializedObject chunkloaderSO;

        private ReorderableList fluidTypesList;

        //Foldouts
        private bool terrainFoldout = true;
        private bool chunksFoldout = true;
        private bool modificationFoldout = true;
        private bool fluidFoldout = true;
        private bool lightingFoldout = true;
        private bool fallingBlocksFoldout = true;
        private bool optimizationFoldout = true;

        private void OnEnable ()
		{
            world = (World)target;
            osdController = FindObjectOfType<OSDController>();
            usingOSD = osdController != null;

            //Check if the world contains the necessary layers
            int lightingLayer = LayerMask.NameToLayer("Lighting");
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int ignoreLightingLayer = LayerMask.NameToLayer("Ignore Lighting");
            if (lightingLayer == -1 || terrainLayer == -1 || ignoreLightingLayer == -1)
                EditorUtility.DisplayDialog("Warning", "The project does not contain the necessary layers for Terrain Engine 2D to work, please add these three layers to the project: Terrain, Lighting, Ignore Lighting.", "Ok");

            fluidRenderer = FindObjectOfType<FluidRenderer>();
            fluidGO = fluidRenderer.gameObject;
            if (fluidRenderer == null)
                Debug.LogError("Missing the Fluid Renderer component from the World GameObject.");

            worldInputHandler = FindObjectOfType<WorldInputHandler>();
            if(worldInputHandler == null)
                Debug.LogError("Missing the World Input Handler component from the World GameObject.");

            chunkLoader = world.GetComponentInChildren<ChunkLoader>();
            world.ChunkLoader = chunkLoader;
            if(chunkLoader == null)
                Debug.LogError("Missing the ChunkLoader component from the World GameObject.");
            chunkloaderSO = new SerializedObject(chunkLoader);

            fallingBlockSimulation = world.GetComponentInChildren<FallingBlockSimulation>(true);
            world.FallingBlockSimulation = fallingBlockSimulation;
            if(fallingBlockSimulation == null) Debug.LogError("Missing the FallingBlockSimulation component from the World GameObject.");

            lightSystem = world.GetComponentInChildren<LightSystem>(true);
            world.LightSystem = lightSystem;
            if(lightSystem == null)
                Debug.LogError("Missing the LightSystem component from the World GameObject.");
            
            advancedLightSystem = world.GetComponentInChildren<AdvancedLightSystem>(true);
            world.AdvancedLightSystem = advancedLightSystem;
            if(advancedLightSystem == null)
                Debug.LogError("Missing the AdvancedLightSystem component from the World GameObject.");

            ambientLight = world.GetComponentInChildren<AmbientLight>(true);
            world.AmbientLight = ambientLight;
            if (ambientLight == null)
                Debug.LogError("Missing the AmbientLight component from the World GameObject.");

            lightRenderer = Camera.main.GetComponent<LightRenderer>();
            world.LightRenderer = lightRenderer;
            if (lightRenderer == null)
                Debug.LogError("Missing the LightRenderer component from the WorldCamera GameObject. Please replace your current WorldCamera GameObject with the updated WorldCamera Prefab. ");

            lightCameraGO = Camera.main.transform.GetChild(0).gameObject;
            if(lightCameraGO == null)
                Debug.LogError("Missing the LightCamera GameObject. Please replace your current WorldCamera GameObject with the updated WorldCamera Prefab. ");

            overlayCameraGO = Camera.main.transform.GetChild(1).gameObject;
            if(overlayCameraGO == null)
                Debug.LogError("Missing the OverlayCamera GameObject. Please replace your current WorldCamera GameObject with the updated WorldCamera Prefab. ");

            if (worldDataSO != null)
            {
                InitializeLists();
            }
		}

        void InitializeLists()
        {
            layerList = new ReorderableList(worldDataSO, worldDataSO.FindProperty("BlockLayers"), true, true, true, true);
            blockLists = new List<ReorderableList>();
            for (int i = 0; i < layerList.count; i++)
            {
                AddNewBlockList(layerList.serializedProperty.GetArrayElementAtIndex(i), i);
            }

            layerList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Layers");
            };

            layerList.elementHeightCallback = (index) => {
                float height;
                height = EditorGUIUtility.singleLineHeight * 3 + 10;
                return height;
            };

            layerList.onAddCallback = (ReorderableList list) => {
                int index = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = index;
                SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("name").stringValue = "Layer " + index;
                element.FindPropertyRelative("colliderLayer").boolValue = false;
                element.FindPropertyRelative("material").objectReferenceValue = null;
                element.FindPropertyRelative("blockInfo").arraySize = 0;
                AddNewBlockList(element, index);
            };

            layerList.onRemoveCallback = (ReorderableList list) => {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                if (blockLists.Count > 0)
                {
                    blockLists.Remove(blockLists[blockLists.Count - 1]);
                }
            };

            layerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = layerList.serializedProperty.GetArrayElementAtIndex(index);

                rect.y += 2;
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("name"), new GUIContent("Name:"));
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("colliderLayer"), new GUIContent("Enable Colliders:"));
                EditorGUI.PropertyField(new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * 2, rect.width, EditorGUIUtility.singleLineHeight), element.FindPropertyRelative("material"), new GUIContent("Tileset Material:"));
            };

            fluidTypesList = new ReorderableList(worldDataSO, worldDataSO.FindProperty("FluidTypes"), true, true, true, true);
            fluidTypesList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Fluid Types");
            };

            fluidTypesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                SerializedProperty element = fluidTypesList.serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, new GUIContent("Density: " + index));
            };

            fluidTypesList.onRemoveCallback = (ReorderableList list) => {

                list.serializedProperty.GetArrayElementAtIndex(list.index).objectReferenceValue = null;
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                if (list.index >= list.serializedProperty.arraySize - 1)
                    list.index = list.serializedProperty.arraySize - 1;
            };

        }

		void AddNewBlockList (SerializedProperty element, int index)
		{
			ReorderableList blockList = new ReorderableList (worldDataSO, element.FindPropertyRelative ("blockInfo"), true, true, true, true);

			blockList.drawHeaderCallback = (Rect rectBlock) => {
				EditorGUI.LabelField (rectBlock, element.FindPropertyRelative ("name").stringValue + " Blocks");
			};

			blockList.elementHeightCallback = (indexBlock) => {
                float height = 0;
                if (index == worldData.FallingBlockLayer)
                    height = EditorGUIUtility.singleLineHeight * 6 + 10;
                else
                    height = EditorGUIUtility.singleLineHeight * 5 + 10;
                return height;
			};

			blockList.onAddCallback = (ReorderableList l) => {
				int indexBlock = l.serializedProperty.arraySize;
				l.serializedProperty.arraySize++;
				l.index = indexBlock;
				SerializedProperty elementBlock = l.serializedProperty.GetArrayElementAtIndex (indexBlock);
				elementBlock.FindPropertyRelative ("name").stringValue = "Block " + indexBlock;
                elementBlock.FindPropertyRelative("texturePositionX").floatValue = 0;
                elementBlock.FindPropertyRelative("texturePositionY").floatValue = 0;
                elementBlock.FindPropertyRelative ("textureWidth").intValue = 1;
				elementBlock.FindPropertyRelative ("textureHeight").intValue = 1;
				elementBlock.FindPropertyRelative ("overlapBlock").boolValue = false;
                elementBlock.FindPropertyRelative("transparent").boolValue = true;
                elementBlock.FindPropertyRelative ("numVariations").intValue = 1;
                elementBlock.FindPropertyRelative("fallingBlock").boolValue = false;
            };

			blockList.drawElementCallback = (Rect rectBlock, int indexBlock, bool isActiveBlock, bool isFocusedBlock) => {
				SerializedProperty elementBlock = blockList.serializedProperty.GetArrayElementAtIndex (indexBlock);
				rectBlock.y += 2;

                bool overlapBlock = elementBlock.FindPropertyRelative("overlapBlock").boolValue;
                bool fallingBlock = elementBlock.FindPropertyRelative("fallingBlock").boolValue;

                if (overlapBlock)
                {
                    elementBlock.FindPropertyRelative("textureWidth").intValue = 2;
                    elementBlock.FindPropertyRelative("textureHeight").intValue = 3;
                } else if (fallingBlock)
                {
                    elementBlock.FindPropertyRelative("textureWidth").intValue = 1;
                    elementBlock.FindPropertyRelative("textureHeight").intValue = 1;
                }
                
                EditorGUI.PropertyField (new Rect (rectBlock.x, rectBlock.y, rectBlock.width, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative ("name"), new GUIContent ("Name:"));
				EditorGUI.PropertyField (new Rect (rectBlock.x, rectBlock.y + EditorGUIUtility.singleLineHeight, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative ("overlapBlock"), new GUIContent ("Overlap Block:"));
                EditorGUI.PropertyField (new Rect (rectBlock.x + rectBlock.width / 2, rectBlock.y + EditorGUIUtility.singleLineHeight, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative("transparent"), new GUIContent("Transparent:"));
                EditorGUI.PropertyField (new Rect (rectBlock.x, rectBlock.y + EditorGUIUtility.singleLineHeight * 2, rectBlock.width, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative ("numVariations"), new GUIContent ("Variations:"));
				EditorGUI.PropertyField (new Rect (rectBlock.x, rectBlock.y + EditorGUIUtility.singleLineHeight * 3, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative ("textureWidth"), new GUIContent ("Texture Width:"));
				EditorGUI.PropertyField (new Rect (rectBlock.x + rectBlock.width / 2, rectBlock.y + EditorGUIUtility.singleLineHeight * 3, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative ("textureHeight"), new GUIContent ("Texture Height:"));
                EditorGUI.PropertyField (new Rect (rectBlock.x, rectBlock.y + EditorGUIUtility.singleLineHeight * 4, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative("texturePositionX"), new GUIContent("Texture X Position:"));
                EditorGUI.PropertyField (new Rect (rectBlock.x + rectBlock.width / 2, rectBlock.y + EditorGUIUtility.singleLineHeight * 4, rectBlock.width / 2, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative("texturePositionY"), new GUIContent("Texture Y Position:"));
                if (!worldData.FallingBlocksDisabled && index == worldData.FallingBlockLayer)
                    EditorGUI.PropertyField(new Rect(rectBlock.x, rectBlock.y + EditorGUIUtility.singleLineHeight * 5, rectBlock.width, EditorGUIUtility.singleLineHeight), elementBlock.FindPropertyRelative("fallingBlock"), new GUIContent("Falling Block:"));
            };
			blockLists.Add (blockList);
		}

		public override void OnInspectorGUI ()
		{
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            WorldData prevWorldData = serializedObject.FindProperty("worldData").objectReferenceValue as WorldData;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldData"), new GUIContent("World Data Object: ", "A World Data object which contains all of the data used in generating the world."));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Generate New World Data", "Generate a new World Data Object")))
            {
                WorldData tempWorldData = CreateInstance<WorldData>();
                serializedObject.FindProperty("worldData").objectReferenceValue = tempWorldData;
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/WorldData.asset");
                AssetDatabase.CreateAsset(serializedObject.FindProperty("worldData").objectReferenceValue, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                tempWorldData.GUID = AssetDatabase.AssetPathToGUID(path);
                Debug.Log("Generated a WorldData object at " + path);
            }
            serializedObject.ApplyModifiedProperties();
            World.WorldData = serializedObject.FindProperty("worldData").objectReferenceValue as WorldData;
            if (World.WorldData == null)
            {
                GUILayout.EndHorizontal();
                return;
            }

            if (GUILayout.Button(new GUIContent("Retrieve Old Data", "Attempt to retrieve old serialized World data")))
            {
                RetrieveOldData();
            }
            GUILayout.EndHorizontal();

            worldData = World.WorldData;
            worldDataSO = new SerializedObject(worldData);

            worldData.SavePlayModeChanges = EditorGUILayout.ToggleLeft(new GUIContent("Save Changes in Play Mode", "Whether to save any changes made to the World Data in the inspector while in Play Mode"), worldData.SavePlayModeChanges);
            EditorGUI.EndDisabledGroup();

            if (layerList == null || prevWorldData != worldData)
                InitializeLists();

            selectedTab = GUILayout.Toolbar (selectedTab, new GUIContent[2] {
				new GUIContent ("Main Properties"),
				new GUIContent ("Block Setup", "Add, remove and modify the different layers and blocks of the world")
			});
            switch (selectedTab)
            {
                case 0:
                    GUIStyle foldoutStyle = EditorStyles.foldout;
                    foldoutStyle.fontStyle = FontStyle.Bold;

                    terrainFoldout = EditorGUILayout.Foldout(terrainFoldout, "Terrain", true, foldoutStyle);
                    if (terrainFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainGenerator"), new GUIContent("Terrain Generator Script: ", "The custom script used for procedurally generating the world. Add this script as a component to the World GameObject."));
                        if (serializedObject.FindProperty("terrainGenerator").objectReferenceValue == null)
                        {
                            TerrainGenerator terrainGenerator = world.GetComponent<TerrainGenerator>();
                            if (terrainGenerator != null)
                                serializedObject.FindProperty("terrainGenerator").objectReferenceValue = terrainGenerator;
                        }
                        EditorGUILayout.BeginHorizontal();
                        worldData.SaveWorld = EditorGUILayout.Toggle(new GUIContent("Auto Save: ", "Automatically save the generated terrain to file on exit"), worldData.SaveWorld);
                        EditorGUILayout.PropertyField(worldDataSO.FindProperty("LoadWorld"), new GUIContent("Load World: ", "Load a generated world from file"));
                        EditorGUILayout.EndHorizontal();

                        if (worldData.LoadWorld)
                        {
                            string worldDirectoryPath = null;

                            if (GUILayout.Button(new GUIContent("Select World Directory", "Select the World directory for loading saved data")))
                            {
                                worldDirectoryPath = EditorUtility.OpenFolderPanel("Select the World Directory", Serialization.GetSaveDirectory(), "");
                            }
                            if (!string.IsNullOrEmpty(worldDirectoryPath))
                                worldData.WorldDirectory = worldDirectoryPath + "/";
                            if (!string.IsNullOrEmpty(worldData.WorldDirectory))
                            {
                                string SID;
                                GUIStyle errorStyle = EditorStyles.centeredGreyMiniLabel;
                                errorStyle.normal.textColor = Color.red;
                                if (Serialization.GetSID(worldData.WorldDirectory, out SID))
                                {
                                    if (SID == worldData.GUID)
                                    {
                                        bool loaded = Serialization.LoadBaseData(worldData, worldData.WorldDirectory);

                                        if (!loaded)
                                        {
                                            EditorGUILayout.LabelField("Error loading file", errorStyle);
                                        }
                                        else
                                        {
                                            EditorGUILayout.LabelField("Name: " + worldData.Name);
                                            EditorGUILayout.LabelField("Width: " + worldData.WorldWidth);
                                            EditorGUILayout.LabelField("Height: " + worldData.WorldHeight);
                                            EditorGUILayout.LabelField("Seed: " + worldData.Seed);
                                        }
                                    }
                                    else
                                        EditorGUILayout.LabelField("This world save does not match the World Data object", errorStyle);
                                } else
                                    EditorGUILayout.LabelField("Error loading file", errorStyle);
                            }
                            else
                                EditorGUILayout.LabelField("No map loaded", EditorStyles.centeredGreyMiniLabel);
                        }
                        else
                        {
                            if (worldDataSO.FindProperty("SaveWorld").boolValue)
                                EditorGUILayout.HelpBox("Saving a world will overwrite any other saved world of the same name.", MessageType.Warning);
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("Name"), new GUIContent("Name: ", "The name of the World"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("WorldWidth"), new GUIContent("Width: ", "The total width of the world (in block units)"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("WorldHeight"), new GUIContent("Height: ", "The total height of the world (in block units)"));
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("Seed"), new GUIContent("Seed: ", "A integer value used to procedurally generate the world"));
                            if (GUILayout.Button(new GUIContent("Random Seed", "Randomly generate a seed"), GUILayout.MaxWidth(100)))
                                worldData.Seed = Random.Range(int.MinValue / 100, int.MaxValue / 100); //The int type represents signed 32-bit integers with values between  –2,147,483,648 and 2,147,483,647.
                            EditorGUILayout.EndHorizontal();

                            bool generateWorld = GUILayout.Button(new GUIContent("Generate World", "Generates the world to file using the specified parameters and TerrainGenerator script"));
                            if (generateWorld)
                                GenerateWorld();
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
                    }

                    chunksFoldout = EditorGUILayout.Foldout(chunksFoldout, "Chunks", true, foldoutStyle);
                    if (chunksFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.ChunkSize = EditorGUILayout.IntField(new GUIContent("Chunk Size: ", "The side length of a chunk (in block units). Must be an even number"), worldData.ChunkSize);
                        //Ensure chunkSize is even
                        if (worldData.ChunkSize % 2 != 0)
                            worldData.ChunkSize -= 1;
                        //Round width to the nearest chunkSize
                        worldDataSO.FindProperty("WorldWidth").intValue = Mathf.RoundToInt(worldDataSO.FindProperty("WorldWidth").intValue / (float)worldData.ChunkSize) * worldData.ChunkSize;
                        //Round height to the nearest chunkSize
                        worldDataSO.FindProperty("WorldHeight").intValue = Mathf.RoundToInt(worldDataSO.FindProperty("WorldHeight").intValue / (float)worldData.ChunkSize) * worldData.ChunkSize;
                        EditorGUILayout.PropertyField(chunkloaderSO.FindProperty("loadTransform"), new GUIContent("Load Transform: ", "The transform of the GameObject where chunks will be loaded"));
                        EditorGUILayout.PropertyField(worldDataSO.FindProperty("ChunkLoadRate"), new GUIContent("Load Rate: ", "The rate at which chunks are checked and loaded into the scene"));
                        EditorGUILayout.PropertyField(worldDataSO.FindProperty("ChunkLoadDistance"), new GUIContent("Load Distance: ", "The horizontal distance from the object which chunks will load in"));
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
                    }

                    modificationFoldout = EditorGUILayout.Foldout(modificationFoldout, "Modification", true, foldoutStyle);
                    if (modificationFoldout)
                    {
                        EditorGUI.indentLevel++;
                        if (usingOSD)
                        {
                            EditorGUILayout.BeginHorizontal();
                            worldData.ToggleOSD = EditorGUILayout.Toggle(new GUIContent("Toggle OSD: ", "Enable or disable the OSD"), worldData.ToggleOSD);
                            osdController.enabled = worldData.ToggleOSD;
                            foreach (Transform child in osdController.transform)
                                child.gameObject.SetActive(worldData.ToggleOSD);
                            if (worldData.ToggleOSD)
                                worldData.OSDUpdateRate = EditorGUILayout.FloatField(new GUIContent("OSD Update Rate: ", "The rate at which the OSD updates its values (in seconds)"), worldData.OSDUpdateRate);
                            EditorGUILayout.EndHorizontal();
                            if (worldData.ToggleOSD)
                            {
                                float prevScale = worldData.OSDUIScale;
                                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                                worldData.OSDUIScale = EditorGUILayout.Slider(new GUIContent("OSD Scale: ", "The scale of the UI of the OSD"), worldData.OSDUIScale, 0.25f, 2f);
                                EditorGUI.EndDisabledGroup();
                                if (worldData.OSDUIScale != prevScale)
                                    osdController.SetScale(worldData.OSDUIScale);
                            }
                        }
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.EnableInputHandler = EditorGUILayout.Toggle(new GUIContent("Enable Input Handler: ", "Enable or disable the WorldInputHandler"), worldData.EnableInputHandler);
                        worldInputHandler.enabled = worldData.EnableInputHandler;
                        EditorGUILayout.PropertyField(worldDataSO.FindProperty("MaxModifyRadius"), new GUIContent("Max Modify Radius: ", "The max size of the Modify Radius"));
                        worldData.ToggleCursor = EditorGUILayout.Toggle(new GUIContent("Toggle Cursor: ", "Show or hide the cursor in game"), worldData.ToggleCursor);
                        Cursor.visible = worldData.ToggleCursor;
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
                    }

                    int[] layerOptions = new int[layerList.count];
                    GUIContent[] layerDisplayOptions = new GUIContent[layerList.count];
                    for (int i = 0; i < layerList.count; i++)
                    {
                        layerOptions[i] = i;
                        SerializedProperty element = layerList.serializedProperty.GetArrayElementAtIndex(i);
                        layerDisplayOptions[i] = new GUIContent(element.FindPropertyRelative("name").stringValue);
                    }

                    fluidFoldout = EditorGUILayout.Foldout(fluidFoldout, "Fluid", true, foldoutStyle);
                    if (fluidFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.FluidDisabled = EditorGUILayout.Toggle(new GUIContent("Disable fluid: ", "Disables the fluid simulation, fluid rendering, and prevents placement of fluid"), worldData.FluidDisabled);
                        if (worldData.FluidDisabled)
                        {
                            var basicFluidDynamics = fluidGO.GetComponent<BasicFluidDynamics>();
                            if (basicFluidDynamics)
                                DestroyImmediate(basicFluidDynamics);
                                
                            var advancedFluidDynamics = fluidGO.GetComponent<AdvancedFluidDynamics>();
                            if (advancedFluidDynamics)
                                DestroyImmediate(advancedFluidDynamics);

                            if (fluidRenderer != null)
                            {
                                if (fluidRenderer.enabled)
                                    fluidRenderer.enabled = false;
                                if (IsFluidChunkEnabled())
                                    SetFluidChunk(false);
                            }
                            else
                                throw new MissingComponentException("Can not find the Fluid Renderer. Please add the FluidRenderer script to the World GameObject or enable it in the inspector.");
                        }
                        else
                        {
                            EditorGUILayout.IntPopup(worldDataSO.FindProperty("FluidLayer"), layerDisplayOptions, layerOptions, new GUIContent("Select Fluid Layer: ", "Choose the block layer to use for the fluid simulation"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("RenderFluidAsTexture"), new GUIContent("Render Fluid as Texture: ", "Whether to render the fluid as a single texture as opposed to chunks of meshes"));
                            worldDataSO.ApplyModifiedProperties();
                            if (fluidRenderer != null)
                            {
                                bool renderFluidTexture = worldDataSO.FindProperty("RenderFluidAsTexture").boolValue;
                                if (fluidRenderer.enabled != renderFluidTexture)
                                    fluidRenderer.enabled = renderFluidTexture;
                                if (renderFluidTexture == IsFluidChunkEnabled())
                                    SetFluidChunk(!renderFluidTexture);
                            }
                            else
                                throw new MissingComponentException("Can not find the Fluid Renderer. Please add the FluidRenderer script to the World GameObject or enable it in the inspector.");


                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("BasicFluid"), new GUIContent("Basic Fluid:", "Whether to use the basic lighting system (faster) or the advanced lighting system (multi-fluid type support)"));
                            worldDataSO.ApplyModifiedProperties();
                            EditorGUI.EndDisabledGroup();
                            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("TopDown"), new GUIContent("Top Down: ", "Enable this if your game has a top-down camera style and fluid will flow equally in all directions"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("RunFluidSimulation"), new GUIContent("Run Simulation: ", "Whether to run the fluid simulation"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("FluidUpdateRate"), new GUIContent("Update Rate: ", "The rate at which the fluid simulation will update (in seconds)"));
                            EditorGUI.indentLevel--;
                            EditorGUILayout.LabelField("Physics Properties", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("MaxFluidWeight"), new GUIContent("Max Weight: ", "The maximum amount of liquid a fluid block can hold"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("MinFluidWeight"), new GUIContent("Min Weight: ", "The minimum amount of liquid a fluid block can hold"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("StableFluidAmount"), new GUIContent("Stable Amount: ", "If the amount of fluid flowing out of a block is less than the stable amount, the fluid block is considered stable and will stop updating"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("FluidPressureWeight"), new GUIContent("Pressure Weight: ", "Fluid weight pressure factor (each fluid block can hold pressureWeight more liquid than the block above it)"));
                            EditorGUI.indentLevel--;
                            EditorGUILayout.LabelField("Modification", EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("FluidDropAmount"), new GUIContent("Fluid Drop Amount: ", "The amount of fluid added per frame when placing fluid"));
                            EditorGUI.indentLevel--;
                            if (worldData.BasicFluid)
                            {
                                fluidDynamics = fluidGO.GetComponent<BasicFluidDynamics>();
                                if (!fluidDynamics)
                                    fluidDynamics = fluidGO.AddComponent<BasicFluidDynamics>();
                                
                                var advancedFluidDynamics = fluidGO.GetComponent<AdvancedFluidDynamics>();
                                if (advancedFluidDynamics)
                                    DestroyImmediate(advancedFluidDynamics);

                                EditorGUILayout.LabelField("Basic Fluid Properties", EditorStyles.boldLabel);
                                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("MainFluidColor"), new GUIContent("Main Color: ", "The primary color of the fluid"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("SecondaryFluidColor"), new GUIContent("Secondary Color: ", "The secondary color of the fluid (used to differentiate high vs low pressure/fluid weight). Block with lesser fluid weight will have a color closer to the secondary color."));
                                EditorGUI.indentLevel--;
                                EditorGUI.EndDisabledGroup();
                            } else
                            {
                                fluidDynamics = fluidGO.GetComponent<AdvancedFluidDynamics>();
                                if (!fluidDynamics)
                                    fluidDynamics = fluidGO.AddComponent<AdvancedFluidDynamics>();
                                
                                var basicFluidDynamics = fluidGO.GetComponent<BasicFluidDynamics>();
                                if (basicFluidDynamics)
                                    DestroyImmediate(basicFluidDynamics);

                                EditorGUILayout.LabelField("Advanced Fluid Properties", EditorStyles.boldLabel);
                                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("AllowSurfaceFilling"), new GUIContent("Surface Filling (Experimental): ", "Allow fluids of different densities to mix in order to fill in the top surface layer of fluid (the added fluid is converted to the base)"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("FluidMixingFactor"), new GUIContent("Fluid Mixing Factor: ", "The factor effecting how two fluids mix together, a higher factor results in a more drastic color change"));
                                EditorGUI.indentLevel++;
                                fluidTypesList.DoLayoutList();
                                fluidTypesList.serializedProperty.serializedObject.ApplyModifiedProperties();
                                if (worldData.FluidTypes != null)
                                {
                                    for (int i = 0; i < worldData.FluidTypes.Length; i++)
                                    {
                                        if (worldData.FluidTypes[i] != null)
                                            worldData.FluidTypes[i].Density = (byte)(i);
                                        else
                                            Debug.LogWarning("A FluidType is missing its reference");
                                    }
                                }

                                EditorGUI.indentLevel--;
                                EditorGUI.EndDisabledGroup();
                            }
                        }
                        EditorGUI.indentLevel--;
                    }

                    lightingFoldout = EditorGUILayout.Foldout(lightingFoldout, "Lighting", true, foldoutStyle);
                    if (lightingFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.LightingDisabled = EditorGUILayout.Toggle(new GUIContent("Disable lighting: ", "Disables the light system and prevents light updating"), worldData.LightingDisabled);
                        worldDataSO.ApplyModifiedProperties();
                        if (worldData.LightingDisabled)
                            world.SetLighting(false);
                        else
                        {
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("BasicLighting"), new GUIContent("Basic lighting: ", "Select this if you wish to go back to the basic light system"));
                            worldDataSO.ApplyModifiedProperties();
                            world.SetLighting(true);
                            if (worldData.BasicLighting)
                            {
                                EditorGUILayout.IntPopup(worldDataSO.FindProperty("LightLayer"), layerDisplayOptions, layerOptions, new GUIContent("Light Layer: ", "Choose the block layer for lighting"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("LightBleedAmount"), new GUIContent("Amount of light bleed: ", "Amount of blocks light will bleed into from the edge of the terrain"));
                            }
                            else
                            {
                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.LabelField("Block Lighting", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                                EditorGUILayout.IntPopup(worldDataSO.FindProperty("LightLayer"), layerDisplayOptions, layerOptions, new GUIContent("Light Layer: ", "Choose the block layer for lighting"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("IntensityFactor"), new GUIContent("Intensity Factor: ", "The global intensity of the light, used to determine how far light can propagate from a light source"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("TransmissionFactor"), new GUIContent("Transmission Factor: ", "A higher value means light can transmit through more blocks"));
                                EditorGUI.EndDisabledGroup();
                                EditorGUI.indentLevel--;
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.LabelField("Ambient Light", EditorStyles.boldLabel);
                                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("AmbientLightDisabled"), new GUIContent("Disable: ", "Disable the ambient lighting"));

                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.EndHorizontal();
                                EditorGUI.BeginDisabledGroup(Application.isPlaying || worldData.AmbientLightDisabled);
                                EditorGUI.indentLevel++;
                                EditorGUILayout.IntPopup(worldDataSO.FindProperty("AmbientLightLayer"), layerDisplayOptions, layerOptions, new GUIContent("Ambient Light Layer: ", "Choose the block layer for ambient lighting"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("AmbientIntensityFactor"), new GUIContent("Intensity Factor: ", "The global intensity of the light, used to determine how far light can propagate from a light source"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("AmbientTransmissionFactor"), new GUIContent("Transmission Factor: ", "A higher value means light can transmit through more blocks"));
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("UseHeightMap"), new GUIContent("Use Height Map: ", "Whether to use the heightmap to calulate an ambient light value"));
                                EditorGUI.EndDisabledGroup();
                                EditorGUI.indentLevel--;
                                EditorGUILayout.LabelField("Day Cycle", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("PauseTime"), new GUIContent("Pause Time: ", "Pause the day time cycle"));
                                world.PauseTime = worldDataSO.FindProperty("PauseTime").boolValue;
                                worldDataSO.ApplyModifiedProperties();
                                EditorGUI.BeginDisabledGroup(worldData.PauseTime);
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("TimeFactor"), new GUIContent("Time Factor: ", "The factor used to determine how fast time will go by in the game (a time factor of 1 is realtime)"));
                                serializedObject.FindProperty("timeFactor").intValue = worldDataSO.FindProperty("TimeFactor").intValue;
                                EditorGUILayout.Slider(serializedObject.FindProperty("timeOfDay"), 0, 24, new GUIContent("Time of Day: ", "The time of day, used to control the ambient light color"));
                                EditorGUI.EndDisabledGroup();
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("DaylightColor"), new GUIContent("Day Color: ", "The color of the Camera background during the day (alpha value is used to set the amount of light during the day)"));
                                ambientLight.DaylightColor = worldDataSO.FindProperty("DaylightColor").colorValue;
                                EditorGUILayout.PropertyField(worldDataSO.FindProperty("NightColor"), new GUIContent("Night Color: ", "The color of the Camera background during the night (alpha value is used to set the amount of light at night)"));
                                ambientLight.NightColor = worldDataSO.FindProperty("NightColor").colorValue;
                                worldData.SunriseTime = EditorGUILayout.FloatField(new GUIContent("Time of Sunrise: ", "The time of the sunrise, used when setting the color of the ambient lighting (default 7)"), worldData.SunriseTime);
                                worldData.SunsetTime = EditorGUILayout.FloatField(new GUIContent("Time of Sunset: ", "The time of the sunset, used when setting the color of the ambient lighting (default 19). Must be " +
                                    "set at least one hour after the Sunrise Time"), worldData.SunsetTime);
                                worldData.SunriseTime = Mathf.Clamp(worldData.SunriseTime, 0, 24);
                                worldData.SunsetTime = Mathf.Clamp(worldData.SunsetTime, 0, 24);
                                EditorGUILayout.MinMaxSlider(new GUIContent("Sunrise and Sunset: ", "The time of the sunrise and sunset, used when setting the color of the ambient lighting"), ref worldData.SunriseTime, ref worldData.SunsetTime, 0, 24);
                                if (worldData.SunsetTime < worldData.SunriseTime + 1)
                                    worldData.SunsetTime = worldData.SunriseTime + 1;
                                ambientLight.SunriseTime = worldData.SunriseTime;
                                ambientLight.SunsetTime = worldData.SunsetTime;
                                EditorGUI.indentLevel--;
                                EditorGUILayout.LabelField("Post Processing", EditorStyles.boldLabel);
                                EditorGUI.indentLevel++;
                                EditorGUILayout.IntSlider(worldDataSO.FindProperty("DownScale"), 0, 8, new GUIContent("Down Scale: ", "The amount to scale down the lighting texture (in powers of 2)"));
                                lightRenderer.DownScale = worldDataSO.FindProperty("DownScale").intValue;
                                EditorGUILayout.IntSlider(worldDataSO.FindProperty("NumberBlurPasses"), 0, 10, new GUIContent("Number Blur Passes: ", "The number of times the lighting texture will be blurred"));
                                lightRenderer.NumberBlurPasses = worldDataSO.FindProperty("NumberBlurPasses").intValue;
                                EditorGUI.indentLevel--;
                            }
                        }
                        EditorGUI.indentLevel--;
                        EditorGUI.EndDisabledGroup();
                    }

                    fallingBlocksFoldout = EditorGUILayout.Foldout(fallingBlocksFoldout, "Falling Blocks", true, foldoutStyle);
                    if (fallingBlocksFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.FallingBlocksDisabled = EditorGUILayout.Toggle(new GUIContent("Disable falling blocks: ", "Disables the falling block simulation"), worldData.FallingBlocksDisabled);
                        worldDataSO.ApplyModifiedProperties();
                        if (worldData.FallingBlocksDisabled)
                        {
                            fallingBlockSimulation.enabled = false;
                        }
                        else
                        {
                            fallingBlockSimulation.enabled = true;
                            EditorGUILayout.IntPopup(worldDataSO.FindProperty("FallingBlockLayer"), layerDisplayOptions, layerOptions, new GUIContent("Falling Block Layer: ", "Select the layer which will contain blocks that fall due to gravity"));
                            EditorGUILayout.PropertyField(worldDataSO.FindProperty("FallingBlocksUpdateRate"), new GUIContent("Update Rate: ", "The rate at which the blocks will fall"));
                        }
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
                    }

                    optimizationFoldout = EditorGUILayout.Foldout(optimizationFoldout, "Optimization", true, foldoutStyle);
                    if (optimizationFoldout)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUI.BeginDisabledGroup(Application.isPlaying);
                        worldData.OverlapBlendSquares = EditorGUILayout.Toggle(new GUIContent("Overlap Blend Squares: ", "Speed up mesh generation by overlapping the blend squares over the edges of Overlap Blocks instead of replacing them"), worldData.OverlapBlendSquares);
                        EditorGUILayout.PropertyField(worldDataSO.FindProperty("CullHiddenBlocks"), new GUIContent("Occlusion Culling: ", "Don't generate the blocks if they are hidden behind other blocks/layers"));
                        EditorGUI.EndDisabledGroup();
                        EditorGUI.indentLevel--;
                    }
                    break;

			case 1:
				    GUILayout.Label ("Blocks", EditorStyles.boldLabel);
				    EditorGUI.BeginDisabledGroup (Application.isPlaying);
                    worldData.PixelsPerBlock = EditorGUILayout.IntField(new GUIContent("Pixels Per Block: ", "Set the number of pixels in a single tile texture"), worldData.PixelsPerBlock);
                    EditorGUILayout.PropertyField(worldDataSO.FindProperty("ZBlockDistance"), new GUIContent("Z Block Distance: ", "The z distance between blocks (for render order)"));
                    EditorGUILayout.PropertyField(worldDataSO.FindProperty("ZLayerFactor"), new GUIContent("Z Layer Factor: ", "The z distance factor between layers (for render order)"));
                    worldDataSO.ApplyModifiedProperties();
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(400));
                    worldDataSO.Update();
                    layerListToggle = EditorGUILayout.BeginToggleGroup(new GUIContent("Layers", "Hide the layers list"), layerListToggle);
                    if (layerListToggle)
                        EditorGUILayout.BeginFadeGroup(1);
                    else
                        EditorGUILayout.BeginFadeGroup(0.0001f);
                    layerList.DoLayoutList();
                    layerList.serializedProperty.serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndFadeGroup();
                    EditorGUILayout.EndToggleGroup();

                    int[] blockLayerOptions = new int[layerList.count];
                    GUIContent[] blockLayerDisplayOptions = new GUIContent[layerList.count];

                    for (int i = 0; i < layerList.count; i++)
                    {
                        blockLayerOptions[i] = i;
                        SerializedProperty element = layerList.serializedProperty.GetArrayElementAtIndex(i);
                        blockLayerDisplayOptions[i] = new GUIContent(element.FindPropertyRelative("name").stringValue);
                    }
                    selectedLayer = (byte)EditorGUILayout.IntPopup(new GUIContent("Select block layer: ", "Choose which layer to modify block info"), selectedLayer, blockLayerDisplayOptions, blockLayerOptions);
                    if (blockLists.Count > 0 && selectedLayer < blockLists.Count)
                    {
                        blockLists[selectedLayer].DoLayoutList();
                        blockLists[selectedLayer].serializedProperty.serializedObject.ApplyModifiedProperties();
                    }

                    EditorGUILayout.EndScrollView();
                    EditorGUI.EndDisabledGroup();
                    break;
			    }

            worldDataSO.ApplyModifiedProperties();
            chunkloaderSO.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            
            fluidDynamics.UpdateProperties(worldData);

            EditorUtility.SetDirty(worldData);
        }

        void GenerateWorld()
        {
            if (world.TerrainGenerator == null)
                throw new MissingReferenceException("Missing TerrainGenerator script!");
            EditorUtility.DisplayProgressBar("World Generation", "Generating Terrain", 0);
            //Initialize the necessary parameters for generating the terrain
            world.Initialize();
            fluidDynamics.Initialize();
            world.TerrainGenerator.Initialize(world, worldData, fluidDynamics);
            
            EditorUtility.DisplayProgressBar("World Generation", "Generating Terrain", 0.25f);
            //Generate the terrain data
            world.GenerateTerrain();
            EditorUtility.DisplayProgressBar("World Generation", "Generating Terrain", 0.75f);
            //Save the terrain data
            Serialization.Save();
            
            EditorUtility.DisplayProgressBar("World Generation", "Generating Terrain", 1);
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("World Generator", "World has been generated.", "Return");
            Debug.Log("World " + worldData.Name + " has been generated and saved at: " + Serialization.GetSaveDirectory());
        }

        void RetrieveOldData()
        {
            if (worldData == null)
                return;

            if (osdController != null)
            {
                worldData.ToggleOSD = osdController.gameObject.activeInHierarchy;
                worldData.OSDUpdateRate = osdController.UpdateRate;
            }
            if (chunkLoader != null)
            {
                worldData.ChunkSize = chunkLoader.ChunkSize;
                worldData.ChunkLoadRate = chunkLoader.LoadRate;
                worldData.ChunkLoadDistance = chunkLoader.HorizontalChunkLoadDist;
            }
            if (worldInputHandler != null)
            {
                worldData.EnableInputHandler = worldInputHandler.enabled;
            }
            if(lightSystem != null)
                worldData.LightBleedAmount = lightSystem.LightBleed;
            if (advancedLightSystem != null)
            {
                worldData.IntensityFactor = advancedLightSystem.BlockLighting.IntensityFactor;
                worldData.TransmissionFactor = advancedLightSystem.BlockLighting.TransmissionFactor;
            }
            if(lightRenderer != null)
            {
                worldData.DownScale = lightRenderer.DownScale;
                worldData.NumberBlurPasses = lightRenderer.NumberBlurPasses;
            }
            if (ambientLight != null)
            {
                worldData.AmbientIntensityFactor = ambientLight.IntensityFactor;
                worldData.AmbientTransmissionFactor = ambientLight.TransmissionFactor;
                worldData.UseHeightMap = ambientLight.UseHeightMap;
                worldData.DaylightColor = ambientLight.DaylightColor;
                worldData.NightColor = ambientLight.NightColor;
                worldData.SunriseTime = ambientLight.SunriseTime;
                worldData.SunsetTime = ambientLight.SunsetTime;
            }
            if (fallingBlockSimulation != null)
                worldData.FallingBlocksUpdateRate = fallingBlockSimulation.UpdateRate;

            worldData.ToggleCursor = Cursor.visible;
            worldData.SaveWorld = world.SaveWorld;
            worldData.LoadWorld = world.LoadWorld;
            worldData.Name = world.Name;
            worldData.WorldWidth = world.WorldWidth;
            worldData.WorldHeight = world.WorldHeight;
            worldData.Seed = world.Seed;
            worldData.WorldDirectory = world.WorldDirectory;
            worldData.FluidDisabled = world.FluidDisabled;
            worldData.FluidLayer = world.FluidLayer;
            worldData.RenderFluidAsTexture = world.RenderFluidAsTexture;
            worldData.LightingDisabled = world.LightingDisabled;
            worldData.BasicLighting = world.BasicLighting;
            worldData.LightLayer = world.LightLayer;
            worldData.AmbientLightLayer = world.AmbientLightLayer;
            worldData.PauseTime = world.PauseTime;
            worldData.TimeFactor = world.TimeFactor;
            worldData.FallingBlocksDisabled = world.FallingBlocksDisabled;
            worldData.FallingBlockLayer = world.FallingBlockLayer;
            worldData.OverlapBlendSquares = world.OverlapBlendSquares;
            worldData.CullHiddenBlocks = world.DoNotGenerateHiddenBlocks;
            worldData.PixelsPerBlock = world.PixelsPerBlock;
            worldData.ZBlockDistance = world.ZBlockDistance;
            worldData.ZLayerFactor = world.ZLayerFactor;
            worldData.BlockLayers = world.BlockLayers;

            EditorUtility.SetDirty(worldData);

            //Reset the layerlist
            layerList = null;
        }

		void SetFluidChunk (bool enableFluidChunk)
		{
			AssetDatabase.StartAssetEditing ();
			{
                GameObject chunkPrefab = chunkLoader.ChunkPrefab;
                // GameObject instance = PrefabUtility.InstantiatePrefab (chunkPrefab) as GameObject;
                                                                        
                chunkPrefab.transform.GetChild (0).gameObject.SetActive (enableFluidChunk);
                PrefabUtility.SavePrefabAsset(chunkPrefab);
                // PrefabUtility.ReplacePrefab (instance, chunkPrefab, ReplacePrefabOptions.ConnectToPrefab);
				// DestroyImmediateImmediate (instance);
				AssetDatabase.SaveAssets ();
			}
			AssetDatabase.StopAssetEditing ();
		}

        bool IsFluidChunkEnabled()
        {
            GameObject chunkPrefab = chunkLoader.ChunkPrefab;
            if (chunkPrefab == null)
                throw new MissingReferenceException("The Chunk prefab is not set in the ChunkLoader script.");
            return chunkPrefab.transform.GetChild(0).gameObject.activeSelf;
        }

    }
}