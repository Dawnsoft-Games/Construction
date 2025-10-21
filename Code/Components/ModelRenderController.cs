using Sandbox;
using System.Linq;

namespace Construction;

/// <summary>
/// Kontrolliert das Rendering von SkinnedModelRenderer für Performance-Optimierung.
/// Kann Rendering deaktivieren, Update-Rate reduzieren und Distanz-Culling anwenden.
///
/// Hinweis: `SkinnedModelRenderer` hat nicht überall ein `RenderingEnabled`-Flag. Wir
/// versuchen zuerst das zugehörige SceneModel/SceneObject zu finden und dessen
/// `RenderingEnabled` zu setzen. Falls das nicht verfügbar ist, deaktivieren wir
/// das GameObject selbst (`GameObject.Enabled`).
/// </summary>
[Title( "Model Render Controller" )]
public class ModelRenderController : Component
{
	[Property]
	public SkinnedModelRenderer TargetRenderer { get; set; }

	[Property]
	public bool EnableRendering { get; set; } = true;

	[Property, Range( 1, 10 )]
	public int UpdateEveryNFrames { get; set; } = 1;

	[Property, Range( 0, 2000 )]
	public float CullDistance { get; set; } = 0f; // 0 = kein Culling

	private int _frameCounter = 0;
	[Property]
	public bool EnableDebugLogs { get; set; } = false;
	private float _logAccum = 0f;

	protected override void OnUpdate()
	{
		if ( TargetRenderer == null ) return;

		_frameCounter++;
		bool shouldUpdateThisFrame = (_frameCounter % UpdateEveryNFrames) == 0;

		// Distanz-Culling: benutze den ersten PlayerController im Scene (falls vorhanden)
		bool isInRange = true;
		if ( CullDistance > 0 )
		{
			var player = Scene.GetAll<PlayerController>().FirstOrDefault();
			if ( player != null )
			{
				var dist = Vector3.DistanceBetween( GameObject.WorldPosition, player.GameObject.WorldPosition );
				isInRange = dist <= CullDistance;
			}
		}

		bool shouldRender = EnableRendering && isInRange;

		// Versuch 1: SceneModel/SceneObject RenderingEnabled setzen (Editor-API Beispiele nutzen das)
		var sceneModel = TargetRenderer.SceneModel;
		if ( sceneModel != null )
		{
			if ( sceneModel.RenderingEnabled != shouldRender )
				sceneModel.RenderingEnabled = shouldRender;
		}
		else
		{
			// Fallback: setze das GameObject selbst enabled/disabled
			if ( TargetRenderer.GameObject != null && TargetRenderer.GameObject.Enabled != shouldRender )
				TargetRenderer.GameObject.Enabled = shouldRender;
		}

		// Optional: wenn wir aktualisieren sollen, können wir hier Animationen oder Parameter
		// anstoßen. Das ist je nach Engine-API unterschiedlich; als sicherer Platzhalter
		// lassen wir es aktuell leer.
		if ( shouldRender && shouldUpdateThisFrame )
		{
			// Beispiel: TargetRenderer.SceneModel?.Advance( Time.Delta );
		}

		if ( EnableDebugLogs )
		{
			_logAccum += Time.Delta;
			if ( _logAccum >= 1.0f )
			{
				_logAccum = 0f;
				var pos = TargetRenderer.GameObject?.WorldPosition ?? GameObject.WorldPosition;
				var player = Scene.GetAll<PlayerController>().FirstOrDefault();
				float dist = -1f;
				if ( player != null ) dist = Vector3.DistanceBetween( pos, player.GameObject.WorldPosition );
				Log.Info( $"[ModelRenderController] shouldRender={shouldRender} inRange={isInRange} dist={dist} frameMod={UpdateEveryNFrames} target={TargetRenderer?.GetHashCode()}" );
			}
		}
	}

	public void ForceUpdate()
	{
		if ( TargetRenderer == null ) return;

		var sceneModel = TargetRenderer.SceneModel;
		if ( sceneModel != null )
			sceneModel.RenderingEnabled = EnableRendering;
		else if ( TargetRenderer.GameObject != null )
			TargetRenderer.GameObject.Enabled = EnableRendering;
	}
}