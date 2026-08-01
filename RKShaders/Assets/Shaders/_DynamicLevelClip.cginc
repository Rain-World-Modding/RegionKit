#pragma multi_compile _ COMBINEDLEVEL
#pragma multi_compile _ RIPPLE

inline void dynamicLevelClip (float2 scrPos, float2 textCoord) {
#if COMBINEDLEVEL
#if RIPPLE
    if (tex2D(_DynamicLevelElements, scrPos).x==0 && tex2D(_GameplayRippleMask,scrPos).x==0.0) return;
#else
    if (tex2D(_DynamicLevelElements, scrPos).x==0) return;
#endif
    float3 orig = tex2D(_OrigLevelTex, textCoord).xyz;
    float3 level = tex2D(_LevelTex, textCoord).xyz;
    if (orig.x!=level.x || orig.y != level.y || orig.z != level.z) discard;
#endif
}
