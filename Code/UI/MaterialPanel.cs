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
	/// Offset of the material in pixels (X, Y, Z)
	/// </summary>
	public Vector3 Offset { get; set; } = Vector3.Zero;
	
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
			
			// Apply offset by moving the rect (X and Y)
			if ( Offset.x != 0 || Offset.y != 0 )
			{
				rect.Position += new Vector2( Offset.x, Offset.y );
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
				// Shader materials - render with manual vertex transformation
				try
				{
					// Calculate center point for rotation (with Z offset applied)
					var center = new Vector3( rect.Center.x, rect.Center.y, Offset.z );
					
					// Create rotation matrix from Euler angles
					var hasRotation = RotationX != 0 || RotationY != 0 || RotationZ != 0;
					Rotation rotation = Rotation.Identity;
					
					if ( hasRotation )
					{
						// Convert degrees to rotation (pitch, yaw, roll)
						rotation = Rotation.From( RotationX, RotationY, RotationZ );
					}
					
					// Define quad corners relative to center
					var halfWidth = rect.Width * 0.5f;
					var halfHeight = rect.Height * 0.5f;
					
					Vector3[] corners = new Vector3[]
					{
						new Vector3( -halfWidth, -halfHeight, 0 ), // Top-left
						new Vector3( halfWidth, -halfHeight, 0 ),  // Top-right
						new Vector3( halfWidth, halfHeight, 0 ),   // Bottom-right
						new Vector3( -halfWidth, halfHeight, 0 )   // Bottom-left
					};
					
					// Apply rotation to each corner, then translate to center (with Z offset)
					for ( int i = 0; i < 4; i++ )
					{
						if ( hasRotation )
						{
							corners[i] = rotation * corners[i];
						}
						corners[i] += center; // This now includes Offset.z
					}
					
					// Create vertices with proper UV coordinates
					var v0 = new Vertex( corners[0], Vector3.Forward, Vector3.Right, new Vector2( 0, 0 ) );
					var v1 = new Vertex( corners[1], Vector3.Forward, Vector3.Right, new Vector2( 1, 0 ) );
					var v2 = new Vertex( corners[2], Vector3.Forward, Vector3.Right, new Vector2( 1, 1 ) );
					var v3 = new Vertex( corners[3], Vector3.Forward, Vector3.Right, new Vector2( 0, 1 ) );
					
					// Apply tint to vertices
					v0.Color = Tint;
					v1.Color = Tint;
					v2.Color = Tint;
					v3.Color = Tint;
					
					// Draw two triangles to form a quad
					Span<Vertex> vertices = stackalloc Vertex[6]
					{
						v0, v1, v2,  // First triangle
						v0, v2, v3   // Second triangle
					};
					
					Graphics.Draw( vertices, 6, Material );
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
				Style.BackgroundImage = _cachedTexture;
				Style.BackgroundTint = Tint;
				
				// Apply scale
				Style.BackgroundSizeX = Length.Percent( 100 * Scale );
				Style.BackgroundSizeY = Length.Percent( 100 * Scale );
				
				// Apply offset - CSS background-position works differently when scaled
				// We need to calculate the position considering the scale
				if ( Scale != 1.0f )
				{
					// When scaled, center it and apply offset
					var centerOffsetX = (rect.Width - (rect.Width * Scale)) * 0.5f;
					var centerOffsetY = (rect.Height - (rect.Height * Scale)) * 0.5f;
					Style.BackgroundPositionX = Length.Pixels( centerOffsetX + Offset.x );
					Style.BackgroundPositionY = Length.Pixels( centerOffsetY + Offset.y );
				}
				else
				{
					// No scale, just apply offset
					Style.BackgroundPositionX = Length.Pixels( Offset.x );
					Style.BackgroundPositionY = Length.Pixels( Offset.y );
				}
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
