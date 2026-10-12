using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.Assets.Materials;

namespace EquationPlotter;

enum ColorMapKind {
	Viridis,
	Rainbow,
	Heat,
	CoolWarm
}

static class ColorMap {
	static readonly ColorVect[] ViridisStops = [new(0.267f, 0.005f, 0.329f), new(0.229f, 0.322f, 0.546f), new(0.128f, 0.567f, 0.551f), new(0.369f, 0.789f, 0.383f), new(0.993f, 0.906f, 0.144f)];
	static readonly ColorVect[] RainbowStops = [new(0.35f, 0.1f, 0.85f), new(0.1f, 0.45f, 1f), new(0.1f, 0.85f, 0.55f), new(0.95f, 0.9f, 0.15f), new(1f, 0.45f, 0.1f), new(0.85f, 0.1f, 0.2f)];
	static readonly ColorVect[] HeatStops = [new(0.05f, 0.02f, 0.02f), new(0.6f, 0.05f, 0.02f), new(0.95f, 0.45f, 0.05f), new(1f, 0.85f, 0.3f), new(1f, 1f, 0.9f)];
	static readonly ColorVect[] CoolWarmStops = [new(0.23f, 0.3f, 0.75f), new(0.55f, 0.69f, 0.99f), new(0.87f, 0.87f, 0.87f), new(0.96f, 0.6f, 0.48f), new(0.71f, 0.02f, 0.15f)];

	public static ColorVect Sample(ColorMapKind kind, float fraction) {
		var stops = kind switch {
			ColorMapKind.Rainbow => RainbowStops,
			ColorMapKind.Heat => HeatStops,
			ColorMapKind.CoolWarm => CoolWarmStops,
			_ => ViridisStops
		};
		if (!Single.IsFinite(fraction)) fraction = 0f;
		var scaled = Math.Clamp(fraction, 0f, 1f) * (stops.Length - 1);
		var index = Math.Min((int) scaled, stops.Length - 2);
		var t = scaled - index;
		var a = stops[index];
		var b = stops[index + 1];
		return new ColorVect(a.Red + (b.Red - a.Red) * t, a.Green + (b.Green - a.Green) * t, a.Blue + (b.Blue - a.Blue) * t);
	}

	public static TexelRgb24 SampleTexel(ColorMapKind kind, float fraction) {
		var color = Sample(kind, fraction);
		return TexelRgb24.FromNormalizedFloats(color.Red, color.Green, color.Blue);
	}
}
