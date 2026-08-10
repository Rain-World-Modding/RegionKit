// Original shader by Vigaro, adapted from HoloGrid
// Remade by Alduris to use extra UV channels instead of a uniform

Shader "RegionKit/HKHoloGrid"
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
                #include "_Functions.cginc"
                #include "_TerrainMask.cginc"
                #include "_Snow.cginc"
                #include "_BrainMoldClip.cginc"

                float4 _MainTex_ST;
                sampler2D _MainTex;
                sampler2D _LevelTex;
                sampler2D _NoiseTex2;

                sampler2D _GrabTexture;
                sampler2D _PreLevelColorGrab;
                
                uniform float4 _spriteRect;
                uniform float2 _screenSize;
                uniform float _RAIN;
                uniform float _hologramThreshold;

                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 scrPos : TEXCOORD1;
                    float2 textCoord : TEXCOORD2;
                    half4 hkColor : TEXCOORD3;
                    float4 clr : COLOR;
                };

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.textCoord = iLerp(_spriteRect.xy,_spriteRect.zw,o.scrPos);
                    o.hkColor = half4(v.texcoord1.xy, v.texcoord2.xy);
                    o.clr = v.color;
                    return o;
                }

                
                half DepthAtTextCoord(half2 textCoord, half2 scrPos)
                {
                    bool creatures = tex2D(_PreLevelColorGrab, scrPos.xy) != 0;
                    half4 levelTex = tex2D(_LevelTex, textCoord);
                    levelTex = AddTerrain(levelTex, textCoord, _spriteRect);
                    levelTex = AddSnow(levelTex, textCoord, scrPos);
                    
                    half grad = fmod(round(levelTex.x * 255) - 1, 30.0) / 30.0;
                    
                    #if RoomHasBrainMold
                    grad = lerp(grad,.0,_BrainMoldMask(scrPos));
                    #endif

                    if (levelTex.x == 1.0 && levelTex.y == 1.0 && levelTex.z == 1.0)
                    {
                        grad = 1.0;
                    }
                    if (grad > 6.0 / 30.0 && creatures)
                    {
                        grad = 6.0 / 30.0;
                    }
    
                    return grad;
                }

                half4 frag (v2f i) : SV_Target
                {
                    float2 textCoord = i.textCoord;
                    float light = 0;

                    float centerDist = clamp(distance(half2(0.5, 0.5), i.uv)*2 + (1.0-i.clr.w), 0, 1);

                    half dpth = DepthAtTextCoord(textCoord, i.scrPos);
                    if(dpth >= 0.999) return half4(0,0,0,0);

                    i.uv += ((textCoord - half2(0.5, 0.66))*0.4 + (i.uv - half2(0.5, 0.5))*0.4) * dpth;

                    if(dpth > 0.03 && (floor(i.uv.x * 300) % 20 == 10 || floor(i.uv.y * 300) % 20 == 10))
                    light = 0.35;


                    i.scrPos -= normalize(i.uv - half2(i.clr.x, i.clr.y))* lerp(-0.2,(0.2-dpth)*centerDist/ 40.0, i.clr.z);
                    textCoord -= normalize(i.uv - half2(i.clr.x, i.clr.y))*lerp(-0.2,(0.2-dpth)*centerDist/ 40.0, i.clr.z);

                    dpth = DepthAtTextCoord(textCoord, i.scrPos);


                    if((dpth > 3.0/30.0 && dpth < 7.0/30.0) || (dpth > 14.0/30.0 && dpth < 17.0/30.0)|| (dpth > 22.0/30.0 && dpth < 24.0/30.0)){
                        // thanks joar for this fucking mess of a check
	                    if(DepthAtTextCoord(textCoord + half2(-1.0/1400, 0), i.scrPos+ half2(-1.0/_screenSize.x, 0)) > dpth
	                     ||DepthAtTextCoord(textCoord + half2(1.0/1400, 0), i.scrPos+ half2(1.0/_screenSize.x, 0)) > dpth
	                     ||DepthAtTextCoord(textCoord + half2(0, 1.0/800), i.scrPos+ half2(0, 1.0/_screenSize.y)) > dpth
	                     ||DepthAtTextCoord(textCoord + half2(0, -1.0/800), i.scrPos+ half2(0, -1.0/_screenSize.y)) > dpth)
	                     light = dpth < 7.0/30.0 ? 1 : 0.35;
	                     centerDist = pow(centerDist, 2);
                    }

                    half h = tex2D(_NoiseTex2, half2(textCoord.x*4, textCoord.y*8 - _RAIN*10)).x*2;
                    h -= pow(i.clr.w, 2) * (1.0-pow(centerDist, 2));
                    if(fmod( round((textCoord.y - _RAIN*0.15) * 400) , 3 ) == 0)
                    h += lerp(0.6, 0.15, light);

                    if(h > _hologramThreshold*i.clr.w)
                    return half4(0,0,0,0);

                    light *= 1.0-centerDist;

                    return half4(i.hkColor.xyz,light*0.6);
                }
                ENDCG
            }
        } 
    }
}
