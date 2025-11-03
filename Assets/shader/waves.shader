
HEADER
{
	Description = "";
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
	
	float4 g_vcolor2 < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 0.60, 0.05, 0.00, 1.00 ); >;
	float g_fledgepositionb < UiGroup( ",0/,0/0" ); Default1( 14.828644 ); Range1( -100, 100 ); >;
	float g_flheight < UiGroup( ",0/,0/0" ); Default1( -30.857643 ); Range1( -100, 100 ); >;
	float g_flnoisewidth < UiGroup( ",0/,0/0" ); Default1( 5 ); Range1( 0, 10 ); >;
	float g_fledgeintensityb < UiGroup( ",0/,0/0" ); Default1( 1.8 ); Range1( 0, 100 ); >;
	float4 g_vcolor1 < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.45, 0.00, 1.00 ); >;
	float g_fledgepositiona < UiGroup( ",0/,0/0" ); Default1( 10.634445 ); Range1( -100, 100 ); >;
	float g_fledgeintensity < UiGroup( ",0/,0/0" ); Default1( 1.2 ); Range1( 0, 100 ); >;
	float4 g_vbubblecolor < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 0.60, 0.10, 1.00 ); >;
	
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
		
		float4 l_0 = g_vcolor2;
		float2 l_1 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_2 = l_1.y;
		float l_3 = g_fledgepositionb;
		float l_4 = g_flheight;
		float l_5 = l_3 + l_4;
		float l_6 = l_2 + l_5;
		float2 l_7 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 l_8 = l_7 * float2( 6, 6 );
		float l_9 = ValueNoise( l_8 );
		float2 l_10 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 l_11 = float2( 2.9234703, 2.9234703 ) * l_10;
		float l_12 = 0.5245932 * g_flTime;
		float2 l_13 = TileAndOffsetUv( l_11, float2( 1, 1 ), float2( l_12, l_12 ) );
		float l_14 = Simplex2D( l_13 );
		float l_15 = l_9 + l_14;
		float l_16 = g_flnoisewidth;
		float l_17 = l_15 * l_16;
		float l_18 = step( l_6, l_17 );
		float4 l_19 = l_0 * float4( l_18, l_18, l_18, l_18 );
		float l_20 = g_fledgeintensityb;
		float4 l_21 = l_19 * float4( l_20, l_20, l_20, l_20 );
		float4 l_22 = g_vcolor1;
		float2 l_23 = i.vTextureCoords.xy * float2( 1, 1 );
		float l_24 = l_23.y;
		float l_25 = g_fledgepositiona;
		float l_26 = l_4 + l_25;
		float l_27 = l_24 + l_26;
		float2 l_28 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 l_29 = l_28 * float2( 5.5169473, 5.5169473 );
		float l_30 = ValueNoise( l_29 );
		float l_31 = l_14 + l_30;
		float l_32 = l_16 * l_31;
		float l_33 = step( l_27, l_32 );
		float4 l_34 = l_22 * float4( l_33, l_33, l_33, l_33 );
		float l_35 = g_fledgeintensity;
		float4 l_36 = l_34 * float4( l_35, l_35, l_35, l_35 );
		float4 l_37 = l_21 + l_36;
		float2 l_38 = i.vTextureCoords.xy * float2( 1, 1 );
		float2 l_39 = l_38 / float2( 10000, 10000 );
		float2 l_40 = float2( 12, 12 ) * l_39;
		float l_41 = l_40.x;
		float l_42 = 0.3 * g_flTime;
		float l_43 = l_42 * -1;
		float2 l_44 = TileAndOffsetUv( l_40, float2( 1, 1 ), float2( l_43, l_43 ) );
		float l_45 = l_44.y;
		float2 l_46 = float2( l_41, l_45);
		float l_47 = g_flTime * 1.6;
		float l_48 = sin( l_47 );
		float l_49 = l_48 + 1.4;
		float l_50 = l_49 * 1;
		float l_51 = l_50 + 1;
		float l_52 = VoronoiNoise( l_46, l_51, 20.184029 );
		float l_53 = step( l_52, 0.13206601 );
		float l_54 = step( l_52, 0.12 );
		float l_55 = l_53 - l_54;
		float l_56 = l_55 * l_18;
		float4 l_57 = g_vbubblecolor;
		float4 l_58 = float4( l_56, l_56, l_56, l_56 ) * l_57;
		
		m.Albedo = l_37.xyz;
		m.Emission = l_58.xyz;
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
