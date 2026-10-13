using System.Numerics;
using Egodystonic.TinyFFR;
using Egodystonic.TinyFFR.World;

namespace EquationPlotter;

enum BackgroundStyle {
	SolidColor,
	Clouds,
	Starfield
}

sealed class SceneSettings {
	static readonly ColorVect DefaultBackgroundColor = new(30f / 255f, 34f / 255f, 43f / 255f);

	public BackgroundStyle Background { get; set; } = BackgroundStyle.SolidColor;
	public Vector3 BackgroundColor { get; set; } = new(DefaultBackgroundColor.Red, DefaultBackgroundColor.Green, DefaultBackgroundColor.Blue);
	public float OrbitSensitivity { get; set; } = 1f;
	public float ZoomSensitivity { get; set; } = 1f;
	public bool InvertHorizontal { get; set; }
	public bool InvertVertical { get; set; }
	public bool LabelsEnabled { get; set; } = true;
	public Vector3 LabelColor { get; set; } = new(0.9f, 0.9f, 0.9f);
	public Vector3 LabelOutlineColor { get; set; } = Vector3.Zero;
	public float LabelSizeMultiplier { get; set; } = 1f;
	public bool BoxEnabled { get; set; } = true;
	public bool AxesEnabled { get; set; } = true;
	public bool[] GridEnabled { get; } = [false, true, false];
	public int[] GridCount { get; } = [1, 1, 1];

	public void ApplyBackground(Scene scene) {
		switch (Background) {
			case BackgroundStyle.Clouds:
				scene.SetBackdrop(BuiltInSceneBackdrop.Clouds, SceneBackdropBrightnessPreset.InsideBrightLighting);
				break;
			case BackgroundStyle.Starfield:
				scene.SetBackdrop(BuiltInSceneBackdrop.Starfield, backdropIntensity: 1f);
				break;
			default:
				scene.SetBackdrop(ColorVect.SrgbToLinear(new ColorVect(BackgroundColor.X, BackgroundColor.Y, BackgroundColor.Z)), indirectLightingIntensity: 0.6f);
				break;
		}
	}
}
