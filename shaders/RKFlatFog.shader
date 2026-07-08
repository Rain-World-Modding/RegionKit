Shader "RegionKit/RKFlatFog"
{
    Properties 
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    
    Category 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off  // we can turn backface culling off because we know nothing will be facing backwards

        SubShader
        {
            Pass 
            {
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"
                #include "_Functions.cginc"

                float4 _MainTex_ST;
                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _PalTex;
                sampler2D _PreLevelColorGrab;
                
                uniform float4 _spriteRect;
                uniform float2 _screenSize;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float2 textCoord : TEXCOORD2;
                    float4 clr : COLOR;
                };

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.textCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos);
                    o.clr = v.color;
                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    half3 fogColor = tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)).xyz;
                    half4 creatures = tex2D(_PreLevelColorGrab, i.scrPos.xy);
                    half4 lvlcol = tex2D(_LevelTex, i.textCoord);
                    
                    half grad = fmod(round(lvlcol.x * 255)-1, 30.0)/30.0;

                    if (lvlcol.x == 1.0 && lvlcol.y == 1.0 && lvlcol.z == 1.0) {
                        grad = 1.0;
                    }
                    if (grad > 6.0/30.0 && (creatures.x > 1.0/255.0 || creatures.y != 0.0 || creatures.z != 0.0)) {
                        grad = 6.0/30.0;
                    }

                    return half4(fogColor, lerp(i.clr.b, i.clr.a, saturate(iLerp(i.clr.r, i.clr.g, grad))));
                }
                ENDCG
            }
        } 
    }
}
