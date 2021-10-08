using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D.Lighting
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    /// <summary>
    /// A source of light which generates a mesh
    /// </summary>
    public class MeshLight : LightSource
    {
        protected World world;

        /// <summary>
        /// Distance light can travel
        /// </summary>
        [Tooltip("Distance light can travel")]
        public int LightRadius = 10;
        /// <summary>
        /// Number of points to average across the light mesh for smoothing
        /// </summary>
        [Tooltip("Number of points to average across the light mesh for smoothing")]
        [Range(0, 10)]
        public int SmoothingIterations;
        [SerializeField]
        [Tooltip("Update the light dynamically")]
        private bool dynamic;
        /// <summary>
        /// Update the light dynamically
        /// </summary>
        public bool Dynamic
        {
            get { return dynamic; }
        }
        [SerializeField]
        [Tooltip("If the light does not move and you want it to be referenced in the AdvancedLightSystem")]
        private bool stationary;
        /// <summary>
        /// Wether the light moves or not
        /// Set this to true if you want to be able to reference this light from the AdvancedLightSystem
        /// </summary>
        public bool Stationary
        {
            get { return stationary; }
        }
        /// <summary>
        /// Speed of light flickering
        /// </summary>
        [Header("Flickering")]
        [Tooltip("Speed of lighting flicker (average)")]
        public float FlickerRate = 0;
        /// <summary>
        /// Amount the flicker light rate varies
        /// </summary>
        [Tooltip("Amount the flicker light rate varies")]
        public float FlickerRateVary = 0.1f;
        /// <summary>
        /// The flicker scaling radius
        /// </summary>
        [Tooltip("Flicker scaling radius")]
        public float FlickerScalingRadius = 0.1f;
        //List of light ray points for generating the light mask
        protected List<Vector2> lightPoints;
        //Reference to the mesh filter and renderer for generating the light mask
        private MeshFilter meshFilter;
        //Mesh variables for generating the chunk
        protected Mesh mesh;
        protected List<Vector3> vertices;
        protected List<int> triangles;
        protected List<Vector2> uvs;
        protected List<Color32> colors32;
        //Whether this light has been initialized
        private bool initialized;

        //Light flickering
        private float flickerTimer;
        //Whether to increment the flickerTimer
        private bool increaseTime = true;
        //Vary the flicker rate
        private float flickerVariation;

        protected virtual void OnEnable()
        {
            if(initialized)
                UpdateLight();
        }

        protected override void Awake()
        {
            base.Awake();
            meshFilter = GetComponent<MeshFilter>();

            gameObject.layer = LayerMask.NameToLayer("Lighting");
        }

        protected override void Start()
        {
            base.Start();
            world = World.Instance;
            //Set the z position/render order for the light
            StartCoroutine(SetRenderOrder());

            mesh = meshFilter.mesh;
            mesh.MarkDynamic();

            vertices = new List<Vector3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();
            colors32 = new List<Color32>();

            lightPoints = new List<Vector2>();

            initialized = true;

            UpdateLight();
        }

        private IEnumerator SetRenderOrder()
        {
            yield return 0;
            transform.position = new Vector3(transform.position.x, transform.position.y, world.EndZPoint - world.ZBlockDistance * world.ZLayerFactor * World.LIGHT_SYSTEM_Z_ORDER);
        }

        protected virtual void LateUpdate()
        {
            if (dynamic)
                UpdateLight();

            //Light flickering
            if (FlickerRate > 0)
            {
                transform.localScale = Vector3.Lerp(new Vector3(1 - FlickerScalingRadius, 1 - FlickerScalingRadius, 1), new Vector3(1 + FlickerScalingRadius, 1 + FlickerScalingRadius, 1), flickerTimer * (1 / (FlickerRate + flickerVariation)));
                if (increaseTime)
                {
                    flickerTimer += Time.deltaTime;
                    if (flickerTimer >= FlickerRate + flickerVariation)
                    {
                        increaseTime = false;
                        flickerVariation = Random.Range(-FlickerRateVary, FlickerRateVary);
                    }
                }
                else
                {
                    flickerTimer -= Time.deltaTime;
                    if (flickerTimer <= 0)
                    {
                        increaseTime = true;
                    }
                }
            }
        }

        /// <summary>
        /// Update the light mesh
        /// </summary>
        public void UpdateLight()
        {
            CalculateLighting();
            if (lightPoints.Count == 0)
                mesh.Clear();
            else
            {
                SmoothPoints();
                CreateMesh();
            }
        }

        /// <summary>
        /// Calculate the light mesh points
        /// </summary>
        protected virtual void CalculateLighting()
        {
            lightPoints.Add(Vector2Int.zero);
        }

        /// <summary>
        /// Smooth the light mesh vertices
        /// </summary>
        protected virtual void SmoothPoints()
        {

        }

        /// <summary>
        /// Add the light points to the mesh
        /// </summary>
        protected virtual void CreateMesh()
        {
            //Setup vertices
            vertices.Add(new Vector3(-LightRadius, -LightRadius, 0));
            vertices.Add(new Vector3(-LightRadius, LightRadius, 0));
            vertices.Add(new Vector3(LightRadius, LightRadius, 0));
            vertices.Add(new Vector3(LightRadius, -LightRadius, 0));
            //Setup triangles 
            triangles.Add(0);
            triangles.Add(1);
            triangles.Add(2);
            triangles.Add(2);
            triangles.Add(3);
            triangles.Add(0);
            //Setup uvs
            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));

            UpdateMesh();
        }

        /// <summary>
        /// Update the light mesh
        /// </summary>
        protected void UpdateMesh()
        {
            mesh.Clear();
            mesh.subMeshCount = 1;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors32);
            mesh.RecalculateNormals();

            vertices.Clear();
            uvs.Clear();
            triangles.Clear();
            colors32.Clear();

            lightPoints.Clear();
        }

        /// <summary>
        /// Converts a given angle in degrees to a direction
        /// </summary>
        /// <param name="angleInDegrees">The clockwise angle from the y axis</param>
        /// <returns></returns>
        protected Vector2 AngleToDirection(float angleInDegrees)
        {
            angleInDegrees += transform.eulerAngles.z;
            return new Vector2(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

    }
}