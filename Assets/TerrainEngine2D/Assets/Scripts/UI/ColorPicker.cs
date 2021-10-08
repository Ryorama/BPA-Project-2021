using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Controls the color picker for the Fluid Tool OSD
    /// </summary>
    public class ColorPicker : MonoBehaviour,
       IPointerDownHandler
     , IPointerUpHandler
     , IDragHandler
    {
        //Amount to offset the position of the color selector from the edge of the color wheel
        private const float ColorWheelEdgeOffset = 2;
        //Reference to objects for setting the color data
        private WorldInputHandler worldInputHandler;

        [SerializeField]
        private RectTransform selectorRT;
        [SerializeField]
        private Image colorWheelImage;
        [SerializeField]
        private Image fluidToolImage;

        private GridSelectorImageSetter gridSeletorImageSetter;

        //Positions of the Color Wheel corners for determining the size of the Color Wheel and the position of the selected pixel
        private Vector3[] colorWheelCorners;
        //Color Wheel size variables used to grab the pixel data from the texture
        private int colorWheelWidth;
        private int colorWheelHeight;
        private float colorWheelRadius;

        //The previous screen size
        private int prevScreenWidth;
        private int prevScreenHeight;


        private void Awake()
        {
            colorWheelCorners = new Vector3[4];
            prevScreenWidth = Screen.width;
            prevScreenHeight = Screen.height;
        }

        private void Start()
        {
            worldInputHandler = WorldInputHandler.Instance;
            gridSeletorImageSetter = GridSelectorImageSetter.Instance;
            if(gridSeletorImageSetter != null)
                gridSeletorImageSetter.Initialize();
            Initialize();
        }

        private void Initialize()
        {
            //Determine the size of the Color Wheel
            colorWheelImage.rectTransform.GetWorldCorners(colorWheelCorners);
            colorWheelWidth = Mathf.FloorToInt(colorWheelCorners[3].x - colorWheelCorners[0].x);
            colorWheelHeight = Mathf.FloorToInt(colorWheelCorners[1].y - colorWheelCorners[0].y);
            colorWheelRadius = (colorWheelCorners[2].x - colorWheelCorners[0].x) / 2f - ColorWheelEdgeOffset;
            //Get the initial selected color
            Color32 color = PickColor();
            //Set the fluid color in the WorldInputHandler for placing fluid
            worldInputHandler.FluidColor = new Color32(color.r, color.g, color.b, worldInputHandler.FluidColor.a);
            //Setup the image of the Grid Selector with the new fluid color
            if (gridSeletorImageSetter != null)
                gridSeletorImageSetter.SetImageToFluidBlock(worldInputHandler.FluidColor);
            //Setup the image of the Fluid Tool icon
            fluidToolImage.color = worldInputHandler.FluidColor;
        }

        private void Update()
        {
            if (prevScreenWidth != Screen.width || prevScreenHeight != Screen.height)
                OnScreenResolutionChange();
        }

        private void OnScreenResolutionChange()
        {
            //Set the new size of the render textures
            prevScreenWidth = Screen.width;
            prevScreenHeight = Screen.height;
            Initialize();
        }

        /// <summary>
        /// Get the current pixel color from the position of the selector on the color wheel
        /// </summary>
        /// <returns>Returns the color of the current selected pixel</returns>
        public Color32 PickColor()
        {
            Texture mainTexture = colorWheelImage.mainTexture;
            //Get reference to the current render texture
            RenderTexture currentRT = RenderTexture.active;
            //Create a new render texture tbe same size as the color wheel
            RenderTexture renderTexture = new RenderTexture(colorWheelWidth, colorWheelHeight, 32);
            //Copy the texture into the renderTexture
            Graphics.Blit(mainTexture, renderTexture);
            RenderTexture.active = renderTexture;
            //Set the position of the pixel in the texture based on the position of the selector
            colorWheelImage.rectTransform.GetWorldCorners(colorWheelCorners);
            Vector2 pixelPosition = selectorRT.transform.position - colorWheelCorners[0];
            pixelPosition = new Vector2(pixelPosition.x, colorWheelHeight - pixelPosition.y);
            //Create a 1x1 pixel sized Texture2D using the position of the pixel
            Texture2D tex2d = new Texture2D(1, 1);
            tex2d.ReadPixels(new Rect((int)pixelPosition.x, (int)pixelPosition.y, 1, 1), 0, 0);
            tex2d.Apply();
            //Get the color of that pixel
            Color32 pixel = tex2d.GetPixel(0, 0);
            //Return the active render texture to its previous state
            RenderTexture.active = currentRT;
            return pixel;
        }

        /// <summary>
        /// Set the position of the selector on mouse down
        /// </summary>
        /// <param name="eventData">Event data from the pointer</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            SetSelectorPosition();
        }

        /// <summary>
        /// Set the position of the selector as the pointer is being dragged
        /// </summary>
        /// <param name="eventData">Event data from the pointer</param>
        public void OnDrag(PointerEventData eventData)
        {
            SetSelectorPosition();
        }

        /// <summary>
        /// Set the newly selected color
        /// </summary>
        /// <param name="eventData">Event data from the pointer</param>
        public void OnPointerUp(PointerEventData eventData)
        {
            Color32 pixel = PickColor();
            worldInputHandler.FluidColor = new Color32(pixel.r, pixel.g, pixel.b, worldInputHandler.FluidColor.a);
            if (gridSeletorImageSetter != null)
                gridSeletorImageSetter.SetImageToFluidBlock(worldInputHandler.FluidColor);
            fluidToolImage.color = worldInputHandler.FluidColor;
        }

        /// <summary>
        /// Sets the position of the Color Selector on the Color Wheel
        /// </summary>
        private void SetSelectorPosition()
        {
            Vector3 newPosition = Input.mousePosition;
            Vector3 centerPosition = colorWheelImage.transform.position;
            //Restrict the position of the Color Selector to inside the radius of the Color Wheel
            if (Vector3.Distance(centerPosition, newPosition) > colorWheelRadius)
            {
                Vector3 diff = Vector3.Normalize(centerPosition - newPosition) * colorWheelRadius;
                newPosition = centerPosition - diff;
            }
            selectorRT.transform.position = newPosition;
        }
    }
}