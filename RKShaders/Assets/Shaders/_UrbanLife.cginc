#pragma multi_compile _ URBANLIFE
sampler2D _UrbanShadowsBlurGrab;
float2 _UrbanLifeCamPos;
float UrbanLifeShadows(float2 scrPos, float depth){// TODO: some nasty hardcoding here vvv
    return step(.3,tex2D(_UrbanShadowsBlurGrab,((scrPos+_UrbanLifeCamPos*float2(0.0037,.0252))*float2(.2,.2))*float2(1,.25)+float2(0,.06+depth*.002)));
}
