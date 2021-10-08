using UnityEngine;
using UnityEngine.EventSystems;

namespace TerrainEngine2D
{
    /// <summary>
    /// Basic camera controller for camera movement
    /// </summary>
    public class CameraController : MonoBehaviourSingleton<CameraController>
    {
        //How the camera will follow the object
        //None: Do not follow the object
        //Permanent: Will permanently follow the object until Follow Object is disabled
        //Focus: Will follow an object until the user uses Horizontal/Veritcal movement or right clicks
        public enum Followtype { None, Permanent, Focus }

        private World world;
        private Camera mainCamera;
        private Camera lightingCamera;
        private Camera overlayCamera;
        [Header("Preferences")]
        [SerializeField]
        [Tooltip("Turn this off if you wish to manually set the culling mask of all the cameras")]
        private bool autoSetCullingMask = true;
        [Header("Movement")]
        [Tooltip("Speed of camera movement")]
        [SerializeField]
        private float movementSpeed = 5;
        [Tooltip("Amount to move each frame")]
        [SerializeField]
        private float movementSensitivity = 1.2f;
        [Tooltip("Rate at which the camera will lerp/interpolate to a new position while panning")]
        [SerializeField]
        [Range(0, 1)]
        private float panRate = 0.2f;
        //Origin of the mouse click for dragging the screen
        private Vector2 panOrigin;
        [Header("Zooming")]
        [Tooltip("Whether to zoom towards the center of the camera or towards the cursor")]
        [SerializeField]
        private bool zoomToCursor = true;
        [Tooltip("Speed of camera zoom")]
        [SerializeField]
        private float zoomSpeed = 20;
        [Tooltip("Amount the camera will zoom per tick")]
        [SerializeField]
        private float zoomSensitivity = 5;
        [Tooltip("Maximum camera zoom")]
        [SerializeField]
        private float maxZoom = 30;
        [Tooltip("Minimum camera zoom")]
        [SerializeField]
        private float minZoom = 1;
        //New camera orthographic size used for zooming
        private float newCameraSize;
        //New position coordinates for the camera
        private Vector3 newPosition;
        //Camera's movement velocity
        private Vector3 movementVelocity;
        //Camera's zoom velocity
        private float zoomVelocity;
        [Header("Follow Camera")]
        [Tooltip("Whether the camera will follow an object (disables movement controls)")]
        [SerializeField]
        private Followtype followType;
        [Tooltip("Transform of the object the camera will follow")]
        [SerializeField]
        private Transform objectToFollow;
        [Tooltip("The camera offset from the object being followed")]
        [SerializeField]
        private Vector2 objectCameraOffset;

        [Header("Pixel Perfect Properties")]
        [Tooltip("Use a pixel perfect orthographic camera size")]
        [SerializeField]
        private bool pixelPerfectCamera = false;
        [Tooltip("Round camera position to nearest pixel")]
        [SerializeField]
        private bool pixelPerfectMovement = false;
        [Tooltip("Set scale for the orthographic camera size when pixelPerfectCamera is enabled (higher scale means lower camera size")]
        [SerializeField]
        private int cameraSizeScale = 1;

        protected override void Awake()
        {
            base.Awake();
        }

