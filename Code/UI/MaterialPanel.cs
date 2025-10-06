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

	public override void Tick()
	{
		base.Tick();
		// Log every second to confirm Tick is called
		
	}

	// Use DrawBackground instead of DrawContent - this is called even for empty panels!
	public override void DrawBackground( ref RenderState state )
	{
		base.DrawBackground( ref state );

		if ( Material == null )
		{
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
			
			// Try to get the base texture from the material
			Texture texture = Material.GetTexture( "Color" ) ?? Material.GetTexture( "Albedo" ) ?? Material.GetTexture( "g_tColor" );
			
			if ( texture != null )
			{
				// Use the texture as background via Style
				Style.BackgroundImage = texture;
				Style.BackgroundTint = Tint;
				Style.BackgroundSizeX = Length.Pixels( rect.Width );
				Style.BackgroundSizeY = Length.Pixels( rect.Height );
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
