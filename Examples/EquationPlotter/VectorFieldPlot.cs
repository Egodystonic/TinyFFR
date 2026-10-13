using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

sealed class VectorFieldPlot : IPlot {
	const int PaletteSize = 16;
	const float ArrowCellFraction = 0.9f;

	readonly Scene _scene;
	readonly PlotDomain _domain;
	readonly Mesh _arrowMesh;
	readonly Texture[] _paletteTextures = new Texture[PaletteSize];
	readonly Material[] _paletteMaterials = new Material[PaletteSize];
	readonly ModelInstance[] _arrows;
	readonly Location[] _samplePoints;
	readonly Vect[] _vectors;
	readonly int[] _paletteIndices;
	readonly float _cellSize;
	ColorMapKind _paletteColorMap;
	float _largestMagnitude;

	public string Description { get; }
	public float ColorRangeMin => 0f;
	public float ColorRangeMax => _largestMagnitude;

	public VectorFieldPlot(ILocalTinyFfrFactory factory, Scene scene, PlotDomain domain) {
		_scene = scene;
		_domain = domain;
		var n = domain.VectorFieldResolution;
		_cellSize = PlotDomain.WorldHalfSize * 2f / n;
		_arrowMesh = factory.MeshBuilder.CreateArrow(stemLength: 0.65f, stemRadius: 0.05f, headLength: 0.35f, headRadius: 0.13f, ArrowMeshOrigin.Centre, segmentCount: 12);
		for (var i = 0; i < PaletteSize; ++i) {
			_paletteTextures[i] = factory.TextureBuilder.CreateColorMap(ColorVect.WhiteOpaque, includeAlpha: false, TextureCreationConfig.ForColorMap() with { AllowsDynamicWrites = true });
			_paletteMaterials[i] = factory.MaterialBuilder.CreateStandardMaterial(_paletteTextures[i]);
		}
		_paletteColorMap = (ColorMapKind) (-1);

		_arrows = new ModelInstance[n * n * n];
		_samplePoints = new Location[_arrows.Length];
		_vectors = new Vect[_arrows.Length];
		_paletteIndices = new int[_arrows.Length];
		var index = 0;
		for (var x = 0; x < n; ++x) {
			for (var y = 0; y < n; ++y) {
				for (var z = 0; z < n; ++z) {
					var position = new Location(
						-PlotDomain.WorldHalfSize + (x + 0.5f) * _cellSize,
						-PlotDomain.WorldHalfSize + (y + 0.5f) * _cellSize,
						-PlotDomain.WorldHalfSize + (z + 0.5f) * _cellSize
					);
					_samplePoints[index] = position;
					_arrows[index] = factory.ObjectBuilder.CreateModelInstance(_arrowMesh, _paletteMaterials[0], position);
					scene.Add(_arrows[index]);
					++index;
				}
			}
		}
		Description = $"Vector field: {n} x {n} x {n} = {_arrows.Length:N0} arrows";
	}

	public void Update(float time, CompiledExpression[] expressions) {
		UpdatePalette();
		Span<float> variables = stackalloc float[4];
		variables[3] = time;

		var largestMagnitude = 0f;
		for (var i = 0; i < _arrows.Length; ++i) {
			var point = _samplePoints[i];
			variables[0] = _domain.FromNormalizedX(point.X / (PlotDomain.WorldHalfSize * 2f));
			variables[1] = _domain.FromNormalizedY(point.Y / (PlotDomain.WorldHalfSize * 2f));
			variables[2] = _domain.FromNormalizedZ(point.Z / (PlotDomain.WorldHalfSize * 2f));
			var vector = new Vect(SafeEvaluate(expressions[0], variables), SafeEvaluate(expressions[1], variables), SafeEvaluate(expressions[2], variables));
			_vectors[i] = vector;
			largestMagnitude = MathF.Max(largestMagnitude, vector.Length);
		}
		_largestMagnitude = largestMagnitude;

		for (var i = 0; i < _arrows.Length; ++i) {
			var vector = _vectors[i];
			var magnitude = vector.Length;
			var arrow = _arrows[i];
			var lengthFraction = _domain.NormalizeArrowLengths
				? (largestMagnitude > 0f ? magnitude / largestMagnitude : 0f)
				: MathF.Min(magnitude * _domain.ArrowLengthScale, 1.5f);
			if (lengthFraction <= 0.001f || vector.Direction == Direction.None) {
				arrow.SetScaling(Vect.Zero);
				continue;
			}

			var length = _cellSize * ArrowCellFraction * lengthFraction;
			var thickness = _cellSize * ArrowCellFraction;
			arrow.SetRotation(Rotation.FromStartAndEndDirection(Direction.Forward, vector.Direction));
			arrow.SetScaling(new Vect(thickness, thickness, length));

			var paletteIndex = largestMagnitude > 0f ? Math.Min((int) (magnitude / largestMagnitude * PaletteSize), PaletteSize - 1) : 0;
			if (paletteIndex == _paletteIndices[i]) continue;
			_paletteIndices[i] = paletteIndex;
			arrow.SetMaterial(_paletteMaterials[paletteIndex]);
		}
	}

	void UpdatePalette() {
		if (_paletteColorMap == _domain.ColorMap) return;
		_paletteColorMap = _domain.ColorMap;
		Span<TexelRgb24> texel = stackalloc TexelRgb24[1];
		for (var i = 0; i < PaletteSize; ++i) {
			texel[0] = ColorMap.SampleTexel(_paletteColorMap, (i + 0.5f) / PaletteSize);
			_paletteTextures[i].OverwriteTexels<TexelRgb24>(texel);
		}
	}

	static float SafeEvaluate(CompiledExpression expression, ReadOnlySpan<float> variables) {
		var value = expression.Evaluate(variables);
		return Single.IsFinite(value) ? value : 0f;
	}

	public void Dispose() {
		foreach (var arrow in _arrows) {
			_scene.Remove(arrow);
			arrow.Dispose();
		}
		_arrowMesh.Dispose();
		for (var i = 0; i < PaletteSize; ++i) {
			_paletteMaterials[i].Dispose();
			_paletteTextures[i].Dispose();
		}
	}
}
