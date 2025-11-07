#ifndef PROCEDURAL_H
#define PROCEDURAL_H

#define M_PI2 ( 6.28318530717958647692 )
#define M_INVPI ( 0.31830988618379067154 )
#define M_INV2PI ( 0.15915494309189533577 )

// Generate "TV static" type of noise
float FuzzyNoise(float2 vUv, float2 vDot = float2(12.9898f, 78.233f))
{
    return frac(sin(dot(vUv, vDot))*43758.5453f);
}

float2 FuzzyNoise2D(float2 vUv)
{
    return float2(
        FuzzyNoise(vUv, float2(12.9898f, 78.233f)),
        FuzzyNoise(vUv, float2(39.346f, 11.135f))
    );
}

float2 FuzzyNoiseWithOffset( float2 vUv, float flOffset )
{
    float2 noise = FuzzyNoise2D( vUv );
    return float2(
        sin(noise.y * flOffset) * 0.5f + 0.5f,
        cos(noise.x * flOffset) * 0.5f + 0.5f
    );
}

// Value noise
float ValueNoise(float2 vUv)
{
    // Get our current cell
    int2 curCell = int2(floor(vUv));
    float2 cellUv = frac(vUv);
    cellUv = cellUv * cellUv * (3.0f - 2.0f * cellUv);

    // Get UVs for each corner of our cell
    int2 tl = curCell + int2(0, 0);
    int2 tr = curCell + int2(1, 0);
    int2 bl = curCell + int2(0, 1);
    int2 br = curCell + int2(1, 1);

    // Sample each corner
    float tlR = FuzzyNoise(tl);
    float trR = FuzzyNoise(tr);
    float blR = FuzzyNoise(bl);
    float brR = FuzzyNoise(br);

    // Interpolate to get the average noise of the cell
    float topSmooth = lerp(tlR, trR, cellUv.x);
    float bottomSmooth = lerp(blR, brR, cellUv.x);
    return lerp(topSmooth, bottomSmooth, cellUv.y);
}

// 1.0f / 289.0f
#define SIMPLEX_MOD(x, m) ((x) - floor((x) * 0.0034602076124567f) * (m))
#define SIMPLEX_PERMUTE(x) SIMPLEX_MOD( (x)*(x)*34.0f + (x), 289.0f )

float Simplex2D(float2 vUv)
{
    const float4 CONST = float4(
        0.2113248654f, // (3.0f - sqrt(3.0f) ) / 6.0f
        0.36602540378f, // (sqrt(3.0f) - 1.0f) / 2.0f 
        -0.5773502692f, // -1.0f + 2.0f * 0.2113248654f
        0.0243902439f // 1.0f / 41.0f
    );

    // Skew space to find which cell we're in
    float2 cornerUv = floor(vUv + dot(vUv, CONST.yy));
    float2 x0 = vUv - cornerUv + dot(cornerUv, CONST.xx);

    // Find out which simplex we're in

    // Work out our current triangle order
    float2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float4 x12 = x0.xyxy + CONST.xxzz; // Calculate unskewed cordinates
    x12.xy -= i1;

    // Compute hashed gradients
    cornerUv = SIMPLEX_MOD( cornerUv, float2(289.0f, 289.0f) );
    float3 permutation = SIMPLEX_PERMUTE(
        SIMPLEX_PERMUTE(
            cornerUv.y + float3(0.0f, i1.y, 1.0f)
        ) + cornerUv.x + float3(0.0f, i1.x, 1.0f)
    );

    // Calculate corner contributions
    float3 contribution = max(
        0.5 - float3(
            dot(x0, x0),
            dot(x12.xy, x12.xy),
            dot(x12.zw, x12.zw)
        ), 0.0 );

    contribution = contribution*contribution;
    contribution = contribution*contribution;

    // Calculate gradient ring
    float3 x = 2.0f * frac(permutation * CONST.www) - 1.0f;
    float3 h = abs(x) - 0.5f;
    float3 ox = floor(x + 0.5f);
    float3 a0 = x - ox;

    // Gradient normalization
    contribution *= rsqrt( a0*a0 + h*h );

    // Add contributions and calculate final noise
    float3 gradient = float3(
        a0.x * x0.x + h.x * x0.y,
        a0.yz * x12.xz + h.yz * x12.yw
    );
    return 130.0f * dot(contribution, gradient); 
}

