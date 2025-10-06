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
	/// Rotation of the material in degrees
	/// </summary>
	public float Rotation { get; set; } = 0.0f;

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
		if ( Time.Now % 1.0f < 0.016f )
		{
			Log.Info( $"MaterialPanel.Tick - Material: {(Material != null ? Material.Name : "NULL")}, Box: {Box.Rect}" );
		}
	}

	// Use DrawBackground instead of DrawContent - this is called even for empty panels!
	public override void DrawBackground( ref RenderState state )
	{
		base.DrawBackground( ref state );

		Log.Info( $"MaterialPanel.DrawBackground called - Material: {(Material != null ? Material.Name : "NULL")}, Box.Rect: {Box.Rect}, Tint: {Tint}" );

		if ( Material == null )
		{
			Log.Warning( "MaterialPanel: Material is null in DrawBackground" );
			return;
		}

		try
		{
			Log.Info( $"MaterialPanel: About to DrawQuad with rect {Box.Rect}" );
			Graphics.DrawQuad( Box.Rect, Material, Tint );
			Log.Info( $"MaterialPanel: DrawQuad succeeded!" );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialPanel: DrawQuad failed: {ex.Message}. Using fallback." );
			try
			{
				Graphics.DrawQuad( Box.Rect, Material.UI.Basic, Tint );
			}
			catch
			{
				Style.BackgroundColor = Tint;
			}
		}
	}
}
