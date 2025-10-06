using System;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Construction.UI;

/// <summary>
/// Lightweight helper that lets UI panels display VMAT-based materials as backgrounds
/// without relying on reflection (and therefore remaining whitelist-friendly).
/// A tiny render panel is injected behind the target panel and draws the material each frame.
/// </summary>
public static class MaterialCssSupport
{
	private const string BackgroundClass = "material-background-layer";

	/// <summary>
	/// Applies a loaded <see cref="Material"/> as a drawn background layer.
	/// Prefer this overload when you already have a Material instance.
	/// </summary>
	public static bool ApplyMaterialBackground( Panel panel, Material material, Color? optionalTint = null )
	{
		if ( panel is null || material is null )
		{
			return false;
		}

		Log.Info( $"MaterialCssSupport: Applying material instance to panel" );

		try
		{
			var layer = GetOrCreateLayer( panel );
			if ( layer == null || !layer.IsValid() )
			{
				Log.Warning( "MaterialCssSupport: Failed to create or get background layer" );
				return false;
			}

			layer.SetMaterial( material, optionalTint ?? Color.White );
			return true;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialCssSupport: Failed to apply background layer: {ex.Message}" );
			return false;
		}
	}

	/// <summary>
	/// Backwards-compatible overload: loads the material from path and applies it.
	/// </summary>
	public static bool ApplyMaterialBackground( Panel panel, string materialPath, Color? optionalTint = null )
	{
		if ( panel is null || string.IsNullOrWhiteSpace( materialPath ) )
		{
			return false;
		}

		Material material;
		try
		{
			material = Material.Load( materialPath );
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialCssSupport: Failed to load '{materialPath}': {ex.Message}" );
			return false;
		}

		if ( material is null )
		{
			Log.Warning( $"MaterialCssSupport: Material '{materialPath}' could not be loaded." );
			return false;
		}

		Log.Info( $"MaterialCssSupport: Loaded material '{materialPath}'" );

		return ApplyMaterialBackground( panel, material, optionalTint );
	}

	private static MaterialBackgroundLayer GetOrCreateLayer( Panel panel )
	{
		if ( panel == null || !panel.IsValid() )
		{
			Log.Warning( "MaterialCssSupport: Invalid panel provided to GetOrCreateLayer" );
			return null;
		}

		try
		{
			var layer = panel.Children?.OfType<MaterialBackgroundLayer>().FirstOrDefault( x => x.HasClass( BackgroundClass ) );

			if ( layer != null && layer.IsValid() )
			{
				return layer;
			}

			layer = panel.AddChild<MaterialBackgroundLayer>();
			if ( layer == null )
			{
				Log.Warning( "MaterialCssSupport: AddChild returned null" );
				return null;
			}
			
			layer.AddClass( BackgroundClass );
			layer.ConfigureLayoutFromParent();
			return layer;
		}
		catch ( Exception ex )
		{
			Log.Warning( $"MaterialCssSupport: Exception in GetOrCreateLayer: {ex.Message}" );
			return null;
		}
	}

	private sealed class MaterialBackgroundLayer : Panel
	{
		Material backgroundMaterial;
		Color tint = Color.White;

		public void ConfigureLayoutFromParent()
		{
			Style.Position = PositionMode.Absolute;
			Style.Left = 0;
			Style.Right = 0;
			Style.Top = 0;
			Style.Bottom = 0;
			Style.PointerEvents = PointerEvents.None;
			Style.ZIndex = -100;
		}

		public void SetMaterial( Material material, Color newTint )
		{
			backgroundMaterial = material;
			tint = newTint;
		}

		public override void DrawContent( ref RenderState state )
		{
			base.DrawContent( ref state );

			if ( backgroundMaterial is null )
			{
				return;
			}

			try
			{
				Graphics.DrawQuad( Box.Rect, backgroundMaterial, tint );
			}
			catch ( Exception ex )
			{
				// Log and fall back to a safe UI material to avoid the engine error-texture (magenta checkerboard)
				Log.Warning( $"MaterialCssSupport: DrawQuad failed for material (will fallback): {ex.Message}" );
				try
				{
					Graphics.DrawQuad( Box.Rect, Material.UI.Basic, tint );
				}
				catch
				{
					// as a last resort draw a solid colour
					Graphics.DrawQuad( Box.Rect, Material.UI.Basic, Color.Gray );
				}

				// Also set a safe background color on the layer so the engine's error texture isn't visible
				try
				{
					Style.BackgroundColor = tint;
				}
				catch
				{
					// ignore styling failures
				}
			}
		}
	}
}
