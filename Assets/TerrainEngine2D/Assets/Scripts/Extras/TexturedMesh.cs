using UnityEngine;

// Copyright (C) 2020 Matthew Wilson

namespace TerrainEngine2D
{
    /// <summary>
    /// Generates a custom texture and renders it to a mesh covering the loaded terrain
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class TexturedMesh : MonoBehaviour
    {
        /// <summary>
        /// The filter mode of the texture
        /// </summary>
        public FilterMode FilterMode;

        //Render Texture for generating the light texture
        protected RenderTexture outputTexture;
        //The texture of the ambient light
        protected Texture2D texture2D;
        //Mesh properties
        protected Mesh mesh;
        protected Vector3[] vertices;
        protected Vector2[] uvs;
        protected int[] triangles;
        //Light data for the texture
        protected Color32[] tempPixelData;
        //Size of the texture
        protected int width, height;
        //Whether to update the ambient light
        protected bool update;
        //Material used for the generated mesh
        protected Material material;

        protected virtual void Awake()
        {
            mesh = GetComponent<MeshFilter>().mesh;
        }

        public virtual void Initialize(int width, int height)
        {
            this.width = width;
            this.height = height;
            outputTexture = new RenderTexture(width, height, 0, 
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            outputTexture.enableRandomWrite = true;
            outputTexture.filterMode = FilterMode; 
            outputTexture.Create();
            texture2D = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture2D.filterMode = FilterMode;
            //Clamp the texture to prevent it from repeating
            texture2D.wrapMode = TextureWrapMode.Clamp;

            tempPixelData = new Color32[width * height];
            //Setup the material
            material = GetComponent<MeshRenderer>().material;
            material.mainTexture = texture2D;

            //-----Generate the mesh-----
            //Simple rectangle to mask the loaded world

            //Setup vertices array
            vertices = new Vector3[4];
            vertices[0] = new Vector3(0, 0, 0);
            vertices[1] = new Vector3(0, height, 0);
            vertices[2] = new Vector3(width, height, 0);
            vertices[3] = new Vector3(width, 0, 0);
            //Setup triangles array
            triangles = new int[6];
            triangles[0] = 0;
            triangles[1] = 1;
            triangles[2] = 2;
            triangles[3] = 2;
            triangles[4] = 3;
            triangles[5] = 0;
            //Setup uvs array
            uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(0, 1);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(1, 0);
            //Set the mesh properties
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            //No need to keep the data in memory
            vertices = null;
            triangles = null;
            uvs = null;
        }

        /// <summary>
        /// Set the texture for an update
        /// </summary>
        public void UpdateTexture()
        {
            update = true;
        }

        protected virtual void LateUpdate()
        {
            //Check if the light system needs to update and the lighting is not disabled
            if (update)
            {
                //Regenerate the texture
                GenerateTexture(GetPixelData());
                update = false;
            }
        }

        protected abstract Color32[] GetPixelData();

        /// <summary>
        /// Generate the texture
        /// </summary>
        /// <param name="pixels">The pixel data for the texture</param>
        protected void GenerateTexture(Color32[] pixels)
        {
            //Generate a new texture from the pixel data
            RenderTexture.active = outputTexture;
            texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture2D.SetPixels32(pixels);
            texture2D.Apply();
            RenderTexture.active = null;
        }


    }
}