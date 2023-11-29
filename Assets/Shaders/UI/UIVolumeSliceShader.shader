// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/Volume"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _MinMaxVal ("Min Max Value", Vector) = (0, 1, 0, 0)
        _VisibleRegionsCount ("Visible Source Regions", Integer) = 0



        [MaterialToggle] _FilterSourceRegions ("Filter Source Regions", Float) = 0

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Texture2D  _MainTex;
            SamplerState my_point_clamp_sampler;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _MinMaxVal;
            float _FilterSourceRegions;
            int _VisibleRegionsCount;
            uniform StructuredBuffer<int4> sourceRegionsBuffer : register(t1);
            uniform StructuredBuffer<int> visibleSourceRegionsBuffer : register(t2);

            float invLerp(float from, float to, float value) {
                return (value - from) / (to - from);
            }

            float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value){
                float rel = invLerp(origFrom, origTo, value);
                return lerp(targetFrom, targetTo, rel);
            }

            float3 spectral_jet(float x)
            {
                float3 c;
                if (x < 0.25)
                    c = float3(0.0, 4.0 * x, 1.0);
                else if (x < 0.5)
                    c = float3(0.0, 1.0, 1.0 + 4.0 * (0.25 - x));
                else if (x < 0.75)
                    c = float3(4.0 * (x - 0.5), 1.0, 0.0);
                else
                    c = float3(1.0, 1.0 + 4.0 * (0.75 - x), 0.0);

                // Clamp colour components in [0,1]
                return saturate(c);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
        
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            float4 frag(v2f IN) : SV_Target
            {
                
                float voxelVal = _MainTex.Sample(my_point_clamp_sampler, IN.texcoord).r;
                float scaledVal = remap(_MinMaxVal.x, _MinMaxVal.y, 0.0, 1.0, voxelVal);
                float stretchedVal = remap(_MinMaxVal.z, _MinMaxVal.w, 0.0, 1.0, scaledVal);
                
                
                float4 color = float4(0, 0, 0, 1);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif
                color.rgb = spectral_jet(stretchedVal); //remap to 0 - 1
                color.rgb = lerp(float3(0,0,0), color.rgb, stretchedVal);
                //color.rgb = saturate(color.rgb);
                //color.rgb = saturate(float3(voxelVal / _MinMaxVal.y, voxelVal / _MinMaxVal.y, voxelVal / _MinMaxVal.y));

                int2 pos = int2(IN.texcoord * _MainTex_TexelSize.zw);
                if(_FilterSourceRegions != 0)
                {
                    bool anyFound = false;
                    for (int i = 0; i < _VisibleRegionsCount; i++)
                    {
                        int4 bounds = sourceRegionsBuffer[visibleSourceRegionsBuffer[i]];
                        if(!(pos.x < bounds.x || pos.y < bounds.y || pos.x >= bounds.z || pos.y >= bounds.w))
                        {
                            anyFound = true;
                        }
                    }
                    if(!anyFound)
                    {
                        color = float4(0, 0, 0, 1);
                    }
                }
                return color;
            }
        ENDCG
        }
    }
}