        private void Start()
        {
            world = World.Instance;

            //Setup the Culling Mask
            if (autoSetCullingMask)
            {
                mainCamera = GetComponent<Camera>();
                if (mainCamera != null)
                {
                    mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Terrain");
                    mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Lighting"));
                    if (world.BasicLighting)
                        mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("Ignore Lighting");
                    else
                        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Ignore Lighting"));
                }
                lightingCamera = transform.GetChild(0).GetComponent<Camera>();
                if (lightingCamera != null)
                {
                    lightingCamera.cullingMask = LayerMask.GetMask("Lighting");
                }
                overlayCamera = transform.GetChild(1).GetComponent<Camera>();
                if (overlayCamera != null)
                {
                    overlayCamera.cullingMask |= 1 << LayerMask.NameToLayer("Ignore Lighting");
                }
            }

            if (pixelPerfectCamera)
            {
                //Sets the orthographic camera size to match game pixels with screen pixels
                mainCamera.orthographicSize = (Screen.height / (world.PixelsPerBlock * cameraSizeScale)) * 0.5f;
                //Resets values for camera zoom
                maxZoom = mainCamera.orthographicSize;
                minZoom = maxZoom;
                zoomSpeed = 0;
            }

            //Ensures orthographic camera size is less than the maximum and greater than minimum
            if (mainCamera.orthographicSize > maxZoom)
                mainCamera.orthographicSize = maxZoom;
            if (mainCamera.orthographicSize < minZoom)
                mainCamera.orthographicSize = minZoom;
            newCameraSize = mainCamera.orthographicSize;

            //Set z position/render order of the camera
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint - world.ZBlockDistance * world.ZLayerFactor * World.CAMERA_Z_ORDER);
            newPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (followType == Followtype.None)
                return;

            bool isCursorHoveringUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            Vector2 mousePosition = Input.mousePosition;
            bool isCursorOnScreen = mousePosition.x > 0 && mousePosition.x < Screen.width && mousePosition.y > 0 && mousePosition.y < Screen.height;

            float screenAspectRatio = Screen.width / (float)Screen.height;
            float camHalfWidth;
            float minX, minY, maxX, maxY;

            switch (followType)
            {
                case Followtype.Permanent:
                    if (!isCursorHoveringUI && !pixelPerfectCamera && isCursorOnScreen)
                    {
                        //Camera Zoom to mouse point
                        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 && mainCamera.orthographicSize > minZoom || Input.GetAxisRaw("Mouse ScrollWheel") < 0 && mainCamera.orthographicSize < maxZoom)
                        {
                            float zoomAmount = -Input.GetAxisRaw("Mouse ScrollWheel") * zoomSensitivity;
                            newCameraSize = Mathf.Clamp(newCameraSize + zoomAmount, minZoom, maxZoom);
                        }
                    }
                    mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, newCameraSize, zoomSpeed * Time.unscaledDeltaTime);

                    //Recalculate world boundaries after zoom
                    camHalfWidth = mainCamera.orthographicSize * screenAspectRatio;
                    minX = camHalfWidth;
                    maxX = world.WorldWidth - camHalfWidth;
                    minY = mainCamera.orthographicSize;
                    maxY = world.WorldHeight - mainCamera.orthographicSize;

                    newPosition = new Vector3(objectToFollow.position.x + objectCameraOffset.x, objectToFollow.position.y + objectCameraOffset.y, newPosition.z);

                    newPosition = Vector3.Lerp(transform.position, newPosition, movementSpeed * Time.unscaledDeltaTime);
                    //Ensure camera doesn't move outside the set boundaries
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
                    transform.position = newPosition;

                    return;
                case Followtype.Focus:
                    if (Input.GetAxisRaw("Vertical") != 0 || Input.GetAxisRaw("Horizontal") != 0)
                        followType = Followtype.None;
                    if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    {
                        panOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                        followType = Followtype.None;
                    }

