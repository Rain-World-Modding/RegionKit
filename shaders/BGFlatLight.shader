Shader "RegionKit/BGFlatLight"
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
                #pragma multi_compile_local _ cloudlight
                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"
                #include "_Functions.cginc"
                #include "_TerrainMask.cginc"
                #include "_Snow.cginc"

                float4 _MainTex_ST;
                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _CloudsTex;
                uniform float _fogAmount;
                uniform float _RAIN;
                
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
                    float dist = clamp(1 - 2*distance(i.uv.xy, half2(0.5, 0.5)), 0, 1);
                    half4 lvlcol = tex2D(_LevelTex, i.textCoord);
                    lvlcol = AddTerrain(lvlcol, i.textCoord, _spriteRect);
                    lvlcol = AddSnow(lvlcol,i.textCoord,i.scrPos);
                    if(dist <= 0 || lvlcol.r < 1) return half4(0,0,0,0);

                    #if cloudlight

                    float clouds = tex2D(_CloudsTex,i.textCoord*fixed2(1,1)*.7+float2(_RAIN*.04,0)).x;
                    float len = length(i.uv*2-1);
                    float l2 = smoothstep(1,0.1,len);
                    clouds = l2*clouds;
                    dist = clouds;

                    #endif

                    return half4(i.clr.xyz, i.clr.w * dist);
                }
                ENDCG
            }
        } 
    }
}
