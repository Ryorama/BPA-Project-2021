// Copyright (C) 2020 Matthew Wilson
Shader "TerrainEngine2D/LightSource"
{
	Properties
	{
		//Texture of the light
		_MainTex ("Texture", 2D) = "radial" {}
	}
	SubShader
	{
		Tags{
		"Queue"="Transparent" 
		"RenderType"="Transparent" 
		"IgnoreProjector"="True"
	}
		Cull Off Lighting Off ZWrite Off

		Pass
		{
		Blend OneMinusDstColor One

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
				o.color = v.color;
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb = col.rgb * i.color.rgb;
				col.a = (1 - col.a) * i.color.a;
				return col;
			}
			ENDCG
		}
	}
}
