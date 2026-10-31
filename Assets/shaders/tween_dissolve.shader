// SboxTween demo: a lit surface with glow and dissolve driven by Renderer.Attributes (Glow, Dissolve, GlowColor, BaseColor)
FEATURES
{
    #include "common/features.hlsl"
}

MODES
{
    Forward();
    Depth();
}

COMMON
{
    #include "common/shared.hlsl"
}

struct VertexInput
{
    #include "common/vertexinput.hlsl"
};

struct PixelInput
{
    #include "common/pixelinput.hlsl"
};

VS
{
    #include "common/vertex.hlsl"

    PixelInput MainVs( VertexInput i )
    {
        PixelInput o = ProcessVertex( i );
        return FinalizeVertex( o );
    }
}

PS
{
    #include "common/pixel.hlsl"

    float4 g_vBaseColor < Attribute( "BaseColor" ); Default4( 0.2, 0.25, 0.35, 1.0 ); >;
    float4 g_vGlowColor < Attribute( "GlowColor" ); Default4( 1.0, 0.45, 0.1, 1.0 ); >;
    float g_flGlow < Attribute( "Glow" ); Default( 0.0 ); >;
    float g_flDissolve < Attribute( "Dissolve" ); Default( 0.0 ); >;

    float Hash( float2 p )
    {
        float3 p3 = frac( float3( p.xyx ) * 0.1031 );
        p3 += dot( p3, p3.yzx + 33.33 );
        return frac( ( p3.x + p3.y ) * p3.z );
    }

    float ValueNoise( float2 p )
    {
        float2 cell = floor( p );
        float2 f = frac( p );
        f = f * f * ( 3.0 - 2.0 * f );

        float a = Hash( cell );
        float b = Hash( cell + float2( 1, 0 ) );
        float c = Hash( cell + float2( 0, 1 ) );
        float d = Hash( cell + float2( 1, 1 ) );
        return lerp( lerp( a, b, f.x ), lerp( c, d, f.x ), f.y );
    }

    float4 MainPs( PixelInput i ) : SV_Target0
    {
        float2 uv = i.vTextureCoords.xy;
        float noise = ValueNoise( uv * 14.0 ) * 0.65 + ValueNoise( uv * 37.0 ) * 0.35;

        // Dissolve: drop the pixels below the threshold, burn a glowing edge above it
        float threshold = g_flDissolve * 1.1;
        clip( noise - threshold );
        float edge = g_flDissolve > 0.001 ? 1.0 - smoothstep( 0.0, 0.08, noise - threshold ) : 0.0;

        Material m = Material::Init( i );
        m.Albedo = g_vBaseColor.rgb;
        m.Roughness = 0.35;
        m.Metalness = 0.0;
        m.AmbientOcclusion = 1.0;
        m.Opacity = 1.0;
        m.Emission = g_vGlowColor.rgb * ( g_flGlow + edge * 4.0 );

        return ShadingModelStandard::Shade( i, m );
    }
}