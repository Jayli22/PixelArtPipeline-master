// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Shader created with Shader Forge v1.16 
// Shader Forge (c) Neat Corporation / Joachim Holmer - http://www.acegikmo.com/shaderforge/
// Note: Manually altering this data may prevent you from opening it in Shader Forge
/*SF_DATA;ver:1.16;sub:START;pass:START;ps:flbk:,iptp:0,cusa:False,bamd:0,lico:1,lgpr:1,limd:0,spmd:1,trmd:0,grmd:0,uamb:True,mssp:True,bkdf:False,hqlp:False,rprd:False,enco:False,rmgx:True,rpth:0,hqsc:True,nrmq:1,nrsp:0,vomd:0,spxs:False,tesm:0,culm:0,bsrc:0,bdst:0,dpts:2,wrdp:False,dith:0,rfrpo:True,rfrpn:Refraction,ufog:True,aust:False,igpj:True,qofs:1,qpre:3,rntp:2,fgom:False,fgoc:False,fgod:False,fgor:False,fgmd:0,fgcr:0.5,fgcg:0.5,fgcb:0.5,fgca:1,fgde:0.01,fgrn:0,fgrf:300,ofsf:0,ofsu:0,f2p0:False;n:type:ShaderForge.SFN_Final,id:8119,x:34051,y:32837,varname:node_8119,prsc:2|custl-7107-OUT;n:type:ShaderForge.SFN_Tex2d,id:3421,x:33471,y:32893,ptovrint:False,ptlb:MainTex,ptin:_MainTex,varname:_MainTex,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,ntxv:0,isnm:False|UVIN-7207-UVOUT;n:type:ShaderForge.SFN_Multiply,id:7107,x:33815,y:33025,varname:node_7107,prsc:2|A-3421-RGB,B-5009-OUT,C-3421-A,D-1551-RGB,E-3266-OUT;n:type:ShaderForge.SFN_Vector1,id:5009,x:33457,y:33116,varname:node_5009,prsc:2,v1:0.5;n:type:ShaderForge.SFN_Color,id:1551,x:33445,y:33225,ptovrint:False,ptlb:MainColor,ptin:_MainColor,varname:_MainColor,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,c1:1,c2:1,c3:1,c4:1;n:type:ShaderForge.SFN_Fresnel,id:1989,x:33059,y:33420,varname:node_1989,prsc:2;n:type:ShaderForge.SFN_OneMinus,id:9067,x:33261,y:33420,varname:node_9067,prsc:2|IN-1989-OUT;n:type:ShaderForge.SFN_Slider,id:9701,x:33026,y:33598,ptovrint:False,ptlb:Falloff,ptin:_Falloff,varname:_Falloff,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:10;n:type:ShaderForge.SFN_Power,id:9527,x:33474,y:33420,varname:node_9527,prsc:2|VAL-9067-OUT,EXP-9701-OUT;n:type:ShaderForge.SFN_Panner,id:7207,x:33178,y:32888,varname:node_7207,prsc:2,spu:-0.3,spv:0;n:type:ShaderForge.SFN_VertexColor,id:4405,x:33474,y:33564,varname:node_4405,prsc:2;n:type:ShaderForge.SFN_Multiply,id:3266,x:33684,y:33420,varname:node_3266,prsc:2|A-9527-OUT,B-4405-A,C-7135-OUT;n:type:ShaderForge.SFN_Slider,id:7135,x:33360,y:33715,ptovrint:False,ptlb:power,ptin:_power,varname:node_7135,prsc:2,glob:False,taghide:False,taghdr:False,tagprd:False,tagnsco:False,tagnrm:False,min:0,cur:1,max:10;proporder:3421-1551-9701-7135;pass:END;sub:END;*/

Shader "Moba_EF/25_Add_Falloff_SpeedXY" {
    Properties {
        _MainTex ("MainTex", 2D) = "white" {}
        _MainColor ("MainColor", Color) = (1,1,1,1)
        _Falloff ("Falloff", Range(0, 10)) = 1
        _power ("power", Range(0, 10)) = 1
        _SpeedX("SpeedX", Range(-10, 10)) = -0.3
        _SpeedY("SpeedY", Range(-10, 10)) = -0.3
    }
    SubShader {
        Tags {
            "IgnoreProjector"="True"
            "Queue"="Transparent+10"
            "RenderType"="Transparent"
            "ForceNoShadowCasting"="True"
        }
        LOD 200
        Pass {
            Name "FORWARD"
            Tags {
                "LightMode"="ForwardBase"
            }
            Blend One One
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma exclude_renderers gles3 metal d3d11_9x xbox360 xboxone ps3 ps4 psp2 
            #pragma target 3.0
            uniform float4 _TimeEditor;
            uniform sampler2D _MainTex; uniform float4 _MainTex_ST;
            uniform float4 _MainColor;
            uniform float _Falloff;
            uniform float _power;
            uniform float _SpeedX;
            uniform float _SpeedY;
            struct VertexInput {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord0 : TEXCOORD0;
                float4 vertexColor : COLOR;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float3 normalDir : TEXCOORD2;
                float4 vertexColor : COLOR;
                UNITY_FOG_COORDS(3)
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv0 = v.texcoord0;
                o.vertexColor = v.vertexColor;
                o.normalDir = UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {
                i.normalDir = normalize(i.normalDir);
/////// Vectors:
                float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
                float3 normalDirection = i.normalDir;
////// Lighting:
                float4 node_5521 = _Time + _TimeEditor;
                float2 node_7207 = (i.uv0+node_5521.g*float2(_SpeedX,_SpeedY));
                float4 _MainTex_var = tex2D(_MainTex,TRANSFORM_TEX(node_7207, _MainTex));
                float3 finalColor = (_MainTex_var.rgb*0.5*_MainTex_var.a*_MainColor.rgb*(pow((1.0 - (1.0-max(0,dot(normalDirection, viewDirection)))),_Falloff)*i.vertexColor.a*_power));
                fixed4 finalRGBA = fixed4(finalColor,1);
                UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
                return finalRGBA;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
    CustomEditor "ShaderForgeMaterialInspector"
}
