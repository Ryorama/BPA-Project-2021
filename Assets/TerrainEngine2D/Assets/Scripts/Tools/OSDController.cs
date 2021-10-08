using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TerrainEngine2D.Lighting;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Controller for the OSD
    /// </summary>
    public class OSDController : MonoBehaviourSingleton<OSDController>
    {
        public enum Tool
        {
            Terrain,
            Fluid,
            Light
        }

        //Reference to the world for getting/setting block data/info
        private World world;
        [Header("Objects")]
        //Set selected layer and block properties for the input handler
        private WorldInputHandler worldInputHandler;
        //Set the chunkLoader
        private ChunkLoader chunkLoader;
        //The OSD controls the images of the Grid Selector
        private GridSelectorImageSetter imageSelector;
        //Transform of the layer selection content rect - used as a parent for instantiating layer options
        [SerializeField]
        private Transform layerSelectionContent;
        //The RectTransform used for scaling
        [SerializeField]
        private RectTransform scaleTransform;
        //The RectTransform use for the color picker
        [SerializeField]
        private RectTransform colorPickerRT;
        //Layer option prefab to setup the layer selection box
        [SerializeField]
        private GameObject layerOptionPrefab;
        //Reference to the GameObject for showing and hiding control information
        [SerializeField]
        private GameObject controlsGO;
        [SerializeField]
        //Fluid and Block window GameObjects for showing and hiding tools
        private GameObject blockToolsGO;
        [SerializeField]
        private GameObject fluidToolsGO;
        [SerializeField]
        private GameObject lightToolToggleGO;
        [SerializeField]
        private GameObject fluidToolToggleGO;
        [SerializeField]
        private GameObject advancedFluidToolsGO;
        [Header("Images")]
        //Button for showing/hiding controls
        [SerializeField]
        private Image controlsHideButtonImage;
        //The color wheel image for getting color data
        [SerializeField]
        private Image colorWheelImage;
        //The icon image of the Color Tool
        [SerializeField]
        private Image fluidToolImage;
        [Header("Text")]
        //Dynamic TextMeshProUGUI objects
        [SerializeField]
        private TextMeshProUGUI selectedBlockText;
        [SerializeField]
        private TextMeshProUGUI numChunksText;
        [SerializeField]
        private TextMeshProUGUI numBlocksText;
        [SerializeField]
        private TextMeshProUGUI seedText;
        [SerializeField]
        private TextMeshProUGUI fpsText;
        [SerializeField]
        private TextMeshProUGUI blockSelectionText;
        [SerializeField]
        private TextMeshProUGUI buildToggleText;
        [SerializeField]
        private TextMeshProUGUI controlsText;
        [SerializeField]
        private TextMeshProUGUI lightingToggleText;
        [SerializeField]
        private TextMeshProUGUI modifyRadiusText;
        [SerializeField]
        private TextMeshProUGUI fluidModifyRadiusText;
        [SerializeField]
        private TextMeshProUGUI fluidOpacityText;
        [Header("Sliders")]
        //Slider used to set the modifying radius
        [SerializeField]
        private Slider modifyRadiusSlider;
        //Slider used to set the modifying radius
        [SerializeField]
        private Slider fluidModifyRadiusSlider;
        //Slider used to set the fluid opacity
        [SerializeField]
        private Slider fluidOpacitySlider;
        [Header("Toggles")]
        //Toggles the lighting
        [SerializeField]
        private Toggle lightToggle;
        //Toggles building/destroying for the World Modifier
        [SerializeField]
        private Toggle buildToggle;
        [SerializeField]
        private Toggle[] toolToggles;
        //Toggles for selecting modifying layers
        private Toggle[] layerToggles;
        //Toggles for setting the visiblity of layers
        private Toggle[] layerVisibilityToggles;
        [SerializeField]
        private ToggleGroup layerToggleGroup;
        [Header("Dropdowns")]
        //Dropdown for block selection (for building)
        [SerializeField]
        private TMP_Dropdown blockList;
        [Header("Input Fields")]
        //Input Field for setting the fluid density
        [SerializeField]
        private TMP_InputField fluidDensityInputField;
        [Header("Properties")]
        private float updateRate = 0.05f;
        /// <summary>
        /// Controls the rate at which the OSD updates the dynamic TextMeshProUGUI objects
        /// </summary>
        public float UpdateRate
        {
            get { return updateRate; }
        }

        //Blocktypes of all layers of the current selected position
        private byte[] selectedBlockTypes;

        protected override void Awake()
        {
            base.Awake();
            //Setup properties
            updateRate = World.WorldData.OSDUpdateRate;
        }

        private void Start()
        {
            world = World.Instance;
            worldInputHandler = WorldInputHandler.Instance;
            imageSelector = GridSelectorImageSetter.Instance;
            chunkLoader = ChunkLoader.Instance;
            if(imageSelector != null)
                imageSelector.Initialize();

            layerToggles = new Toggle[world.NumBlockLayers];
            layerVisibilityToggles = new Toggle[world.NumBlockLayers];
            selectedBlockTypes = new byte[world.NumBlockLayers];
            for (int i = 0; i < world.NumBlockLayers; i++)
            {
                //-----Setup the layer options-----
                //Get temporary reference to the current layer options
                GameObject layerOptionTemp = Instantiate(layerOptionPrefab, layerSelectionContent);
                GameObject layerToggleTemp = layerOptionTemp.transform.GetChild(0).gameObject;
                GameObject layerVisibilityToggleTemp = layerOptionTemp.transform.GetChild(1).gameObject;
                //Set the layer name
                layerToggleTemp.GetComponentInChildren<TextMeshProUGUI>().text = world.GetBlockLayer(i).Name;
                //Add the layer toggle to the array
                layerToggles[i] = layerToggleTemp.GetComponent<Toggle>();
                //Set the name of the layer toggle GameObject
                layerToggles[i].name = layerToggles[i].name + "LayerToggle";
                //Adds listener for when a layer toggle is selected, passes the layer index (id)
                int layerIndex = i;
                layerToggles[i].onValueChanged.AddListener((value) => { LayerSelected(layerIndex); });

                layerVisibilityToggles[i] = layerVisibilityToggleTemp.GetComponent<Toggle>();
                layerVisibilityToggles[i].name = layerVisibilityToggles[i].name + "LayerVisibilityToggle";
                layerVisibilityToggles[i].onValueChanged.AddListener((value) => { ToggleVisibility(layerIndex); });
                //Reset the alpha (visibility) of each blockLayer material
                Color layerMatColor = world.GetBlockLayer(i).Material.color;
                world.GetBlockLayer(i).Material.color = new Color(layerMatColor.r, layerMatColor.g, layerMatColor.b, 1);
            }
            //Setup the fluid density input field
            fluidDensityInputField.characterValidation = TMP_InputField.CharacterValidation.CustomValidator;
            fluidDensityInputField.text = "0";
            SetFluidDensity();
            SetFluidOpacity();

            worldInputHandler.Initialize();

            ToggleBuild();
            //Sets whether lighting is currently toggled
            lightToggle.isOn = !World.WorldData.LightingDisabled;
            if (lightToggle.isOn)
                lightingToggleText.text = "Lighting On";
            else
                lightingToggleText.text = "Lighting Off";
            //Begin with the Block Tools selected
            ToolSelected(0, true);
            //Sets the first layer toggle to on
            layerToggles[0].isOn = true;
            //Disable the lighting tool if basic lighting is selected
            if (World.WorldData.BasicLighting || World.WorldData.LightingDisabled)
                lightToolToggleGO.SetActive(false);
            //Remove the fluid tool if fluid is disabled
            if (World.WorldData.FluidDisabled)
                fluidToolToggleGO.SetActive(false);
            //Disable the advanced fluid tools if basic fluid is selected
            if (World.WorldData.BasicFluid)
            {
                advancedFluidToolsGO.SetActive(false);
                if(imageSelector != null)
                    imageSelector.SetImageToFluidBlock(((BasicFluidDynamics)FluidDynamics.Instance).MainColor);
                fluidToolImage.color = ((BasicFluidDynamics)FluidDynamics.Instance).MainColor;
            }
            //Sets the maximum slider value for the ModifyRadiusSlider
            modifyRadiusSlider.maxValue = World.WorldData.MaxModifyRadius;
            SetModifyRadiusValue();
            //Sets up the selected layers in the World Modifier
            SetSelectedLayers();
            seedText.text = "Seed: " + world.Seed;
            //Starts updating the dynamic text objects
            InvokeRepeating("OSDUpdate", 0, updateRate);
        }

        /// <summary>
        /// Updates the dynammic TextMeshProUGUI objects of the OSD
        /// </summary>
        void OSDUpdate()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int x = Mathf.FloorToInt(mousePosition.x);
            int y = Mathf.FloorToInt(mousePosition.y);

            for (int i = 0; i < selectedBlockTypes.Length; i++)
            {
                selectedBlockTypes[i] = world.GetBlockLayer(i).GetBlockType(x, y);
            }

            //Sets the string of block types at the current selected location
            SetBlockSelectionText();
            //Sets the SelectedBlockText to the Grid Selector's position
            SetSelectedBlockText(worldInputHandler.XGridPosition, worldInputHandler.YGridPosition);
            //Sets the frames per second
            SetFPSText((int)(1.0f / Time.smoothDeltaTime));
            //Sets the number of loaded chunks
            SetNumChunksText(chunkLoader.NumChunks);
            //Sets the total number of blocks in the world
            SetNumBlocksText(world.WorldWidth * world.WorldHeight);
        }

        /// <summary>
        /// Toggles the LightSystem MeshRenderer to show/hide the shadow mask
        /// </summary>
        public void ToggleLighting()
        {
            world.SetLighting(lightToggle.isOn);

            lightToolToggleGO.SetActive(lightToggle.isOn);
            if (!lightToggle.isOn && worldInputHandler.SelectedTool == Tool.Light)
                toolToggles[0].isOn = true;
            

            if (lightToggle.isOn)
                lightingToggleText.text = "Lighting On";
            else
                lightingToggleText.text = "Lighting Off";
        }

        /// <summary>
        /// Called when the ModifyRadiusSlider changes - Sets the modifying radius
        /// </summary>
        public void SetModifyRadiusValue()
        {
            //Reload the Grid Selector image with the new modifying radius
            switch (worldInputHandler.SelectedTool)
            {
                case Tool.Terrain:
                    worldInputHandler.ModifyRadius = (int)modifyRadiusSlider.value;
                    modifyRadiusText.text = "Brush Size: " + (1 + worldInputHandler.ModifyRadius * 2);
                    if (buildToggle.isOn)
                        SetSelectedBlock();
                    break;
                case Tool.Fluid:
                    worldInputHandler.ModifyRadius = (int)fluidModifyRadiusSlider.value;
                    fluidModifyRadiusText.text = "Brush Size: " + (1 + worldInputHandler.ModifyRadius * 2);
                    if (world.BasicFluid)
                    {
                        if (buildToggle.isOn && imageSelector != null)
                            imageSelector.SetImageToFluidBlock(((BasicFluidDynamics)FluidDynamics.Instance).MainColor);
                    } else
                    {
                        if (buildToggle.isOn && imageSelector != null)
                            imageSelector.SetImageToFluidBlock(worldInputHandler.FluidColor);
                    }
                    break;
                case Tool.Light:
                    if (buildToggle.isOn && imageSelector != null)
                        imageSelector.SetImageToLightSource();
                    break;
            }
            if (!buildToggle.isOn && imageSelector != null)
                imageSelector.SetImageToDefault();
        }

        /// <summary>
        /// Called when a visibility toggle is clicked - Toggles the visibility of that specific layer
        /// </summary>
        /// <param name="index">The index of the selected layer</param>
        public void ToggleVisibility(int index)
        {
            //Gets the current color of that layers material
            Color layerColor = world.GetBlockLayer(index).Material.color;
            //Sets the alpha of the layers material to show or hide that layer
            if (layerVisibilityToggles[index].isOn)
                world.GetBlockLayer(index).Material.color = new Color(layerColor.r, layerColor.g, layerColor.b, 1);
            else
                world.GetBlockLayer(index).Material.color = new Color(layerColor.r, layerColor.g, layerColor.b, 0);
        }

        /// <summary>
        /// Sets the BlockSelectionText to a string holding the names of the block types of each layer at the current position of the Grid Selector
        /// </summary>
        void SetBlockSelectionText()
        {
            const int MaxBlockInfoLayersShown = 4;
            //Temporary string to hold the block names
            string blockSelectionString = null;
            //Counter to cap the maximum amount of layers shown
            int counter = 0;
            //Loop through all the block layers
            for (int i = world.NumBlockLayers - 1; i >= 0; i--)
            {
                if (!layerToggles[i].isOn)
                    continue;
                //Get the selected block at the current layer
                byte selectedBlockType = selectedBlockTypes[i];
                //If there is no block at that point set the block name to "Air" else use the block name
                if (selectedBlockType != BlockLayer.AIR_BLOCK)
                {
                    blockSelectionString += world.GetBlockLayer(i).Name + " - " + world.GetBlockLayer(i).GetBlockInfo(selectedBlockType).Name + "\n";
                    counter++;
                    if (counter >= MaxBlockInfoLayersShown)
                        break;
                }
            }
            blockSelectionText.text = blockSelectionString;
            LayoutRebuilder.ForceRebuildLayoutImmediate(blockSelectionText.rectTransform);
        }

        /// <summary>
        /// Called when a LayerToggle is clicked - Sets the options for the BlockList dropdown and sets the selected layer(s)
        /// </summary>
        /// <param name="index">Index of the selected layer</param>
        public void LayerSelected(int index)
        {
            if (buildToggle.isOn)
            {
                //Create and add the list of block types of the selected layer to the BlockList dropdown
                List<string> blockOptions = new List<string>();
                for(int i = 1; i <= world.GetBlockLayer(index).GetNumBlockTypes(); i++)
                {
                    BlockInfo blockInfo = world.GetBlockLayer(index).GetBlockInfo(i);
                    blockOptions.Add(blockInfo.Name);
                }
                    
                blockList.ClearOptions();
                blockList.AddOptions(blockOptions);
                blockList.value = 0;
            }
            SetSelectedLayers();
        }

        /// <summary>
        /// Sets the current selected layer(s) and sets the current selected block
        /// </summary>
        public void SetSelectedLayers()
        {
            worldInputHandler.ClearSelectedLayers();
            //Sets the list of selected layers in the World Modifier
            for (int i = 0; i < layerToggles.Length; i++)
            {
                //If the current layer is toggled on, then add it to the list of selected layers
                if (layerToggles[i].isOn)
                    worldInputHandler.AddSelectedLayer((byte)i);
            }
            SetSelectedBlock();
        }

        /// <summary>
        /// Sets the current selected block
        /// </summary>
        public void SetSelectedBlock()
        {
            if (buildToggle.isOn)
            {
                //Set the selected block to the selected value of the BlockList dropdown
                worldInputHandler.SelectedBlock = (byte)(blockList.value + BlockLayer.BLOCK_INDEX_OFFSET);
                //Get the information on the selected block (only one layer can be selected when building)
                BlockInfo blockInfo = world.GetBlockLayer(worldInputHandler.SelectedLayers[0]).GetBlockInfo(worldInputHandler.SelectedBlock);
                //Disable and reset the ModifyRadiusSlider if placing regular blocks (they cannot be placed in multiples at a time)
                if (!blockInfo.OverlapBlock)
                {
                    modifyRadiusSlider.enabled = false;
                    modifyRadiusSlider.value = 0;
                    worldInputHandler.ModifyRadius = 0;
                }
                else
                {
                    //Ensure the ModifyingRadiusSlider is enabled
                    modifyRadiusSlider.enabled = true;
                }
                //Sets the image of the Grid Selector to the current selected block
                if (imageSelector != null)
                    imageSelector.SetImageToBlock(blockInfo, world.GetBlockLayer(worldInputHandler.SelectedLayers[0]).Material, worldInputHandler.SelectedBlock, worldInputHandler.SelectedLayers[0], world.GetBlockLayer(worldInputHandler.SelectedLayers[0]).ZLayerOrder);
            }
        }

        /// <summary>
        /// Called by the ModdifyToggles - Toggles properties for building or destroying blocks
        /// </summary>
        public void ToggleBuild()
        {
            //Building
            if (buildToggle.isOn)
            {
                buildToggleText.text = "Build";
                worldInputHandler.IsBuilding = true;
                if (worldInputHandler.SelectedTool == Tool.Terrain)
                {
                    //Resets all layer toggles to 'off' and sets the first toggle 'on' (ensures only one layer can be toggled on at once)
                    for (int i = 0; i < world.NumBlockLayers; i++)
                    {
                        layerToggles[i].group = layerToggleGroup;
                        layerToggles[i].isOn = false;
                    }
                    layerToggles[0].isOn = true;
                }
                else if (worldInputHandler.SelectedTool == Tool.Fluid)
                {
                    if (!world.BasicFluid)
                        advancedFluidToolsGO.SetActive(true);
                }
                //Resets the Grid Selector image
                if (imageSelector != null)
                {
                    switch (worldInputHandler.SelectedTool)
                    {
                        case Tool.Fluid:
                            if(world.BasicFluid)
                                imageSelector.SetImageToFluidBlock(((BasicFluidDynamics)FluidDynamics.Instance).MainColor);
                            else
                                imageSelector.SetImageToFluidBlock(worldInputHandler.FluidColor);
                            break;
                        case Tool.Light:
                            imageSelector.SetImageToLightSource();
                            break;
                    }
                }
            }
            //Destroying
            else
            {
                buildToggleText.text = "Remove";
                worldInputHandler.IsBuilding = false;

                if (worldInputHandler.SelectedTool == Tool.Terrain)
                {
                    //Removes the LayerToggles group to allow for multiple layers to be selected at once
                    for (int i = 0; i < world.NumBlockLayers; i++)
                    {
                        layerToggles[i].group = null;
                    }
                    //Clears the block list since it is not used when destroying
                    blockList.ClearOptions();
                    //Ensures the ModifyRadiusSlider is enabled
                    modifyRadiusSlider.enabled = true;
                } else if (worldInputHandler.SelectedTool == Tool.Fluid)
                {
                    advancedFluidToolsGO.SetActive(false);
                }
                //Resets the Grid Selector image
                if (imageSelector != null)
                    imageSelector.SetImageToDefault();
            }
        }

        /// <summary>
        /// Set the selected tool for use by the WorldInputHandler
        /// </summary>
        /// <param name="toolIndex">The index to the selected tool</param>
        /// <param name="force">Force the tool selection even if it is already selected in the WorldInputHandler</param>
        public void ToolSelected(int toolIndex, bool force)
        {
            if (toolToggles[toolIndex].isOn)
            {
                if (worldInputHandler.SelectedTool != (Tool)toolIndex || force)
                    SelectTool(toolIndex);
            }
        }

        /// <summary>
        /// Set the selected tool for use by the WorldInputHandler
        /// </summary>
        /// <param name="toolIndex">The index to the selected tool</param>
        public void ToolSelected(int toolIndex)
        {
            if (toolToggles[toolIndex].isOn)
            {
                if (worldInputHandler.SelectedTool != (Tool)toolIndex)
                    SelectTool(toolIndex);
            }
        }

        /// <summary>
        /// Set the selected tool for use by the WorldInputHandler
        /// </summary>
        /// <param name="toolIndex">The index to the selected tool</param>
        public void SelectTool(int toolIndex)
        {
            if (toolToggles[toolIndex].isOn)
            {
                worldInputHandler.SelectedTool = (Tool)toolIndex;
                switch (worldInputHandler.SelectedTool)
                {
                    case Tool.Terrain:
                        blockToolsGO.SetActive(true);
                        fluidToolsGO.SetActive(false);
                        if (buildToggle.isOn)
                            SetSelectedBlock();
                        else
                        {
                            if(imageSelector != null)
                                imageSelector.SetImageToDefault();
                        }
                        break;
                    case Tool.Fluid:
                        blockToolsGO.SetActive(false);
                        fluidToolsGO.SetActive(true);
                        if (imageSelector != null)
                        {
                            if(world.BasicFluid)
                                imageSelector.SetImageToFluidBlock(((BasicFluidDynamics)FluidDynamics.Instance).MainColor);
                            else
                                imageSelector.SetImageToFluidBlock(worldInputHandler.FluidColor);
                        }
                        break;
                    case Tool.Light:
                        blockToolsGO.SetActive(false);
                        fluidToolsGO.SetActive(false);
                        worldInputHandler.ModifyRadius = 0;
                        if (imageSelector != null)
                            imageSelector.SetImageToLightSource();
                        break;
                }
            }
            SetModifyRadiusValue();
            ToggleBuild();
        }

        /// <summary>
        /// Show/Hide the controls instructions window
        /// </summary>
        public void ToggleControls()
        {
            bool isActive = controlsGO.activeInHierarchy;
            controlsGO.SetActive(!isActive);
            controlsText.gameObject.SetActive(isActive);
            controlsHideButtonImage.enabled = !isActive;
        }

        /// <summary>
        /// Serialize the world data to file
        /// </summary>
        public void SaveBlockData()
        {
            if(world.SaveWorld)
                Serialization.SaveTerrainData();
            else
            {
                Serialization.Save();
                world.SaveWorld = true;
            }
        }

        /// <summary>
        /// Set the opacity of the fluid tool based on the UI slider values
        /// </summary>
        public void SetFluidOpacity()
        {
            byte opacity = (byte)Mathf.Min(fluidOpacitySlider.value, 255);
            fluidOpacityText.text = "Opacity: " + opacity;
            Color32 currColor = worldInputHandler.FluidColor;
            worldInputHandler.FluidColor = new Color32(currColor.r, currColor.g, currColor.b, opacity);
            if (imageSelector != null)
                imageSelector.SetImageToFluidBlock(worldInputHandler.FluidColor);
            fluidToolImage.color = worldInputHandler.FluidColor;
        }

        /// <summary>
        /// Set the density of the fluid tool base on the UI density input
        /// </summary>
        public void SetFluidDensity()
        {
            int density;
            if (int.TryParse(fluidDensityInputField.text, out density))
            {
                worldInputHandler.FluidDensity = (byte)density;
            }
            else
            {
                worldInputHandler.FluidDensity = 0;
                fluidDensityInputField.text = "0";
                Debug.LogWarning("Fluid Density Input Field could not be parsed to an Integer");
            }
        }

        /// <summary>
        /// Set the scale of the OSD
        /// </summary>
        /// <param name="scale">The factor to scale the OSD by</param>
        public void SetScale(float scale)
        {
            scaleTransform.localScale = new Vector3(scale, scale, 1);
            float scalingOffsetX = (scaleTransform.rect.width / 2f) * (scale - 1);
            float scalingOffsetY = (scaleTransform.rect.height / 2f) * (scale - 1);
            scaleTransform.offsetMin = new Vector2(scalingOffsetX, scalingOffsetY);
            scaleTransform.offsetMax = new Vector2(-scalingOffsetX, -scalingOffsetY);
        }

        /// <summary>
        /// Sets the text of the current selected block position
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public void SetSelectedBlockText(int x, int y)
        {
            selectedBlockText.text = "Selected Block \n (" + x + ", " + y + ")";
        }
        /// <summary>
        /// Sets the text of the current number of loaded chunks
        /// </summary>
        /// <param name="numChunks">Number of chunks</param>
        public void SetNumChunksText(int numChunks)
        {
            numChunksText.text = "Loaded Chunks: " + numChunks;
        }
        /// <summary>
        /// Sets the text of the total number of blocks in the world
        /// </summary>
        /// <param name="numBlocks">Total number of blocks</param>
        public void SetNumBlocksText(int numBlocks)
        {
            numBlocksText.text = "Total Blocks: " + numBlocks;
        }
        /// <summary>
        /// Sets the current frames-per-second of the game
        /// </summary>
        /// <param name="fps">Current Frames-Per-Second (FPS)</param>
        public void SetFPSText(int fps)
        {
            fpsText.text = "FPS: " + fps;
        }
    }
}