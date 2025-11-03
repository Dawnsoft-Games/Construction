
HEADER
{
	Description = "Simple procedural fire using noise + vertical gradient";
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
	#define S_TRANSLUCENT 0
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
	
	float4 g_vcolordark < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.60, 0.02, 0.00, 1.00 ); >;
	float g_flone < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 1 ); >;
	float g_flnoisescale < UiGroup( ",0/,0/0" ); Default1( 4 ); Range1( 0, 100 ); >;
	float g_flspeed < UiGroup( ",0/,0/0" ); Default1( 0.35 ); Range1( 0, 10 ); >;
	float g_flturbulence < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 10 ); >;
	float g_flverticalstretch < UiGroup( ",0/,0/0" ); Default1( 1.5 ); Range1( 0, 10 ); >;
	float g_flheight < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 10 ); >;
	float g_flbaseoffset < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( -10, 10 ); >;
	float4 g_vcolormid < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.45, 0.00, 1.00 ); >;
	float4 g_vbubblecolor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.90, 0.50, 1.00 ); >;
	
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
		
		float4 l_0 = g_vcolordark;
		float l_1 = g_flone;
		float2 l_2 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_3 = g_flnoisescale;
		float2 l_4 = l_2 * float2( l_3, l_3 );
		float l_5 = g_flspeed;
		float l_6 = g_flTime * l_5;
		float2 l_7 = TileAndOffsetUv( l_4, float2( 1, 1 ), float2( l_6, l_6 ) );
		float l_8 = Simplex2D( l_7 );
		float l_9 = g_flturbulence;
		float l_10 = l_8 * l_9;
		float3 l_11 = i.vPositionOs;
		float l_12 = l_11.y;
		float l_13 = g_flverticalstretch;
		float l_14 = g_flheight;
		float l_15 = l_13 * l_14;
		float l_16 = l_12 * l_15;
		float l_17 = g_flbaseoffset;
		float l_18 = l_16 + l_17;
		float l_19 = l_10 + l_18;
		float l_20 = l_1 - l_19;
		float4 l_21 = l_0 * float4( l_20, l_20, l_20, l_20 );
		float4 l_22 = g_vcolormid;
		float4 l_23 = l_22 * float4( l_19, l_19, l_19, l_19 );
		float4 l_24 = l_21 + l_23;
		float4 l_25 = g_vbubblecolor;
		float4 l_26 = l_25 * float4( l_19, l_19, l_19, l_19 );
		
		m.Albedo = l_24.xyz;
		m.Emission = l_26.xyz;
		m.Opacity = 1;
		m.Roughness = 1;
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