// Keep permute/macros available for a 3D implementation below

float Simplex3D(float3 v)
{
    // Classic 3D simplex noise (adapted). Returns in range approximately [-1,1].
    const float F3 = 1.0f/3.0f;
    const float G3 = 1.0f/6.0f;

    // Skew the input space to determine which simplex cell we're in
    float s = (v.x + v.y + v.z) * F3;
    float3 i = floor(v + s);
    float t = (i.x + i.y + i.z) * G3;
    float3 X0 = i - t; // unskew the cell origin back to (x,y,z) space
    float3 x0 = v - X0; // the x,y,z distances from the cell origin

    // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
    float3 rank = step(float3(x0.y, x0.z, x0.x), x0);
    float3 i1 = float3(0,0,0);
    float3 i2 = float3(0,0,0);
    // Rank sorting (which simplex we are in)
    if(x0.x >= x0.y)
    {
        if(x0.y >= x0.z) { i1 = float3(1,0,0); i2 = float3(1,1,0); }
        else if(x0.x >= x0.z) { i1 = float3(1,0,0); i2 = float3(1,0,1); }
        else { i1 = float3(0,0,1); i2 = float3(1,0,1); }
    }
    else
    {
        if(x0.y < x0.z) { i1 = float3(0,0,1); i2 = float3(0,1,1); }
        else if(x0.x < x0.z) { i1 = float3(0,1,0); i2 = float3(0,1,1); }
        else { i1 = float3(0,1,0); i2 = float3(1,1,0); }
    }

    // Offsets for the other corners
    float3 x1 = x0 - i1 + G3;
    float3 x2 = x0 - i2 + 2.0f*G3;
    float3 x3 = x0 - 1.0f + 3.0f*G3;

    // Permutations: compute four permutation values (one per simplex corner)
    // Ensure vector sizes match the SIMPLEX_PERMUTE macro expectations.
    float4 perm = SIMPLEX_PERMUTE(
        SIMPLEX_PERMUTE(
            SIMPLEX_PERMUTE(i.z + float4(0.0f, i1.z, i2.z, 1.0f)) + i.y + float4(0.0f, i1.y, i2.y, 1.0f)
        ) + i.x + float4(0.0f, i1.x, i2.x, 1.0f)
    );

    // Gradients (approx.)
    float4 grad4 = 2.0f * frac(perm * 0.0243902439f) - 1.0f; // reuse const from 2D (1/41)

    // Contribution
    float4 t0 = 0.6f - float4(dot(x0,x0), dot(x1,x1), dot(x2,x2), dot(x3,x3));
    t0 = max(t0, 0.0f);
    t0 = t0 * t0;
    t0 = t0 * t0;

    // Compute dot products between gradient and location vectors
    float n0 = dot(float3(grad4.x, grad4.y, grad4.z), x0);
    float n1 = dot(float3(grad4.y, grad4.z, grad4.x), x1);
    float n2 = dot(float3(grad4.z, grad4.x, grad4.y), x2);
    float n3 = dot(float3(grad4.x, grad4.z, grad4.y), x3);

    float noise = 32.0f * (t0.x * n0 + t0.y * n1 + t0.z * n2 + t0.w * n3);
    return noise;
}

