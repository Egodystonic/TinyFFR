using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace SceneProgrammer;

sealed class RenderSettings {
	public BuiltInQualityConfiguration Quality { get; set; } = BuiltInQualityConfiguration.High;
	public bool DepthOfFieldEnabled { get; set; }
	public float FocusDistance { get; set; } = 5f;
	public float DepthOfFieldStrength { get; set; } = 1f;

	public void ApplyTo(Renderer renderer, Camera camera) {
		renderer.SetQuality(new RenderQualityConfig(Quality) { DepthOfFieldStrength = DepthOfFieldStrength });
		camera.FocusDistance = DepthOfFieldEnabled ? FocusDistance : null;
	}
}
