using System;
using Sandbox;
using Sandbox.UI;

namespace Construction.UI;

/// <summary>
/// A panel that renders a VMAT material as its background.
/// Uses DrawContent to render the material directly each frame.
/// </summary>
public class MaterialPanel : Sandbox.UI.Panel
{
	public Material Material { get; set; }
	public Color Tint { get; set; } = Color.White;

	public override void DrawContent( ref RenderState state )
	{
		base.DrawContent( ref state );

		if ( Material == null )
		{
			return;
		}

		try
		{
			// Draw material directly as background
			Graphics.DrawQuad( Box.Rect, Material, Tint );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialPanel: DrawQuad failed: {ex.Message}. Using fallback color." );
			// Fallback: draw solid color
			try
			{
				Graphics.DrawQuad( Box.Rect, Material.UI.Basic, Tint );
			}
			catch
			{
				// Last resort: set background color via style
				Style.BackgroundColor = Tint;
			}
		}
	}
}
