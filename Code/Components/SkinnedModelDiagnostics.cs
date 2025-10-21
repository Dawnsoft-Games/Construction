using Sandbox;
using System;
using System.Diagnostics;

namespace Construction;

/// <summary>
/// Einfache Laufzeit-Diagnose: zählt SkinnedModelRenderer-Instanzen und loggt periodisch.
/// Nutze das, um grob zu sehen, ob viele Modelle aktiv sind.
/// </summary>
[Title( "Skinned Model Diagnostics" )]
public class SkinnedModelDiagnostics : Component
{
    [Property]
    public float LogIntervalSeconds { get; set; } = 5.0f;

    private float _accum = 0f;

    protected override void OnUpdate()
    {
        _accum += Time.Delta;
        if ( _accum < LogIntervalSeconds ) return;
        _accum = 0f;

        var all = Scene.GetAll<SkinnedModelRenderer>();
        int total = 0;
        foreach ( var smr in all )
        {
            if ( smr == null ) continue;
            total++;
        }

        Log.Info( $"[SkinnedModelDiagnostics] Active SkinnedModelRenderers: {total}" );
    }
}
