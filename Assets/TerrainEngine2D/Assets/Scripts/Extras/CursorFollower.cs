using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Extras
{
    /// <summary>
    /// Follows the cursor
    /// </summary>
    public class CursorFollower : MonoBehaviour
    {

        [SerializeField]
        private bool useScreenPosition;
        [SerializeField]
        private bool useGridPosition = true;

        private void Start()
        {
            World world = World.Instance;
        }

        void Update()
        {
            //Gets position of the mouse in world coordinates
            Vector3 mousePosition = Input.mousePosition;
            if(!useScreenPosition)
                mousePosition = Camera.main.ScreenToWorldPoint(mousePosition);
            if (useGridPosition)
            {
                //Converts the mouse position to a grid coordinate
                Vector3 gridPosition = new Vector3(Mathf.Floor(mousePosition.x), Mathf.Floor(mousePosition.y), transform.position.z);
                //Sets the GameObject's position to the grid coordinate
                transform.position = gridPosition;
            }
            else
                transform.position = new Vector3(mousePosition.x, mousePosition.y, transform.position.z);
        }
    }
}