sampler2D _SlopedTerrainMask;

half4 TerrainAtScrPos(float2 screenPos)
{    
    half4 result = tex2D(_SlopedTerrainMask, screenPos);
    result.r = 2 - result.r * 3;
    return result;
}

half4 TerrainAtLevelPos(float2 textCoord, float4 _spriteRect)
{
    float2 screenPos = lerp(_spriteRect.xy, _spriteRect.zw, textCoord);
    return TerrainAtScrPos(screenPos);
}

half4 AddTerrain(half4 levelCol, float2 textCoord, float4 _spriteRect)
{
    half levelDepth = (round(levelCol * 255) % 90 - 1) % 30;
    if (all(levelCol.rgb == 1))
        levelDepth = 30;
    
    half4 terrain = TerrainAtLevelPos(textCoord, _spriteRect);
    half terrainDepth = round(terrain.r * 30);

    if (levelDepth <= terrainDepth)
        return levelCol;
    else
        return half4((clamp(terrainDepth, 0, 29) + 31 + (terrain.g > 0.5) * 90) / 255.0, 0, 0, 1);
}

half4 SampleTerrainAndLevel(sampler2D _LevelTex, float2 textCoord, float4 _spriteRect)
{
    half4 levelCol = tex2D(_LevelTex, textCoord);
    return AddTerrain(levelCol, textCoord, _spriteRect);
}

half TerrainAndLevelDepthUnclamped(sampler2D _LevelTex, float2 textCoord, float4 _spriteRect)
{
    half4 levelCol = tex2D(_LevelTex, textCoord);
    half levelDepth = fmod(round(levelCol.r * 255) - 1, 30.0);
    
    half4 terrain = TerrainAtLevelPos(textCoord, _spriteRect);
    half terrainDepth = round(terrain.r * 30);
    
    if (all(levelCol.rgb == 1))
        return terrainDepth;
    

    return min(levelDepth, terrainDepth);
}

half TerrainAndLevelDepth(sampler2D _LevelTex, float2 textCoord, float4 _spriteRect)
{
    half4 levelCol = tex2D(_LevelTex, textCoord);
    half levelDepth = fmod(round(levelCol.r * 255) - 1, 30.0);
    
    if (all(levelCol.rgb == 1))
        levelDepth = 30;
    
    half4 terrain = TerrainAtLevelPos(textCoord, _spriteRect);
    half terrainDepth = round(terrain.r * 30);

    return min(levelDepth, terrainDepth);
}

// Discard pixels where the level, terrain curve, or objects exist
void BackgroundClip(sampler2D _LevelTex, sampler2D _GrabTexture, float2 textCoord, float2 scrPos)
{
    half4 levelCol = tex2D(_LevelTex, textCoord);
    
    // Clip when obscured by the level
    if (all(levelCol.rgb != 1))
        discard;
    
    // Clip when obscured by objects
    half4 c = tex2D(_GrabTexture, scrPos);
    if (c.x > 1.0 / 255.0 || c.y != 0.0 || c.z != 0.0)
        discard;
    
    // Clip when obscured by terrain curve
    half4 terrain = TerrainAtScrPos(scrPos);
    if (terrain.r < 1.0)
        discard;
}

// Like BackgroundClip, but does not clip if the level has 100% effect color
void BackgroundClipVanilla(sampler2D _LevelTex, sampler2D _GrabTexture, float2 textCoord, float2 scrPos)
{
    half4 levelCol = tex2D(_LevelTex, textCoord);
    
    // Clip when obscured by the level
    if (all(levelCol.rgb != 1))
        discard;
    
    // Clip when obscured by objects
    half4 c = tex2D(_GrabTexture, scrPos);
    if (c.x > 1.0 / 255.0 || c.y != 0.0 || c.z != 0.0)
        discard;
    
    // DON'T clip when effect color is full
    // OE uses this for some highlights on plants
    if (any(levelCol.rgb != 1))
        return;
    
    // Clip when obscured by terrain curve
    half4 terrain = TerrainAtScrPos(scrPos);
    if (terrain.r < 1.0)
        discard;
}
