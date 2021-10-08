// Copyright (C) 2020 Matthew Wilson

Shader "TerrainEngine2D/Fluid"
{
	Properties
	{
		//The fluid texture
		_MainTex ("Texture", 2D) = "white" {}
		//The alpha value to be clipped for smoothing the texture edges
		_AlphaCutout("Alpha Cutout", Range(0, 1)) = 0.15
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }

		Pass
		{
			//ZWrite required for proper rendering order of terrain layers
			ZWrite On
			Cull Off

			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;
				return o;
			}

			sampler2D _MainTex;
			float _AlphaCutout;
		
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * i.color;
				clip(col.a - _AlphaCutout);
				return col;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