                    if (!isCursorHoveringUI && !pixelPerfectCamera)
                    {
                        //Camera Zoom to mouse point
                        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 && mainCamera.orthographicSize > minZoom || Input.GetAxisRaw("Mouse ScrollWheel") < 0 && mainCamera.orthographicSize < maxZoom)
                        {
                            float zoomAmount = -Input.GetAxisRaw("Mouse ScrollWheel") * zoomSensitivity;
                            newCameraSize = Mathf.Clamp(newCameraSize + zoomAmount, minZoom, maxZoom);
                        }
                    }
                    mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, newCameraSize, zoomSpeed * Time.unscaledDeltaTime);

                    //Recalculate world boundaries after zoom
                    camHalfWidth = mainCamera.orthographicSize * screenAspectRatio;
                    minX = camHalfWidth;
                    maxX = world.WorldWidth - camHalfWidth;
                    minY = mainCamera.orthographicSize;
                    maxY = world.WorldHeight - mainCamera.orthographicSize;

                    newPosition = new Vector3(objectToFollow.position.x + objectCameraOffset.x, objectToFollow.position.y + objectCameraOffset.y, newPosition.z);
                    newPosition = Vector3.Lerp(transform.position, newPosition, movementSpeed * Time.unscaledDeltaTime);
                    //Ensure camera doesn't move outside the set boundaries
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
                    transform.position = newPosition;
                    return;
            }
        }

        private void LateUpdate()
        {
            if (followType != Followtype.None)
                return;

            bool isCursorHoveringUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            Vector2 mousePosition = Input.mousePosition;
            bool isCursorOnScreen = mousePosition.x > 0 && mousePosition.x < Screen.width && mousePosition.y > 0 && mousePosition.y < Screen.height;

            float screenAspectRatio = Screen.width / (float)Screen.height;
            float camHalfWidth;
            float minX, minY, maxX, maxY;

            //Double the speed if holding shift
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                movementSensitivity *= 2f;
                zoomSensitivity *= 2f;
            }
            else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
            {
                movementSensitivity /= 2f;
                zoomSensitivity /= 2f;
            }

            if (!isCursorHoveringUI && !pixelPerfectCamera && isCursorOnScreen)
            {
                //Camera Zoom to mouse point
                if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 && mainCamera.orthographicSize > minZoom || Input.GetAxisRaw("Mouse ScrollWheel") < 0 && mainCamera.orthographicSize < maxZoom)
                {
                    if (zoomToCursor)
                    {
                        Vector3 currentPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                        float zoomAmount = -Input.GetAxisRaw("Mouse ScrollWheel") * zoomSensitivity;
                        zoomAmount = Mathf.Clamp(zoomAmount, minZoom - newCameraSize, maxZoom - newCameraSize);
                        newPosition -= (currentPoint - transform.position) / (mainCamera.orthographicSize) * zoomAmount;
                        newCameraSize += zoomAmount;
                    } else
                    {
                        float zoomAmount = -Input.GetAxisRaw("Mouse ScrollWheel") * zoomSensitivity;
                        newCameraSize = Mathf.Clamp(newCameraSize + zoomAmount, minZoom, maxZoom);
                    }
                }
            }
            mainCamera.orthographicSize = newCameraSize;//Mathf.Lerp(cam.orthographicSize, newCameraSize, zoomSpeed * Time.unscaledDeltaTime);
            //Recalculate world boundaries after zoom
            camHalfWidth = mainCamera.orthographicSize * screenAspectRatio;
            minX = camHalfWidth;
            maxX = world.WorldWidth - camHalfWidth;
            minY = mainCamera.orthographicSize;
            maxY = world.WorldHeight - mainCamera.orthographicSize;

            //Ensure camera doesn't move outside the set boundaries
            newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
            newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);
            transform.position = newPosition;//Vector3.Lerp(transform.position, newPosition, zoomSpeed * Time.unscaledDeltaTime);

            //Pan camera by click and dragging
            if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || (Input.GetKey(KeyCode.RightControl))))
            {
                //Get the origin of the mouse click
                if (Input.GetMouseButtonDown(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) && !isCursorHoveringUI))
                    panOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                //Move the camera in the opposite direction of which the mouse is moving
                if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    //Get the current position of the mouse
                    Vector2 currentPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    //Find the new destination of the camera
                    Vector2 destination = panOrigin - (currentPoint - (Vector2)transform.position);
                    //Set the camera's new position by lerping between its current position and the destination for smooth motion
                    newPosition.x = Mathf.Lerp(transform.position.x, destination.x, panRate);
                    newPosition.y = Mathf.Lerp(transform.position.y, destination.y, panRate);

                    //Ensure camera doesn't move outside the set boundaries
                    newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                    newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

                    transform.position = newPosition;
                }
            }
            else
            {
                if (Time.timeScale == 0)
                {
                    //Move camera with horizontal and vertical input, scales depending on the zoom level
                    newPosition.y += Input.GetAxisRaw("Vertical") * movementSensitivity * mainCamera.orthographicSize * Time.unscaledDeltaTime;
                    newPosition.x += Input.GetAxisRaw("Horizontal") * movementSensitivity * mainCamera.orthographicSize * Time.unscaledDeltaTime;
                }
                else
                {
                    //Move camera with horizontal and vertical input, scales depending on the zoom level
                    newPosition.y += Input.GetAxis("Vertical") * movementSensitivity * mainCamera.orthographicSize * Time.unscaledDeltaTime;
                    newPosition.x += Input.GetAxis("Horizontal") * movementSensitivity * mainCamera.orthographicSize * Time.unscaledDeltaTime;
                }

                //Ensure camera doesn't move outside the set boundaries
                newPosition.x = Mathf.Clamp(newPosition.x, minX, maxX);
                newPosition.y = Mathf.Clamp(newPosition.y, minY, maxY);

                transform.position = Vector3.Lerp(transform.position, newPosition, movementSpeed * Time.unscaledDeltaTime);
            }


            //Maintian pixel perfect camera positioning
            if (pixelPerfectMovement)
            {
                //Round to nearest pixel to maintain pixel perfect movement
                newPosition.x = Mathf.Round(newPosition.x * world.PixelsPerBlock) / world.PixelsPerBlock;
                newPosition.y = Mathf.Round(newPosition.y * world.PixelsPerBlock) / world.PixelsPerBlock;
            }
        }

        /// <summary>
        /// Follow a specified transform
        /// </summary>
        /// <param name="objectTransform">The transform to follow</param>
        /// <param name="followType">The type of following</param>
        public void FollowObject(Transform objectTransform, Followtype followType)
        {
            objectToFollow = objectTransform;
            this.followType = followType;
        }

        /// <summary>
        /// Follow a specified transform with a camera offset
        /// </summary>
        /// <param name="objectTransform">The transform to follow</param>
        /// <param name="followType">The type of following</param>
        /// <param name="objectCameraOffset">The offset for the camera</param>
        public void FollowObject(Transform objectTransform, Followtype followType, Vector2 objectCameraOffset)
        {
            objectToFollow = objectTransform;
            this.followType = followType;
            this.objectCameraOffset = objectCameraOffset;
        }

        /// <summary>
        /// Follow a specified transform
        /// </summary>
        /// <param name="objectTransform">The transform to follow</param>
        /// <param name="followType">The type of following</param>
        /// <param name="cameraZoom">Zoom to set the camera to</param>
        public void FollowObject(Transform objectTransform, Followtype followType, float cameraZoom)
        {
            objectToFollow = objectTransform;
            this.followType = followType;
            newCameraSize = cameraZoom;
        }

        /// <summary>
        /// Follow a specified transform with a camera offset
        /// </summary>
        /// <param name="objectTransform">The transform to follow</param>
        /// <param name="followType">The type of following</param>
        /// <param name="cameraZoom">Zoom to set the camera to</param>
        /// <param name="objectCameraOffset">The offset for the camera</param>
        public void FollowObject(Transform objectTransform, Followtype followType, float cameraZoom, Vector2 objectCameraOffset)
        {
            objectToFollow = objectTransform;
            this.followType = followType;
            newCameraSize = cameraZoom;
            this.objectCameraOffset = objectCameraOffset;
        }

        /// <summary>
        /// Stop following the object
        /// </summary>
        public void StopFollowingObject()
        {
            followType = Followtype.None;
        }
    }
}