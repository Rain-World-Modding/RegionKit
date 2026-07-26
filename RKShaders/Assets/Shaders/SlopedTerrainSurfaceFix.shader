Shader "Futile/SlopedTerrainSurface"
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
        Cull Off
        ZTest Always // this is the fix

        SubShader   
        {
            Pass
            {
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
#include "TerrainCommon.cginc"

//#pragma profileoption NumTemps=64
//#pragma profileoption NumInstructionSlots=2048

// Textures
sampler2D _UniNoise;
sampler2D _SandNormals;
sampler2D _SlopedTerrainMask;

float _fogAmount;
float2 _screenOffset;

float4 _lightSourceColors[16];
float4 _lightSourceParams[16]; // (x, y, radius, unused)

struct v2f {
    float4 pos : SV_POSITION;
    float3 normal : TEXCOORD0;
    float3 tangent : TEXCOORD1;
    noperspective float4 scrPos : TEXCOORD2;
    float4 clr : COLOR;
};

v2f vert (appdata_full v)
{
    v2f o;
    o.pos = UnityObjectToClipPos (v.vertex);

    // Apply camera angle
    // This is in the shader so perspective correction applies to terrain depth
    o.pos.z = 0.0;
    //o.pos.w = (10.0 - (v.color.a * 30.0 - 6.0) * -0.025) * 0.1;
    o.pos.w = 0.985 + v.color.a * 0.075;
    o.pos.xy += (lerp(_spriteRect.xy, _spriteRect.zw, float2(0.5, 2.0 / 3.0)) * 2.0 - 1.0) * float2(1.0, -1.0) * (o.pos.w - 1.0);

    o.normal.xy = v.texcoord;
    o.normal.z = sqrt(1.0 - o.normal.x * o.normal.x - o.normal.y * o.normal.y);
    o.tangent = float3(v.color.gb, 0.0);
    o.scrPos = ComputeScreenPos(o.pos / o.pos.w);
    o.clr = v.color;
    return o;
}

// A modified version of slerp that rotates upwards towards (0,0,1)
float3 RotateTowardsZ(float3 normal, float3 tangent, float t)
{
    if(t >= 1.0) return float3(0,0,1);

    float angle = acos(normal.z);
    angle = normal.y < 0.0 ? (6.2831853 - angle) : angle;
    angle *= 1.0 - t;

    float s = sin(angle);
    float c = cos(angle);
    float3 up = cross(float3(0,0,1), tangent);

    return float3(0,0,c) + up * s;
}

float3 ComputeNormal(float3 inNormal, float3 inTangent, float2 uv, float z, float2 noise, float2 rippleParams, out float3 meshNormal)
{
    // Compute normal and tangents
    float3 normal = normalize(inNormal);
    float3 tangent = normalize(inTangent);

    float2 normalMapOffset = 0.08 * float2(
        tex2D(_NoiseTex, uv * 0.2).r * 2.0 - 1.0,
        tex2D(_NoiseTex, uv * 0.2 + float2(0.5, 0.0)).r * 2.0 - 1.0);
    
    // Flatten normals when drawing front of terrain, and draw curved edge
    if (z < 0.0)
    {
        float t = min(z / -TERRAIN_EDGE_RADIUS, 1.0);
        tangent = normalize(lerp(tangent, float3(1.0, 0.0, 0.0), t));
        normal = RotateTowardsZ(normal, tangent, t);
    }

    meshNormal = normal;

    float3 bitangent = cross(normal, tangent);

    float3 normalMap = tex2D(_SandNormals, uv + normalMapOffset).rgb * 2.0 - 1.0;
    normalMap = normalize(lerp(float3(0,0,1), normalMap, TERRAIN_WAVES));

    // Normal vector after map is applied, but before any grain
    float3 baseNormal = normal * normalMap.z
        + tangent * normalMap.y
        + bitangent * normalMap.x;
        
    float grainFactor = saturate(dot(normalize(float2(-_lightDirAndPixelSize.x, _lightDirAndPixelSize.y)), baseNormal.xy) * (1.0 - abs(baseNormal.z) * 0.5));
    grainFactor = pow(max((grainFactor - 1.0) / (1.0 - TERRAIN_LIGHT_FACTOR) + 1.0, 0.0), 0.2) * 0.8 + 0.2;

    normalMap.x += pow(noise.x * 2.0 - 1.0, 3) * TERRAIN_GRAIN * grainFactor * 0.5;
    normalMap.y += pow(noise.y * 2.0 - 1.0, 3) * TERRAIN_GRAIN * grainFactor * 0.5;
    normalMap = normalize(normalMap);

    // Apply normal map
    normal = normal * normalMap.z
        + tangent * normalMap.y
        + bitangent * normalMap.x;

    return normal;
}

half4 frag (v2f i) : SV_Target
{
    float2 levelUv = ScreenToLevelTex(i.scrPos);
    half4 texcol = tex2D(_LevelTex, levelUv);
    float3 lightDir = normalize(float3(-_lightDirAndPixelSize.x, _lightDirAndPixelSize.y, 2.0));
    float4 rippleParams = GetRippleParams(i.scrPos);
    float ripplePaletteAmount = 1 - (1 - rippleParams.x) * (1 - rippleParams.y);

    float2 worldPos = _camInRoomRect.xy / _camInRoomRect.zw * _screenSize + i.scrPos * _screenSize;
    half dpth = i.clr.w;

    // Round out depth of edges
    //dpth -= sqrt(1 - saturate(i.clr.r / 20.0 + 1)) * 0.1;
    
    float2 uv = float2(worldPos.x, dpth * 120.0 + 1.0 * min(i.clr.r, 0.0)) / 200.0;
    uv.y += uv.x * 0.2;

    half noise = tex2Dlod(_UniNoise, float4(levelUv.xy * 40.0, 0.0, 0.0)).r;
    half noise2 = tex2Dlod(_UniNoise, float4(levelUv.xy * 30.0 + float2(0.5, 0.5), 0.0, 0.0)).r;
    float fadeFactor = saturate(1.0 - i.clr.r / -TERRAIN_EDGE_RADIUS);

    float3 meshNormal;
    float3 normal = ComputeNormal(i.normal, i.tangent, uv, i.clr, float2(noise, noise2), rippleParams, meshNormal);

    // Cut out around terrain and objects
    half terrainDpth = (((uint)round(texcol.x * 255.0) - 1u) % 30u) / 30.0;
    if(texcol.x == 1 && texcol.y == 1 && texcol.z == 1)
    {
        terrainDpth = 10;
    }

    if(terrainDpth > 6.0/30.0)
    {
        float4 grabTexCol = tex2Dproj(_GrabTexture, i.scrPos);
        if (grabTexCol.x > 1.0/255.0 || grabTexCol.y != 0.0 || grabTexCol.z != 0.0)
        {
            terrainDpth = 6.0/30.0;
        }
    }

    if(dpth > terrainDpth)
    {
        discard;
    }
    
    half2 screenPos = half2(lerp(_spriteRect.x+_screenOffset.x, _spriteRect.z+_screenOffset.x, levelUv.x), lerp(_spriteRect.y+_screenOffset.y, _spriteRect.w+_screenOffset.y, levelUv.y));
    #if UNITY_UV_STARTS_AT_TOP
        screenPos.y = 1 - screenPos.y;
    #endif

    float pixelsAboveWater = (_waterLevel - screenPos.y) * _screenSize.y + 100;
    float wetness = saturate((50 - pixelsAboveWater) / 100);

    // Sunlight
    float sunlight = max(dot(lightDir, normal), 0.0);
    sunlight = sqrt(max((sunlight - 1.0) / (1.0 - TERRAIN_LIGHT_FACTOR) + 1.0, 0.0));

    // Recreate shadow casting logic, but with the level editor lightmap
    sunlight *= tex2D(_SlopedTerrainMask, i.scrPos).g;
    sunlight *= ObjectShadows(levelUv, screenPos, dpth * 30.0 - 1.0);

    sunlight *= pow(fadeFactor, 3.0);
    sunlight *= 1 - wetness;

    // Ambient light
    float ambientLight = (max(dot(normal, meshNormal), 0.0) * (0.5 * TERRAIN_WAVES + 1.0) + max(normal.z * normal.z, 0.0) * 0.25);
    ambientLight *= fadeFactor;
    
    float lightness = sunlight * 0.75 + ambientLight * 0.25;

    half4 sunColor = PaletteSurfaceColor(lightness, dpth, 1, rippleParams);
    half4 ambientColor = PaletteSurfaceColor(lightness, dpth, 0, rippleParams);
    
    // Specular glitter
    float3 viewDir = normalize(float3((i.scrPos - float2(0.5, 0.5)) * _ScreenParams.xy, -2000.0));
    float specular = pow(saturate(dot(lightDir, reflect(viewDir, normal))), 7.0);
    half4 skyColor = LevelPalette(0.5 / 32.0, 7.5 / 8.0, rippleParams);
    half4 fogColor = LevelPalette(1.5 / 32.0, 7.5 / 8.0, rippleParams);
    half4 sandColor = lerp(ambientColor, sunColor, sunlight);

    // Simulate light filtering through water
    sandColor = lerp(
        sandColor,
        LevelPalette(5.5 / 32.0, 7.5 / 8.0, rippleParams),
        clamp(-pixelsAboveWater / 10, 0, 1) * clamp(i.clr.r, 0, 0.1));

    sandColor.rgb += PaletteGlitterColor(rippleParams)
        * fadeFactor
        * saturate(noise2 * noise * 2.0 - 1.0)
        * lerp(TERRAIN_GRAIN, 0.5, ripplePaletteAmount)
        * (specular * sunlight + ambientLight * 0.05 + ripplePaletteAmount * 0.05);
        
    sandColor = lerp(sandColor, fogColor, saturate(i.clr.r) * _fogAmount);
    sandColor = lerp(sandColor, skyColor, saturate(i.clr.r * 3 - 2) * TERRAIN_SKY_FADE);

    // Apply black goo and fade
    float4 darkGoo = LevelPalette(0.5 / 32.0, 0.5 / 8.0, rippleParams);
    float4 lightGoo = LevelPalette(0.5 / 32.0, 1.5 / 8.0, rippleParams);
    float gooLevel = tex2D(_NoiseTex, float2(worldPos.x / 3000.0, 0.0)).r * 25.0 + TERRAIN_GOO_HEIGHT;

    if (-i.clr.r - gooLevel - tex2D(_NoiseTex, worldPos.xx / float2(120.0, 200.0)).r * 10.0 > 0.0)
        return darkGoo;

    if (-i.clr.r - gooLevel + 6.0 - tex2D(_NoiseTex, worldPos.xx / float2(150.0, 310.0) + float2(0.5, 0.5)).r * 14.0 > 0.0)
        return lightGoo;

    if (i.clr.r < 0.0)
    {
        //sandColor = lerp(tex2D(_terrainPalette, float2(0.5, 0.5 / 3.0)), sandColor, min(lerp(1.0, lightness * 10.0, min(1.0, i.clr.r / -TERRAIN_EDGE_RADIUS)), 1.0));
        sandColor = lerp(sandColor, lightGoo, pow(-i.clr.r / gooLevel, 1.5) * 0.75);
    }

    // Apply point light sources
    float3 lightTint = PaletteLightTint(rippleParams).rgb;
    float3 addedLight = 0.0;
    for(int j = 0; j < 16 && _lightSourceParams[j].z > 0.0; j++)
    {
        float3 incidentDir = float3(worldPos, dpth) - float3(_lightSourceParams[j].xy, 7.0);
        float dist = length(incidentDir);
        incidentDir /= dist;

        float flat = 0.8;
        float diffuse = 0.8 * max(dot(-incidentDir, normal), 0.0);
        float specular = min(pow(saturate(dot(-incidentDir, reflect(viewDir, normal))), 20.0) * 100.0, 10.0)
            * lerp(0.2, saturate(noise2 * noise * 2.0 - 1.0), TERRAIN_GRAIN);
        //specular = min(specular, 1.0);
        
        
        // Apply falloff
        half c = pow(1 - min(pow(dist / _lightSourceParams[j].z, 2), 1.0), 3.5);
        float amount = (flat + diffuse) * max(c, 0.0)
            + specular / dist;

        amount *= saturate(1.0 + i.clr.r / TERRAIN_EDGE_RADIUS * 2.0);
        addedLight.rgb += _lightSourceColors[j].rgb * _lightSourceColors[j].a * amount;
    }
    sandColor.rgb += addedLight * lightTint * saturate(1 - i.clr.r);
    ApplyShiftWaveColor(sandColor,rippleParams.w,rippleParams.z);
    return sandColor;
}
ENDCG
            }
        } 
    }
}
