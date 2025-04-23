Shader "Unlit/WaterFallDepth"
{
    Properties
    {
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    Category 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        ZWrite Off
        //Alphatest Greater 0
        Blend SrcAlpha OneMinusSrcAlpha 
        Fog { Color(0,0,0,0) }
        Lighting Off
        Cull Off //we can turn backface culling off because we know nothing will be facing backwards

        BindChannels 
        {
            Bind "Vertex", vertex
            Bind "texcoord", texcoord 
            Bind "Color", color 
        }

        SubShader   
        {
            GrabPass { }
            Pass 
            {
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ RIPPLE
                #include "UnityCG.cginc"
                #include "_ShaderFix.cginc"

                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _NoiseTex;
                sampler2D _PalTex;
                sampler2D _GameplayRipplePalTex;
                sampler2D _GameplayRippleMask;
                sampler2D _PreLevelColorGrab;

                #if defined(SHADER_API_PSSL)
                sampler2D _GrabTexture;
                #else
                sampler2D _GrabTexture : register(s0);
                #endif

                uniform float _RAIN;
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                //uniform float _waterPosition;

                uniform float _fogAmount;
                uniform float _rippleFogAmount;

                struct v2f {
                    float4  pos : SV_POSITION;
                    float2  uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float4 clr : COLOR;
                };

                float4 _MainTex_ST;

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos (v.vertex);
                    o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
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

                    // Figure out gradient color
                    half sincol = (sin((0.8*_RAIN + (tex2D(_NoiseTex, float2(textCoord.x*15, 1.8*_RAIN + textCoord.y*0.2) ).x * 3)) * 3.14 * 2)*0.5)+0.5;

                    half2 sinoff = half2(0, lerp(-0.013, 0.013, sincol));
                    half4 texcol = tex2D(_PreLevelColorGrab, textCoord+sinoff);
                    half4 lvlcol = tex2D(_LevelTex, textCoord+sinoff);

                    half grad = fmod(round(lvlcol.x * 255)-1, 30.0)/30.0;

                    if (lvlcol.x == 1.0 && lvlcol.y == 1.0 && lvlcol.z == 1.0) {
                        grad = 1.0;
                    }
                    if (grad > 6.0/30.0 && (texcol.x > 1.0/255.0 || texcol.y != 0.0 || texcol.z != 0.0)) {
                        grad = 6.0/30.0;
                    }
  
                    grad = pow(floor(lerp(grad, sincol, 0.2)*10)/10, 0.7);
 
                    // Flow rate
                    half edgeCloseness = i.uv.x < 0.5f ? i.uv.x*10.0 : (1.0-i.uv.x)*10.0;
                    edgeCloseness = min(edgeCloseness, i.uv.y < 0.5f ? i.uv.y*(1.0/i.clr.y) : (1.0-i.uv.y)*(1.0/i.clr.z));

                    if(lerp(edgeCloseness, sincol, 0.5) < 0.5) return float4(0,0,0,0);
                    if(sincol < 1-i.clr.x) return float4(0,0,0,0);

                    // Find the actual color (between deep water dark and light)
                    fixed4 color = lerp(tex2D(_PalTex, float2(5.5/32.0, 7.5/8.0)), tex2D(_PalTex, float2(4.5/32.0, 7.5/8.0)), grad);

                    #if RIPPLE
                        // Ripple shenanigans
                        fixed rippleMask  = tex2D(_GameplayRippleMask,i.scrPos).y;
                        rippleMask = smoothstep(0,.4,rippleMask);
                        fixed4 rippleColor = tex2D(_GameplayRipplePalTex,fixed2(7.5/32.0, 7.5/8.0));
                        color = lerp(color,rippleColor,rippleMask);
                    #endif

                    // Custom depth
                    half4 dpthcol = tex2D(_LevelTex, textCoord);
                    half dpth = 1.0-i.clr.w;

                    half terrainDpth = (((uint)round(dpthcol.x * 255)-1) % 30)/30.0;
                    if (dpthcol.x == 1 && dpthcol.y == 1 && dpthcol.z == 1) // sky
                        terrainDpth = 1;

                    if (terrainDpth > 6.0/30.0) { // creatures n stuff
                        float4 grabTexCol = tex2D(_PreLevelColorGrab, half2(i.scrPos.x, i.scrPos.y));
                        if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0)
                            terrainDpth = 6.0/30.0;
                    }

                    if (dpth > terrainDpth) return float4(0,0,0,0);

                    // Fog color lerp! (a custom thing)
                    float fogMult = (dpth - 6.0/30.0) * 0.8;
                    #if RIPPLE
                    color = lerp(color, tex2D(_GameplayRipplePalTex, float2(1.5/32.0, 7.5/8.0)), saturate(_rippleFogAmount * fogMult));
                    #else
                    color = lerp(color, tex2D(_PalTex, float2(1.5/32.0, 7.5/8.0)), saturate(_fogAmount * fogMult));
                    #endif

                    return color;
                }
                ENDCG
            }
        } 
    }
}