// Fractional Brownian Motion (FBM) using Simplex3D
float SimplexFBM3D(float3 p, int octaves, float lacunarity, float gain)
{
    float amplitude = 1.0f;
    float frequency = 1.0f;
    float sum = 0.0f;
    float maxAmp = 0.0f;

    [unroll]
    for(int i = 0; i < 8; i++) // cap unroll to 8
    {
        if(i >= octaves) break;
        sum += amplitude * Simplex3D(p * frequency);
        maxAmp += amplitude;
        amplitude *= gain;
        frequency *= lacunarity;
    }

    // Normalize to [-1,1] approximately
    if(maxAmp > 0.0f) sum /= maxAmp;
    return sum;
}

#undef SIMPLEX_MOD
#undef SIMPLEX_PERMUTE

float VoronoiNoise(float2 vUv, float flAngleOffset, float flDensity)
{
    float2 g = floor( vUv * flDensity );
    float2 v = frac( vUv * flDensity );

    // Random big number!
    float flDistanceToCell = 4.0f;
    [unroll]
    for(int y=-1; y<=1; y++)
    {
        [unroll]
        for(int x=-1; x<=1; x++)
        {
            float2 vRelativeCell = float2( x, y );
            float2 vOffsetInCell = FuzzyNoiseWithOffset( vRelativeCell + g, flAngleOffset );
            float2 vAbsolute = vRelativeCell + vOffsetInCell;
            float flDistanceToPoint = distance( vAbsolute, v );
            if( flDistanceToPoint < flDistanceToCell )
            {
                flDistanceToCell = flDistanceToPoint;
            }
        }
    }
    return flDistanceToCell;
}


// Masks
float Checkerboard(float2 vUv, float2 vFrequency)
{
    vUv = (vUv * 0.5f) * vFrequency;
    float2 vUvDerivaties = fwidth(vUv);
    float2 vDerivativeScale = 0.35f / vUvDerivaties;

    float2 vDistanceToEdge = 4.0f * abs(frac(vUv + 0.25f) - 0.5f) - 1.0f;

    float2 vCheckerMask = clamp( vDistanceToEdge * vDerivativeScale, -1.0f, 1.0f );
    float fFinalMask = saturate( 0.5f + 0.5f * vCheckerMask.x * vCheckerMask.y );

    return fFinalMask;
}

float Circle(float2 vUv, float fSize)
{
    float fCircleSdf = length((vUv * 2.0f - 1.0f) / fSize);
    return saturate( (1.0f - fCircleSdf) / fwidth( fCircleSdf ) );
}

float Ellipse(float2 vUv, float2 vSize)
{
    float fCircleSdf = length((vUv * 2.0f - 1.0f) / vSize);
    return saturate( (1.0f - fCircleSdf) / fwidth( fCircleSdf ) );
}

float Square(float2 vUv, float fSize)
{
    float2 fSquareSdf = abs( vUv * 2.0f - 1.0f ) - fSize;
    fSquareSdf = 1.0f - fSquareSdf / fwidth( fSquareSdf );
    return saturate(min(fSquareSdf.x, fSquareSdf.y));
}

float Rect(float2 vUv, float2 fSize)
{
    float2 fSquareSdf = abs( vUv * 2.0f - 1.0f ) - fSize;
    fSquareSdf = 1.0f - fSquareSdf / fwidth( fSquareSdf );
    return saturate(min(fSquareSdf.x, fSquareSdf.y));
}

// UV Helpers
float2 TileUv( float2 vUv, float2 vTile )
{
    return vUv * vTile;
}

float2 OffsetUv( float2 vUv, float2 vOffset )
{
    return vUv + vOffset;
}

float2 TileAndOffsetUv( float2 vUv, float2 vTile, float2 vOffset )
{
    return vUv * vTile + vOffset;
}

float2 PolarCoordinates( float2 vUv, float fRadiusScale, float fLengthScale )
{
    float fRadiusSdf = length(vUv) * 2.0f * fRadiusScale;
    float fAngle = atan2(vUv.x, vUv.y) * M_INV2PI * fLengthScale;
    return float2( fRadiusSdf, fAngle );
}

#endif