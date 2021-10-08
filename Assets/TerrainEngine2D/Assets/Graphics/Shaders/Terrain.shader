// Copyright (C) 2020 Matthew Wilson
//Basic Transparent Cutout Shader used for the terrain block layers

Shader "TerrainEngine2D/Terrain"
{
	Properties
	{
		//Texture used for the terrain layer
		_MainTex ("Texture", 2D) = "white" {}
		//Main color of the terrain layer (primarily used for toggling visibility)
		_Color ("Color", Color) = (1, 1, 1, 1)
		//The threshold used for cutting out the texture
		_Cutout ("Cutout Threshold", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags { "Queue"="AlphaTest+100" "RenderType"="Transparent"}

		Pass
		{
			//Cull hidden pixels
			Cull Back
			//ZWrite required for proper rendering order of terrain layers
			ZWrite On

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float4 _Color;
			float _Cutout;

			fixed4 frag (v2f i) : SV_Target
			{
				//Get the main texture and multiply it by the main color
				fixed4 col = tex2D(_MainTex, i.uv) * _Color;
				//Clip pixels with an alpha less than the cutout threshold
				clip(col.a - _Cutout);
				return col;
			}
			ENDCG
		}
	}
	Fallback "Legacy Shaders/Transparent/Cutout/Diffuse"
}
