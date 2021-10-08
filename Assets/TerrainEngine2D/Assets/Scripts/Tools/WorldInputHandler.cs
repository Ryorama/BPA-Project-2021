using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TerrainEngine2D.Lighting;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Example**
    /// Handles input for the World Modifier
    /// </summary>
    public class WorldInputHandler : MonoBehaviourSingleton<WorldInputHandler>
    {
        private World world;
        private FluidDynamics fluidDynamics;
        private AdvancedLightSystem advancedLightSystem;

        [SerializeField]
        [Tooltip("A prefab of a GameObject with a LightSource Component")]
        private GameObject lightPrefab;
        [SerializeField]
        [Tooltip("The Z position for the light on instantiation")]
        private float lightZPosition;
        /// <summary>
        /// The manually set Z position for a placed light object
        /// </summary>
        public float LightZPosition
        {
            get { return lightZPosition; }
        }
        [SerializeField]
        [Tooltip("The LayerMask used to determine if a collider block can be placed (checks for any other 2d colliders that are not terrain)")]
        private LayerMask colliderMask;
        //The light source attached to the light prefab
        private LightSource prefabLightSource;

        private bool isBuilding;
        /// <summary>
        /// Whether terrain will be built on click
        /// If this is false, terrain will be destroyed on click
        /// </summary>
        public bool IsBuilding
        {
            get { return isBuilding; }
            set { isBuilding = value; }
        }
        private byte selectedBlock;
        /// <summary>
        /// Current selected block of the current selected layer (used when building)
        /// </summary>
        public byte SelectedBlock
        {
            get { return selectedBlock; }
            set { selectedBlock = value; }
        }
        private List<byte> selectedLayers;
        /// <summary>
        /// The current selected BlockLayers
        /// </summary>
        public List<byte> SelectedLayers
        {
            get { return selectedLayers; }
        }
        private int modifyRadius;
        /// <summary>
        /// The radius for modifying blocks
        /// </summary>
        public int ModifyRadius
        {
            get { return modifyRadius; }
            set { modifyRadius = value; }
        }
        private int xGridPosition;
        /// <summary>
        /// X position of the cursor in grid coordinates
        /// </summary>
        public int XGridPosition
        {
            get { return xGridPosition; }
        }
        private int yGridPosition;
        /// <summary>
        /// Y position of the cursor in grid coordinates
        /// </summary>
        public int YGridPosition
        {
            get { return yGridPosition; }
        }
        private OSDController.Tool selectedTool;
        /// <summary>
        /// The tool selected in the OSD for modifying the world
        /// </summary>
        public OSDController.Tool SelectedTool
        {
            get { return selectedTool; }
            set { selectedTool = value; }
        }
        private Color32 fluidColor = new Color32(0, 80, 200, 225);
        /// <summary>
        /// The color to set placed fluid (Advanced Fluid System)
        /// </summary>
        public Color32 FluidColor
        {
            get { return fluidColor; }
            set { fluidColor = value; }
        }
        private byte fluidDensity;
        /// <summary>
        /// The density to set the placed fluid (Advanced Fluid System)
        /// </summary>
        public byte FluidDensity
        {
            get { return fluidDensity; }
            set { fluidDensity = value; }
        }

        private bool initialized;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            prefabLightSource = lightPrefab.GetComponent<LightSource>();
            if (prefabLightSource == null)
            {
                prefabLightSource = lightPrefab.GetComponentInChildren<LightSource>();
                if(prefabLightSource == null)
                    throw new MissingComponentException("The Light Source Prefab is missing a LightSource component.");
            }
            if (!initialized)
                Initialize();
        }

        /// <summary>
        /// Setup the WorldInputHandler
        /// </summary>
        public void Initialize()
        {
            world = World.Instance;
            fluidDynamics = FluidDynamics.Instance;
            advancedLightSystem = AdvancedLightSystem.Instance;
            selectedLayers = new List<byte>(world.NumBlockLayers);
            selectedLayers.Add(0);
            isBuilding = false;
            initialized = true;
        }

        private void Update()
        {
            //Save the position of the mouse in grid coordinates
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            xGridPosition = Mathf.FloorToInt(mousePosition.x);
            yGridPosition = Mathf.FloorToInt(mousePosition.y);

            //Pressing the Left Mouse Button
            if (Input.GetMouseButton(0))
            {
                //Check if input should be handled
                if (DoNotHandleInput())
                    return;
                switch (selectedTool)
                {
                    case OSDController.Tool.Terrain:
                        //Place blocks if building is toggled, else destroy blocks at the mouse position
                        if (isBuilding)
                        {
                            //If this is a collider layer, check to make sure you aren't placing a block on another collider
                            if (world.GetBlockLayer(selectedLayers[0]).ColliderLayer)
                            {
                                RaycastHit2D hit = Physics2D.BoxCast(new Vector2(xGridPosition + 0.5f, yGridPosition + 0.5f), new Vector2(modifyRadius * 2 + 1 - 0.1f, modifyRadius * 2 + 1 - 0.1f), 0, Vector2.zero, 1, colliderMask);
                                if (hit.collider == null)
                                    WorldModifier.SetBlock(xGridPosition, yGridPosition, false, selectedLayers[0], selectedBlock, modifyRadius);
                            }
                            else
                                WorldModifier.SetBlock(xGridPosition, yGridPosition, false, selectedLayers[0], selectedBlock, modifyRadius);
                        }
                        else
                            WorldModifier.RemoveBlock(xGridPosition, yGridPosition, selectedLayers, modifyRadius);
                        break;
                    case OSDController.Tool.Fluid:
                        if (isBuilding)
                        {
                            if (world.BasicFluid)
                                WorldModifier.PlaceFluid(xGridPosition, yGridPosition, fluidDynamics.FluidDropAmount, modifyRadius);
                            else
                                WorldModifier.PlaceFluid(xGridPosition, yGridPosition, fluidDynamics.FluidDropAmount, modifyRadius, fluidDensity, fluidColor);
                        } else
                            WorldModifier.RemoveFluid(xGridPosition, yGridPosition, modifyRadius);
                        break;
                    case OSDController.Tool.Light:
                        if (world.BasicLighting)
                            return;

                        if (Input.GetMouseButtonDown(0))
                        {
                            if (isBuilding)
                            {
                                RaycastHit2D hit = Physics2D.Raycast(new Vector2(xGridPosition + 0.5f, yGridPosition + 0.5f), Vector2.zero);
                                if (hit.collider == null && world.GetBlockLayer(0).IsBlockAt(xGridPosition, yGridPosition) && !advancedLightSystem.IsLightAt(new Vector2Int(XGridPosition, yGridPosition)))
                                {
                                    if (!(prefabLightSource is BlockLightSource) || !AdvancedLightSystem.Instance.IsLightAt(new Vector2Int(xGridPosition, yGridPosition)))
                                        Instantiate(lightPrefab, new Vector3(xGridPosition, yGridPosition, lightZPosition), new Quaternion(0, 0, 0, 0));
                                }
                            } else
                            {
                                LightSource lightSource;
                                if (advancedLightSystem.TryGetLight(new Vector2Int(xGridPosition, yGridPosition), out lightSource))
                                    Destroy(lightSource.transform.parent.gameObject);
                            }
                        }
                        break;
                }
            }
            //Pressing the Right Mouse Button
            else if (Input.GetMouseButton(1))
            {
                if (DoNotHandleInput())
                    return;
                switch (selectedTool)
                {
                    case OSDController.Tool.Terrain:
                        WorldModifier.RemoveBlock(xGridPosition, yGridPosition, selectedLayers, modifyRadius);
                        break;
                    case OSDController.Tool.Fluid:
                        WorldModifier.RemoveFluid(xGridPosition, yGridPosition, modifyRadius);
                        break;
                    case OSDController.Tool.Light:
                        if (world.BasicLighting)
                            return;

                        LightSource lightSource;
                        if (advancedLightSystem.TryGetLight(new Vector2Int(xGridPosition, yGridPosition), out lightSource))
                            Destroy(lightSource.transform.parent.gameObject);
                        break;
                }
            }
        }

        /// <summary>
        /// Check if input should not be handled
        /// </summary>
        /// <returns>Returns true if mouse is hovering over UI</returns>
        bool DoNotHandleInput()
        {
            //Check if mouse is hovering over UI
            if (EventSystem.current)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return true;
            }
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                return true;
            return false;
        }

        /// <summary>
        /// Set the current selected layer
        /// </summary>
        /// <param name="layer">Index of the selected layer</param>
        public void SetSelectedLayer(byte layer)
        {
            if (selectedLayers == null)
                return;
            selectedLayers.Clear();
            selectedLayers[0] = layer;
        }
        /// <summary>
        /// Add a layer to the list of selected layers
        /// </summary>
        /// <param name="layer">Index of the layer to be added</param>
        /// <returns>Returns true if the layer was successfully added</returns>
        public bool AddSelectedLayer(byte layer)
        {
            if (selectedLayers == null)
                return false;
            if (selectedLayers.Count < world.NumBlockLayers)
            {
                selectedLayers.Add(layer);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clears all the selected layers
        /// </summary>
        public void ClearSelectedLayers()
        {
            if (selectedLayers == null)
                return;
            selectedLayers.Clear();
        }
    }
}
