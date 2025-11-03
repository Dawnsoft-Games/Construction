using System;
using Sandbox;
using Sandbox.UI;

namespace Construction.UI;

/// <summary>
/// A panel that renders a 3D model (like a sphere) in 2D UI space.
/// Supports zooming with mouse wheel and rotation with mouse drag.
/// Uses ScenePanel for proper 3D rendering in UI.
/// </summary>
public class Model3DPanel : ScenePanel
{
	/// <summary>
	/// The 3D model to render
	/// </summary>
	public Model Model { get; set; }
	
	/// <summary>
	/// Material to apply to the model
	/// </summary>
	public Material Material { get; set; }
	
	/// <summary>
	/// Tint color for the model
	/// </summary>
	public Color Tint { get; set; } = Color.White;
	
	/// <summary>
	/// Distance from camera (affects size)
	/// </summary>
	public float Distance { get; set; } = 150.0f;
	
	/// <summary>
	/// Scale of the model
	/// </summary>
	public float Scale { get; set; } = 1.0f;
	
	/// <summary>
	/// Minimum zoom distance
	/// </summary>
	public float MinDistance { get; set; } = 50.0f;
	
	/// <summary>
	/// Maximum zoom distance
	/// </summary>
	public float MaxDistance { get; set; } = 500.0f;
	
	/// <summary>
	/// Rotation of the model
	/// </summary>
	public Angles ModelRotation { get; set; } = new Angles( 0, 0, 0 );
	
	/// <summary>
	/// Enable mouse interaction for rotation
	/// </summary>
	public bool EnableRotation { get; set; } = true;
	
	/// <summary>
	/// Enable mouse wheel zoom
	/// </summary>
	public bool EnableZoom { get; set; } = true;
	
	/// <summary>
	/// Auto-rotate the model continuously
	/// </summary>
	public bool AutoRotate { get; set; } = false;
	
	/// <summary>
	/// Auto-rotation speed in degrees per second
	/// </summary>
	public float AutoRotateSpeed { get; set; } = 30.0f;

	private bool _isDragging = false;
	private Vector2 _lastMousePos;
	private SceneObject _sceneObject;
	private SceneLight _sceneLight1;
	private SceneLight _sceneLight2;
	private SceneLight _sceneLight3;

	public Model3DPanel()
	{
		Style.PointerEvents = PointerEvents.All;
		
		// Set camera properties
		// Scene handling is done when the panel is first rendered. Use a default model instead of building geometry.
		// Use a shipped dev model to avoid Model.Builder API differences.
		if ( Model == null )
		{
			Model = Model.Load( "models/dev/sphere.vmdl" );
		}
		
		// Scene will be created on first render in SetupScene()
	}

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );
		
		if ( firstTime )
		{
			SetupScene();
		}
	}

	private void SetupScene()
	{
		if ( World == null )
			return;
		
		// Clean up old objects
		_sceneObject?.Delete();
		_sceneLight1?.Delete();
		_sceneLight2?.Delete();
		_sceneLight3?.Delete();
		
		// Create scene object for the model
		if ( Model != null )
		{
			_sceneObject = new SceneObject( World, Model, new Transform( Vector3.Zero, Rotation.Identity, Scale ) );
			
			if ( Material != null )
			{
				_sceneObject.SetMaterialOverride( Material );
			}
			
			_sceneObject.ColorTint = Tint;
		}
		
		// Add lights to make the model visible
		_sceneLight1 = new SceneLight( World, new Vector3( 200, 0, 100 ), 1000, Color.White );
		_sceneLight2 = new SceneLight( World, new Vector3( -200, 0, 100 ), 800, Color.White * 0.7f );
		_sceneLight3 = new SceneLight( World, new Vector3( 0, 200, 50 ), 600, Color.White * 0.5f );
	}

	public override void Tick()
	{
		base.Tick();
		
		// Auto-rotation
		if ( AutoRotate )
		{
			var angles = ModelRotation;
			angles.yaw += AutoRotateSpeed * Time.Delta;
			angles.yaw = angles.yaw % 360;
			ModelRotation = angles;
		}
		
		// Update scene object
		if ( _sceneObject != null )
		{
			var rotation = Rotation.From( ModelRotation );
			_sceneObject.Transform = new Transform( Vector3.Zero, rotation, Scale );
			
			if ( Material != null )
			{
				_sceneObject.SetMaterialOverride( Material );
			}
			_sceneObject.ColorTint = Tint;
		}
		else if ( Model != null && World != null )
		{
			// Recreate scene object if it was lost
			SetupScene();
		}
		
		// Wheel handling removed: editor-only APIs (WheelEvent/Application.MouseWheelDelta) are not
		// available in all build targets. Use the UI controls (slider) to change Scale/Distance instead.
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );
		
		if ( EnableRotation && e.MouseButton == MouseButtons.Left )
		{
			_isDragging = true;
			_lastMousePos = Mouse.Position;
			e.StopPropagation();
		}
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );
		
		if ( e.MouseButton == MouseButtons.Left )
		{
			_isDragging = false;
		}
	}

	protected override void OnMouseMove( MousePanelEvent e )
	{
		base.OnMouseMove( e );
		
		if ( _isDragging && EnableRotation )
		{
			var delta = Mouse.Position - _lastMousePos;
			_lastMousePos = Mouse.Position;
			
			// Update rotation based on mouse movement
			var angles = ModelRotation;
			angles.yaw -= delta.x * 0.5f;
			angles.pitch += delta.y * 0.5f;
			
			// Clamp pitch to avoid flipping
			angles.pitch = Math.Clamp( angles.pitch, -89, 89 );
			
			ModelRotation = angles;
			
			e.StopPropagation();
		}
	}

	// Note: some UI assemblies (editor) define WheelEvent and OnWheel, but the runtime
	// game UI may not. Instead, handle mouse wheel input each Tick via Application.MouseWheelDelta

	public override void OnDeleted()
	{
		base.OnDeleted();
		
		// Clean up scene objects and lights
		_sceneObject?.Delete();
		_sceneLight1?.Delete();
		_sceneLight2?.Delete();
		_sceneLight3?.Delete();
		
		_sceneObject = null;
		_sceneLight1 = null;
		_sceneLight2 = null;
		_sceneLight3 = null;
	}
}
