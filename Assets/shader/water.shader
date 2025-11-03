
HEADER
{
	Description = "Water shader with parallax/height, normal-based waves, reflection, fresnel, foam and caustics. Lots of tweakable parameters.";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth();
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 0
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 1
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
	#if ( PROGRAM == VFX_PROGRAM_PS )
		bool vFrontFacing : SV_IsFrontFace;
	#endif
};

VS
{
	#include "common/vertex.hlsl"

	PixelInput MainVs( VertexInput v )
	{
		
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;
		
		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;
		
		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		return FinalizeVertex( i );
		
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	float4 g_vdeepcolor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.02, 0.04, 0.08, 1.00 ); >;
	float g_fluvtiling < UiGroup( ",0/,0/0" ); Default1( 4 ); Range1( 0.01, 1000 ); >;
	float g_flwavespeed < UiGroup( ",0/,0/0" ); Default1( 0.3 ); Range1( 0, 100 ); >;
	float g_flheightscale < UiGroup( ",0/,0/0" ); Default1( 0.12 ); Range1( 0, 50 ); >;
	float g_flnormalstrength < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 50 ); >;
	float4 g_vshallowcolor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.20, 0.35, 0.50, 1.00 ); >;
	float4 g_vreflectiontint < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flfresnelpower < UiGroup( ",0/,0/0" ); Default1( 0.6 ); Range1( 0, 10 ); >;
	float g_flreflectionintensity < UiGroup( ",0/,0/0" ); Default1( 0.6 ); Range1( 0, 10 ); >;
	float g_flparallaxstrength < UiGroup( ",0/,0/0" ); Default1( 0.12 ); Range1( 0, 20 ); >;
	float g_flcausticsstrength < UiGroup( ",0/,0/0" ); Default1( 0.2 ); Range1( 0, 50 ); >;
	float g_flopacity < UiGroup( ",0/,0/0" ); Default1( 0.6 ); Range1( 0, 10 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		
		Material m = Material::Init( i );
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float4 l_0 = g_vdeepcolor;
		float2 l_1 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_2 = g_fluvtiling;
		float2 l_3 = l_1 * float2( l_2, l_2 );
		float l_4 = g_flwavespeed;
		float l_5 = g_flTime * l_4;
		float2 l_6 = TileAndOffsetUv( l_3, float2( 1, 1 ), float2( l_5, l_5 ) );
		float l_7 = Simplex2D( l_6 );
		float l_8 = g_flheightscale;
		float l_9 = l_7 * l_8;
		float l_10 = ValueNoise( l_6 );
		float l_11 = l_9 + l_10;
		float l_12 = g_flnormalstrength;
		float l_13 = l_11 * l_12;
		float4 l_14 = l_0 * float4( l_13, l_13, l_13, l_13 );
		float4 l_15 = g_vshallowcolor;
		float l_16 = 0.0f;
		float4 l_17 = l_15 * float4( l_16, l_16, l_16, l_16 );
		float4 l_18 = l_14 + l_17;
		float4 l_19 = g_vreflectiontint;
		float3 l_20 = i.vPositionOs;
		float l_21 = l_20.z;
		float l_22 = l_21 - l_16;
		float l_23 = g_flfresnelpower;
		float l_24 = l_22 * l_23;
		float4 l_25 = l_19 * float4( l_24, l_24, l_24, l_24 );
		float l_26 = g_flreflectionintensity;
		float4 l_27 = l_25 * float4( l_26, l_26, l_26, l_26 );
		float4 l_28 = l_18 + l_27;
		float l_29 = g_flparallaxstrength;
		float l_30 = l_11 * l_29;
		float2 l_31 = l_3 + float2( l_30, l_30 );
		float l_32 = g_flcausticsstrength;
		float l_33 = l_10 * l_32;
		float2 l_34 = l_31 * float2( l_33, l_33 );
		float2 l_35 = l_34 * float2( 1, 1 );
		float l_36 = g_flopacity;
		float2 l_37 = l_35 * float2( l_36, l_36 );
		
		m.Albedo = l_28.xyz;
		m.Emission = float3( l_37, 0 );
		m.Opacity = l_36;
		m.Roughness = 0.2;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );
		
		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );
		
		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
		m.TextureCoords = i.vTextureCoords.xy;
				
		return ShadingModelStandard::Shade( m );
	}
}
