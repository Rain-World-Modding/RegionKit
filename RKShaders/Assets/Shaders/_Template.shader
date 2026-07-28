Shader "RegionKit/Template"
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

                //sampler2D _GrabTexture;
                
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
                    // Main shader code goes here
                    return i.clr;
                }
                ENDCG
            }
        } 
    }
}
