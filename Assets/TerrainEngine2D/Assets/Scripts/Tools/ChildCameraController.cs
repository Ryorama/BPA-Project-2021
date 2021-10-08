using UnityEngine;

namespace TerrainEngine2D
{
    /// <summary>
    /// Controls a child camera to mimick a parent camera
    /// </summary>
    public class ChildCameraController : MonoBehaviour
    {
        private Camera childCamera;
        private Camera mainCamera;

        private void Awake()
        {
            childCamera = GetComponent<Camera>();
            mainCamera = Camera.main;
        }

        void Update()
        {
            childCamera.orthographicSize = mainCamera.orthographicSize;
        }
    }
}