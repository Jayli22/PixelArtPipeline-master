// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "E3D/SceneSP/E3D-Ring-MirrorGround"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		[HDR]_BaseColor("BaseColor", Color) = (0.5882353,0.5882353,0.5882353,1)
		_UOffset("U-Offset", Range( -2 , 2)) = 0
		_UTiling("U-Tiling", Range( 0 , 3)) = 1.752941
		_VOffset("V-Offset", Range( -2 , 2)) = 0
		[HideInInspector]_Ref("_Ref", 2D) = "white" {}
		_VTiling("V-Tiling", Range( 0 , 3)) = 0.6482049
		_Smooth("Smooth", Range( 0 , 1)) = 0
		_MaskMap("MaskMap", 2D) = "white" {}
		_RefMini("RefMini", Range( 0 , 1)) = 0
		_RefLight("RefLight", Range( 0 , 1)) = -0.1529412
		[HDR]_GlowColor("GlowColor", Color) = (0.5752595,0.9612784,0.9779412,1)
		_MirrorSmoothPower("Mirror-Smooth-Power", Range( 0 , 1)) = 0
		_Alpha("Alpha", Range( 0 , 1)) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" }
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityCG.cginc"
		#include "UnityShaderVariables.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		struct Input
		{
			float2 uv_texcoord;
			float3 worldNormal;
			float3 worldPos;
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _MaskMap;
		uniform float _VTiling;
		uniform float _UTiling;
		uniform float _UOffset;
		uniform float _VOffset;
		uniform float _Alpha;
		uniform float4 _BaseColor;
		uniform sampler2D _MainTex;
		uniform float4 _GlowColor;
		uniform float _Smooth;
		uniform sampler2D _Ref;
		uniform float _RefMini;
		uniform float _RefLight;
		uniform float _MirrorSmoothPower;

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			float2 temp_output_77_0 = ( float2( 1,1 ) * _VTiling );
			float2 temp_output_3_0 = (( temp_output_77_0 * -1.0 ) + (i.uv_texcoord - float2( 0,0 )) * (temp_output_77_0 - ( temp_output_77_0 * -1.0 )) / (float2( 1,1 ) - float2( 0,0 )));
			float2 break11 = temp_output_3_0;
			float temp_output_4_0 = length( temp_output_3_0 );
			float2 appendResult8 = (float2(( ( 1.0 - (0.0 + (atan2( break11.y , break11.x ) - ( ( _UTiling * UNITY_PI ) * -1.0 )) * (1.0 - 0.0) / (( _UTiling * UNITY_PI ) - ( ( _UTiling * UNITY_PI ) * -1.0 ))) ) - _UOffset ) , ( temp_output_4_0 - frac( _VOffset ) )));
			float4 tex2DNode112 = tex2D( _MaskMap, appendResult8 );
			SurfaceOutputStandard s138 = (SurfaceOutputStandard ) 0;
			s138.Albedo = ( _BaseColor * tex2D( _MainTex, appendResult8 ) ).rgb;
			float3 ase_worldNormal = i.worldNormal;
			s138.Normal = ase_worldNormal;
			s138.Emission = ( tex2DNode112.b * _GlowColor ).rgb;
			s138.Metallic = tex2DNode112.g;
			s138.Smoothness = ( tex2DNode112.r * tex2DNode112.a * _Smooth );
			s138.Occlusion = 1.0;

			data.light = gi.light;

			UnityGI gi138 = gi;
			#ifdef UNITY_PASS_FORWARDBASE
			Unity_GlossyEnvironmentData g138 = UnityGlossyEnvironmentSetup( s138.Smoothness, data.worldViewDir, s138.Normal, float3(0,0,0));
			gi138 = UnityGlobalIllumination( data, s138.Occlusion, s138.Normal, g138 );
			#endif

			float3 surfResult138 = LightingStandard ( s138, viewDir, gi138 ).rgb;
			surfResult138 += s138.Emission;

			#ifdef UNITY_PASS_FORWARDADD//138
			surfResult138 -= s138.Emission;
			#endif//138
			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 unityObjectToClipPos115 = UnityObjectToClipPos( ase_vertex3Pos );
			float4 computeScreenPos116 = ComputeScreenPos( unityObjectToClipPos115 );
			float4 tex2DNode124 = tex2D( _Ref, (( computeScreenPos116 / (computeScreenPos116).w )).xy );
			float4 temp_cast_3 = (_RefMini).xxxx;
			float4 color123 = IsGammaSpace() ? float4(1.2,1.2,1.2,1) : float4(1.493478,1.493478,1.493478,1);
			float3 desaturateInitialColor130 = (float4( 0,0,0,0 ) + (tex2DNode124 - temp_cast_3) * (color123 - float4( 0,0,0,0 )) / (float4( 1,1,1,0 ) - temp_cast_3)).rgb;
			float desaturateDot130 = dot( desaturateInitialColor130, float3( 0.299, 0.587, 0.114 ));
			float3 desaturateVar130 = lerp( desaturateInitialColor130, desaturateDot130.xxx, 1.0 );
			float lerpResult131 = lerp( 1.0 , tex2DNode112.r , _MirrorSmoothPower);
			float3 clampResult136 = clamp( ( desaturateVar130 * _RefLight * lerpResult131 * tex2DNode112.a ) , float3( 0,0,0 ) , float3( 1,1,1 ) );
			float4 lerpResult137 = lerp( float4( surfResult138 , 0.0 ) , tex2DNode124 , float4( clampResult136 , 0.0 ));
			c.rgb = lerpResult137.rgb;
			c.a = ( tex2DNode112.a * _Alpha );
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows nofog 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				o.worldNormal = worldNormal;
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				o.worldPos = worldPos;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = IN.worldPos;
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = IN.worldNormal;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16400
271;455;1183;582;1260.16;302.5054;3.947907;True;False
Node;AmplifyShaderEditor.CommentaryNode;110;-2273.471,-91.9949;Float;False;1230.022;897.8641;UV-Region;7;3;1;80;77;81;78;79;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector2Node;79;-2201.212,413.6116;Float;False;Constant;_Vector0;Vector 0;10;0;Create;True;0;0;False;0;1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;78;-2199.959,548.951;Float;False;Property;_VTiling;V-Tiling;7;0;Create;True;0;0;False;0;0.6482049;0.6482049;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-1862.243,395.99;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-1838.545,555.1652;Float;False;Constant;_Float3;Float 3;10;0;Create;True;0;0;False;0;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;111;-1001.218,-312.7004;Float;False;2035.394;1126.193;https://www.element3ds.com/forum.php?mod=forumdisplay&fid=104;18;28;13;27;29;30;19;18;17;37;5;39;21;4;22;20;10;11;8;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-1629.996,442.1539;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;1;-1927.12,105.5835;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;3;-1402.007,167.564;Float;True;5;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;1,1;False;3;FLOAT2;-1,-1;False;4;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;30;-922.8055,-193.7084;Float;False;Property;_UTiling;U-Tiling;4;0;Create;True;0;0;False;0;1.752941;0.65;0;3;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;113;-141.6309,861.1842;Float;False;1310.843;433.6547;Fl-UV-Cal;6;118;117;116;115;114;119;;1,1,1,1;0;0
Node;AmplifyShaderEditor.PiNode;27;-630.5526,-188.6585;Float;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-918.5526,-113.6585;Float;False;Constant;_Float0;Float 0;3;0;Create;True;0;0;False;0;-1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;114;-57.09786,962.373;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;11;-767.0494,-7.108442;Float;False;FLOAT2;1;0;FLOAT2;0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;28;-382.0785,-192.083;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ATan2OpNode;10;-494.0845,-30.36576;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.UnityObjToClipPosHlpNode;115;131.4013,961.9729;Float;False;1;0;FLOAT3;0,0,0;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;13;-219.9548,-36.64676;Float;True;5;0;FLOAT;0;False;1;FLOAT;-3.14;False;2;FLOAT;3.14;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-221.2653,347.5013;Float;False;Property;_VOffset;V-Offset;5;0;Create;True;0;0;False;0;0;0.94;-2;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComputeScreenPosHlpNode;116;323.0006,960.4732;Float;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FractNode;22;77.40958,352.3033;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LengthOpNode;4;-748.3925,277.8116;Float;True;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;117;536.5615,1072.998;Float;True;False;False;False;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;19;-12.31892,191.7819;Float;False;Property;_UOffset;U-Offset;3;0;Create;True;0;0;False;0;0;0.22;-2;2;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;17;95.66895,-36.16151;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;118;818.2822,960.6434;Float;False;2;0;FLOAT4;0,0,0,0;False;1;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;18;348.0241,-40.3693;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;21;288.6353,279.5612;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;120;1201,853.8956;Float;False;1640.318;732.6959;Ref-Power [https://www.element3ds.com/forum.php?mod=forumdisplay&fid=104];13;137;136;134;132;131;130;129;127;126;125;124;123;121;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ComponentMaskNode;119;906.0931,1288.785;Float;False;True;True;False;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;8;775.2718,165.6061;Float;True;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;123;1288.338,1379.311;Float;False;Constant;_Color0;Color 0;7;1;[HDR];Create;True;0;0;False;0;1.2,1.2,1.2,1;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;112;1126.602,295.3611;Float;True;Property;_MaskMap;MaskMap;9;0;Create;True;0;0;False;0;None;0c96599e0e0e0394698a022289f9c05f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;122;1702.675,697.6256;Float;False;179.9999;134;FinalSmooth;1;128;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;121;1260.349,1240.759;Float;False;Property;_RefMini;RefMini;10;0;Create;True;0;0;False;0;0;0.669;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;124;1308.521,980.779;Float;True;Property;_Ref;_Ref;6;1;[HideInInspector];Create;True;0;0;False;0;None;;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;129;1695.634,1225.982;Float;False;5;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;1,1,1,0;False;3;COLOR;0,0,0,0;False;4;COLOR;1,1,1,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;126;1733.706,935.3049;Float;False;Constant;_F1;F1;9;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;149;1138.801,225.1015;Float;False;Property;_Smooth;Smooth;8;0;Create;True;0;0;False;0;0;0.686;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;128;1739.546,747.6794;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;6;1149.65,18.37041;Float;True;Property;_MainTex;MainTex;1;0;Create;True;0;0;False;0;None;66fd2a693732e214da2c34738631cf6c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;142;1176.334,537.7733;Float;False;Property;_GlowColor;GlowColor;12;1;[HDR];Create;True;0;0;False;0;0.5752595,0.9612784,0.9779412,1;0.2499999,0.2499999,0.2499999,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;140;1214.92,-149.1027;Float;False;Property;_BaseColor;BaseColor;2;1;[HDR];Create;True;0;0;False;0;0.5882353,0.5882353,0.5882353,1;0.5367647,0.5367647,0.5367647,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;125;1735.532,1140.718;Float;False;Constant;_Sat1;Sat1;10;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;127;1732.401,1048.125;Float;False;Property;_MirrorSmoothPower;Mirror-Smooth-Power;13;0;Create;True;0;0;False;0;0;0.54;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;131;2122.556,931.9142;Float;False;3;0;FLOAT;1;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;148;1615.446,250.6566;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;1517.92,-56.10266;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;143;1598.903,434.817;Float;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DesaturateOpNode;130;2078.862,1124.873;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;132;2007.716,1236.577;Float;False;Property;_RefLight;RefLight;11;0;Create;True;0;0;False;0;-0.1529412;0.252;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;147;1490.695,575.5139;Float;False;Property;_Alpha;Alpha;14;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;133;2268.381,702.8924;Float;False;179.9999;134;FinalImage;1;135;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CustomStandardSurface;138;1913.336,258.5752;Float;False;Metallic;Tangent;6;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,1;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;134;2376.848,1065.25;Float;False;4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;146;1815.695,538.5139;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;135;2300.125,754.9082;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;136;2528.895,1051.167;Float;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;1,1,1;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;137;2688.84,925.923;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FractNode;37;297.6898,518.3203;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RelayNode;144;2283.309,546.7622;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;5;-499.4536,418.9444;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;39;-5.708482,495.7806;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;107;3116.747,579.1046;Float;False;True;2;Float;ASEMaterialInspector;0;0;CustomLighting;E3D/SceneSP/E3D-Ring-MirrorGround;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;77;0;79;0
WireConnection;77;1;78;0
WireConnection;80;0;77;0
WireConnection;80;1;81;0
WireConnection;3;0;1;0
WireConnection;3;3;80;0
WireConnection;3;4;77;0
WireConnection;27;0;30;0
WireConnection;11;0;3;0
WireConnection;28;0;27;0
WireConnection;28;1;29;0
WireConnection;10;0;11;1
WireConnection;10;1;11;0
WireConnection;115;0;114;0
WireConnection;13;0;10;0
WireConnection;13;1;28;0
WireConnection;13;2;27;0
WireConnection;116;0;115;0
WireConnection;22;0;20;0
WireConnection;4;0;3;0
WireConnection;117;0;116;0
WireConnection;17;0;13;0
WireConnection;118;0;116;0
WireConnection;118;1;117;0
WireConnection;18;0;17;0
WireConnection;18;1;19;0
WireConnection;21;0;4;0
WireConnection;21;1;22;0
WireConnection;119;0;118;0
WireConnection;8;0;18;0
WireConnection;8;1;21;0
WireConnection;112;1;8;0
WireConnection;124;1;119;0
WireConnection;129;0;124;0
WireConnection;129;1;121;0
WireConnection;129;4;123;0
WireConnection;128;0;112;1
WireConnection;6;1;8;0
WireConnection;131;0;126;0
WireConnection;131;1;128;0
WireConnection;131;2;127;0
WireConnection;148;0;112;1
WireConnection;148;1;112;4
WireConnection;148;2;149;0
WireConnection;139;0;140;0
WireConnection;139;1;6;0
WireConnection;143;0;112;3
WireConnection;143;1;142;0
WireConnection;130;0;129;0
WireConnection;130;1;125;0
WireConnection;138;0;139;0
WireConnection;138;2;143;0
WireConnection;138;3;112;2
WireConnection;138;4;148;0
WireConnection;134;0;130;0
WireConnection;134;1;132;0
WireConnection;134;2;131;0
WireConnection;134;3;112;4
WireConnection;146;0;112;4
WireConnection;146;1;147;0
WireConnection;135;0;138;0
WireConnection;136;0;134;0
WireConnection;137;0;135;0
WireConnection;137;1;124;0
WireConnection;137;2;136;0
WireConnection;37;0;39;0
WireConnection;144;0;146;0
WireConnection;5;0;4;0
WireConnection;39;0;5;0
WireConnection;107;9;144;0
WireConnection;107;13;137;0
ASEEND*/
//CHKSM=9B76CA82ACE1C22E785452190D5C91A92B3AD22C