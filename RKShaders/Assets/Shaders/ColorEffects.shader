// Created by Vigaro

Shader "RegionKit/ColorEffects" 
{
    Properties 
    {
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
    }
    
    Category 
    {
        Tags { }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha 
        Cull Off

        SubShader   
        {
            GrabPass
            {
                "_GrabPass"
            }
            Pass 
            {
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _GrabPass;

                struct v2f {
                    float4 pos        : SV_POSITION;
                    float2 uv         : TEXCOORD0;
                    float2 scrPos     : TEXCOORD1;
                    float4 clr        : COLOR;
                };

                v2f vert (appdata_full v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.texcoord;
                    o.scrPos = ComputeScreenPos(o.pos);
                    o.clr = v.color;
                    return o;
                }

                float3 applyHue(float3 aColor, float aHue)
                {
                    float angle = radians(aHue);
                    float3 k = float3(0.57735, 0.57735, 0.57735);
                    float cosAngle = cos(angle);
                    //Rodrigues' rotation formula
                    return aColor * cosAngle + cross(k, aColor) * sin(angle) + k * dot(k, aColor) * (1 - cosAngle);
                }
                 
                half4 frag (v2f i) : SV_Target
                {
                    float4 col = tex2D(_GrabPass, i.scrPos);
                    float4 texCol = tex2D(_MainTex, i.uv);

                    float _Hue = 360 * i.clr.r;
                    float _Brightness = i.clr.g * 2 - 1;
                    float _Contrast = i.clr.b * 2;
                    float _Saturation = i.clr.a * 2;

                    if (i.clr.r * texCol.a > 0) {
                        col.rgb = applyHue(col.rgb, _Hue);
                    }
                    if (i.clr.b * texCol.a > 0) {
                        col.rgb = (col.rgb - 0.5f) * (_Contrast) + 0.5f;
                    }
                    if (i.clr.g * texCol.a > 0) {
                        col.rgb = col.rgb + _Brightness;
                    }
                    if (i.clr.a * texCol.a > 0) {
                        float3 intensity = dot(col.rgb, float3(0.299,0.587,0.114));
                        col.rgb = lerp(intensity, col.rgb, _Saturation);
                    }
                 
                    return col;
                }
                ENDCG
            }
        } 
    }
}