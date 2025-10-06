using System;
using Sandbox;
using Sandbox.UI;

namespace Construction.UI;

/// <summary>
/// A panel that renders a VMAT material as its background.
/// Uses DrawBackground to render the material behind all content.
/// </summary>
public class MaterialPanel : Sandbox.UI.Panel
{
	public Material Material { get; set; }
	public Color Tint { get; set; } = Color.White;
	
	/// <summary>
	/// Offset of the material in pixels (X, Y)
	/// </summary>
	public Vector2 Offset { get; set; } = Vector2.Zero;
	
	/// <summary>
	/// Scale of the material (1.0 = normal size, 2.0 = double size, 0.5 = half size)
	/// </summary>
	public float Scale { get; set; } = 1.0f;
	
	/// <summary>
	/// Rotation around Z-axis (Roll) in degrees - 2D rotation
	/// </summary>
	public float RotationZ { get; set; } = 0.0f;
	
	/// <summary>
	/// Rotation around X-axis (Pitch) in degrees - tilt up/down
	/// </summary>
	public float RotationX { get; set; } = 0.0f;
	
	/// <summary>
	/// Rotation around Y-axis (Yaw) in degrees - tilt left/right
	/// </summary>
	public float RotationY { get; set; } = 0.0f;
	
	/// <summary>
	/// If true, the material position is fixed relative to the screen (doesn't scroll with content)
	/// </summary>
	public bool ParallelToScreen { get; set; } = false;

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );
		
		// Force the panel to always render
		Style.Display = DisplayMode.Flex;
		Style.Width = Length.Percent( 100 );
		Style.Height = Length.Percent( 100 );
		
		Log.Info( $"MaterialPanel.OnAfterTreeRender - firstTime: {firstTime}, Material: {(Material != null ? Material.Name : "NULL")}" );
	}

	private Material _lastMaterial;
	private Texture _cachedTexture;
	private bool _useDirectMaterial = false;

	public override void Tick()
	{
		base.Tick();
		
		// Update texture when material changes
		if ( Material != _lastMaterial )
		{
			_lastMaterial = Material;
			UpdateMaterialTexture();
		}
	}

	private void UpdateMaterialTexture()
	{
		if ( Material == null )
		{
			_cachedTexture = null;
			_useDirectMaterial = false;
			Style.BackgroundImage = null;
			return;
		}

		// Try to get the base texture from the material
		_cachedTexture = Material.GetTexture( "Color" ) ?? 
		                 Material.GetTexture( "Albedo" ) ?? 
		                 Material.GetTexture( "g_tColor" ) ??
		                 Material.GetTexture( "TextureColor" ) ??
		                 Material.GetTexture( "tintmasktexture" );

		if ( _cachedTexture == null )
		{
			// No texture found - this is a shader material
			// NOTE: Shader materials will render in 3D world space, not 2D UI space
			// This is a limitation of the s&box API
			_useDirectMaterial = true;
			Log.Info( $"MaterialPanel: Material {Material.Name} is a shader material (will render in 3D space)" );
		}
		else
		{
			_useDirectMaterial = false;
			Log.Info( $"MaterialPanel: Material {Material.Name} has texture (will render in 2D UI)" );
		}
	}

	// Use DrawBackground instead of DrawContent - this is called even for empty panels!
	public override void DrawBackground( ref RenderState state )
	{
		base.DrawBackground( ref state );

		if ( Material == null )
		{
			Style.BackgroundImage = null;
			Style.BackgroundColor = Color.Transparent;
			return;
		}

		try
		{
			var rect = Box.Rect;
			
			// If ParallelToScreen is enabled, use screen-space coordinates
			if ( ParallelToScreen )
			{
				// Get the screen-space position
				rect = new Rect( 0, 0, Screen.Width, Screen.Height );
			}
			
			// Apply offset by moving the rect
			if ( Offset != Vector2.Zero )
			{
				rect.Position += Offset;
			}
			
			// Apply scale by changing the size from center
			if ( Scale != 1.0f )
			{
				var center = rect.Center;
				var newSize = rect.Size * Scale;
				rect = new Rect( center.x - newSize.x * 0.5f, center.y - newSize.y * 0.5f, newSize.x, newSize.y );
			}
			
			if ( _useDirectMaterial )
			{
				// Shader materials - EXPERIMENTAL: Try to force 2D rendering
				// The problem: Graphics.Draw uses 3D world space
				// Solution attempt: Use Graphics attributes to override projection
				try
				{
					// Save current graphics state
					var oldAttributes = Graphics.Attributes;
					
					// Try to set 2D orthographic attributes
					Graphics.Attributes.Set( "UI_RENDERING", 1.0f );
					Graphics.Attributes.Set( "SCREEN_SPACE", 1.0f );
					
					// Apply transforms
					var transformedRect = rect;
					if ( ParallelToScreen )
					{
						transformedRect = new Rect( Offset.x, Offset.y, Screen.Width * Scale, Screen.Height * Scale );
					}
					else
					{
						var center = rect.Center;
						var newSize = rect.Size * Scale;
						transformedRect = new Rect( 
							center.x - newSize.x * 0.5f + Offset.x, 
							center.y - newSize.y * 0.5f + Offset.y, 
							newSize.x, 
							newSize.y 
						);
					}
					
					// Use DrawQuad but with UI attributes
					Graphics.DrawQuad( transformedRect, Material, Tint );
					
					// Restore graphics state
					Graphics.Attributes = oldAttributes;
				}
				catch ( Exception ex )
				{
					Log.Warning( $"MaterialPanel: Failed to render shader material: {ex.Message}" );
					Style.BackgroundColor = Tint;
				}
			}
			else if ( _cachedTexture != null )
			{
				// Regular texture materials render in 2D UI space
				var transformedRect = rect;
				
				// Apply ParallelToScreen
				if ( ParallelToScreen )
				{
					transformedRect = new Rect( Offset.x, Offset.y, Screen.Width * Scale, Screen.Height * Scale );
				}
				
				Style.BackgroundImage = _cachedTexture;
				Style.BackgroundTint = Tint;
				Style.BackgroundSizeX = Length.Percent( 100 * Scale );
				Style.BackgroundSizeY = Length.Percent( 100 * Scale );
				Style.BackgroundPositionX = Length.Pixels( Offset.x );
				Style.BackgroundPositionY = Length.Pixels( Offset.y );
			}
			else
			{
				// Fallback: Just use the tint color
				Style.BackgroundColor = Tint;
			}
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialPanel: Draw failed: {ex.Message}" );
		}
	}
}
