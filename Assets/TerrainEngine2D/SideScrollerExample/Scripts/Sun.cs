using UnityEngine;
using TerrainEngine2D.Lighting;

// Copyright (C) 2018 Matthew K Wilson

namespace TerrainEngine2D.SideScrollerDemo
{
    /// <summary>
    /// Controls the position of the sun 
    /// </summary>
    public class Sun : MonoBehaviour
    {
        private World world;
        private Transform mainCameraTransform;
        private SpriteRenderer spriteRenderer;

        [Header("Orbit")]
        [SerializeField]
        [Tooltip("The center of the suns orbit")]
        private Vector2 orbitCenter = new Vector2(0, 40);
        [SerializeField]
        [Tooltip("The radius of the suns orbit")]
        private float orbitRadius = 80;
        [Header("Sunrise")]
        [SerializeField]
        [Tooltip("The angle at which the sun will rise (clockwise from the -y axis)")]
        private float angleSunRise = 120;
        [Header("Sunset")]
        [SerializeField]
        [Tooltip("The angle at which the sun will set (clockwise from the -y axis)")]
        private float angleSunSet = 240;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            world = World.Instance;
            mainCameraTransform = Camera.main.transform;

            if(angleSunSet <= angleSunRise)
            {
                Debug.LogError("Angle of Sunset is less than or equal to the angle of Sunrise. Setting values to default.");
                angleSunRise = 90;
                angleSunSet = 270;
            }
        }

        private void Update()
        {
            float time = world.TimeOfDay;
            float timeSunrise = World.WorldData.SunriseTime;
            float timeSunset = World.WorldData.SunsetTime + 1;

            if (time < timeSunrise || time > timeSunset)
            {
                spriteRenderer.enabled = false;
                return;
            }
            else
                spriteRenderer.enabled = true;

            float maxSunlightAngle = angleSunSet - angleSunRise;
            float totalSunTime = timeSunset - timeSunrise;
            //Get the angle for the Sun vector
            float angleInDegrees = (time - timeSunrise) / totalSunTime * maxSunlightAngle;
            //The angle clockwise from the -y axis (represents the sun position at 0/24h)
            float angleForSunPosition = -angleInDegrees -angleSunRise - 90;
            //Get the Vector2 position of the sun
            Vector2 sunVector = DegreeToVector(angleForSunPosition) * orbitRadius + orbitCenter;
            //Set the position of the sun
            transform.position = new Vector3(sunVector.x + mainCameraTransform.position.x, sunVector.y, transform.position.z);
        }

        /// <summary>
        /// Convert an angle in degrees to a Vector2
        /// </summary>
        /// <param name="angle">Angle in degrees</param>
        /// <returns>Returns the resultant Vector2</returns>
        private Vector2 DegreeToVector(float angle)
        {
            float angleInRadians = Mathf.Deg2Rad * angle;
            return new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));
        }
    }
}