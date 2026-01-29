Shader "Alduris/BGCloudLight"
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
                    float4 clr : COLOR;
                };

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                half4 frag (v2f i) : SV_Target
                {
                    float2 textCoord = float2(floor(i.scrPos.x*_screenSize.x)/_screenSize.x, floor(i.scrPos.y*_screenSize.y)/_screenSize.y);
                    textCoord.x -= _spriteRect.x;
                    textCoord.y -= _spriteRect.y;
                    textCoord.x /= _spriteRect.z - _spriteRect.x;
                    textCoord.y /= _spriteRect.w - _spriteRect.y;

                    float dist = clamp(1 - 2*distance(i.uv.xy, half2(0.5, 0.5)), 0, 1);
                    half4 lvlcol = tex2D(_LevelTex, textCoord);
                    if(dist <= 0 || lvlcol.r < 1) return half4(0,0,0,0);

                    float clouds = tex2D(_CloudsTex,textCoord*fixed2(1,1)*.7+float2(_RAIN*.01,0)).x;
                    float len = length(i.uv*2-1);
                    float l2 = smoothstep(1,0.1,len);
                    clouds = l2*clouds;
                    return half4(i.clr.xyz, i.clr.w * clouds);
                }
                ENDCG
            }
        } 
    }
}
