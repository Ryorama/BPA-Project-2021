using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Extras
{
    /// <summary>
    /// This script rotates the transform to face the mouse cursor
    /// </summary>
    public class FaceCursor : MonoBehaviour
    {
        [SerializeField]
        [Range(0, 1)]
        private float rotationRate = 0.5f;

        private void Update()
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            float angle = Mathf.Atan2(mousePosition.x, mousePosition.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.back), rotationRate);
        }
    }
}