namespace EquationPlotter;

enum PlotType {
	Surface,
	VectorField,
	ParametricSurface,
	ScalarSlice
}

enum SliceAxis {
	Horizontal,
	FacingZ,
	FacingX
}

sealed class PlotDomain {
	public const float WorldHalfSize = 2f;

	public float XMin { get; set; } = -3f;
	public float XMax { get; set; } = 3f;
	public float YMin { get; set; } = -1.5f;
	public float YMax { get; set; } = 1.5f;
	public float ZMin { get; set; } = -3f;
	public float ZMax { get; set; } = 3f;
	public float UMin { get; set; }
	public float UMax { get; set; } = MathF.Tau;
	public float VMin { get; set; }
	public float VMax { get; set; } = MathF.Tau;

	public int SurfaceResolution { get; set; } = 128;
	public int VectorFieldResolution { get; set; } = 10;
	public int ParametricResolution { get; set; } = 96;
	public int SliceResolution { get; set; } = 128;

	public bool NormalizeArrowLengths { get; set; } = true;
	public float ArrowLengthScale { get; set; } = 1f;
	public SliceAxis SliceAxis { get; set; } = SliceAxis.Horizontal;
	public float SlicePosition { get; set; }
	public ColorMapKind ColorMap { get; set; } = ColorMapKind.Viridis;

	public float ToWorldX(float x) => ToWorld(x, XMin, XMax);
	public float ToWorldY(float y) => ToWorld(y, YMin, YMax);
	public float ToWorldZ(float z) => ToWorld(z, ZMin, ZMax);
	public float FromNormalizedX(float normalized) => FromNormalized(normalized, XMin, XMax);
	public float FromNormalizedY(float normalized) => FromNormalized(normalized, YMin, YMax);
	public float FromNormalizedZ(float normalized) => FromNormalized(normalized, ZMin, ZMax);
	public float FromNormalizedU(float normalized) => FromNormalized(normalized, UMin, UMax);
	public float FromNormalizedV(float normalized) => FromNormalized(normalized, VMin, VMax);

	static float ToWorld(float value, float min, float max) => ((value - min) / (max - min) * 2f - 1f) * WorldHalfSize;
	static float FromNormalized(float normalized, float min, float max) => min + (normalized + 0.5f) * (max - min);
}
