using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;

namespace EquationPlotter;

sealed class GridTexture : IDisposable {
	readonly TexelRgb24[] _texels;

	public XYPair<int> Dimensions { get; }
	public Texture Texture { get; }
	public Material Material { get; }

	public GridTexture(ILocalTinyFfrFactory factory, XYPair<int> dimensions, bool ignoresLighting = false) {
		Dimensions = dimensions;
		_texels = new TexelRgb24[dimensions.Area];
		Texture = factory.TextureBuilder.CreateColorMap(
			TexturePattern.GradientHorizontal(ColorVect.WhiteOpaque, ColorVect.WhiteOpaque, resolution: dimensions),
			includeAlpha: false,
			TextureCreationConfig.ForColorMap() with { AllowsDynamicWrites = true }
		);
		Material = ignoresLighting ? factory.MaterialBuilder.CreateLightingIgnoringMaterial(Texture) : factory.MaterialBuilder.CreateStandardMaterial(Texture);
	}

	public void Set(XYPair<int> gridCoordinate, TexelRgb24 texel) {
		_texels[(Dimensions.Y - 1 - gridCoordinate.Y) * Dimensions.X + gridCoordinate.X] = texel;
	}

	public void Upload() => Texture.OverwriteTexels<TexelRgb24>(_texels);

	public void Dispose() {
		Material.Dispose();
		Texture.Dispose();
	}
}
