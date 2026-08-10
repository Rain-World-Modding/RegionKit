Shader "Futile/MurkyWaterSaveMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		ZWrite Off
		Blend  Off
		Lighting Off
		Cull Off 

        Pass
        {
            CGPROGRAM
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag		
			#include "UnityCG.cginc"
			#include "_ShaderFix.cginc"

			#define MAX_AIR_POCKETS 8

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 scrPos : TEXCOORD1;
			};

			uniform float4 _airPockets[MAX_AIR_POCKETS];

			v2f vert (appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.scrPos = ComputeScreenPos(o.pos);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Cut out around air pockets
				for (int j = 0; j < MAX_AIR_POCKETS && _airPockets[j].z > _airPockets[j].x; j++) {
					float4 bounds = _airPockets[j];
					if (all(i.scrPos > bounds.xy) && all(i.scrPos < bounds.zw))
						return 0;
				}
				
				return 1;
			}

            ENDCG
        }
    }
}
