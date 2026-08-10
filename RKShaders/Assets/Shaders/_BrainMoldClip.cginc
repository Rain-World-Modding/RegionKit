#pragma multi_compile _ RoomHasBrainMold
sampler2D _BrainMold;
sampler2D _PreBrainMold;

inline float _BrainMoldMask( float2 scrPos )
{
    float brainMoldMask = 0;
#if RoomHasBrainMold
    brainMoldMask = tex2D(_PreBrainMold,scrPos)!=tex2D(_BrainMold,scrPos);
#endif
    return brainMoldMask;
}

inline fixed4 _BrainMoldTexture( float2 scrPos )
{
    fixed4 brainMold = 0;
#if RoomHasBrainMold
    fixed4 preBrainMold =tex2D(_PreBrainMold,scrPos); 
    brainMold = tex2D(_BrainMold,scrPos);
    brainMold.a = (preBrainMold!=brainMold);
#endif
    return brainMold;
}

inline void _BrainMoldClip( float2 scrPos )
{
#if RoomHasBrainMold
    bool brainMold = _BrainMoldMask( scrPos )>0;
    if (brainMold) discard;
#endif
}

