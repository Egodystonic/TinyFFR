namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshAnimationWrapStyleTest {
	const float TestTolerance = 0.001f;
	
	[Test]
	public void ShouldCorrectlyApplyWrapping() {
		const float DefaultDuration = 2f;
		
		void AssertInput(float expectation, MeshAnimationWrapStyle wrapStyle, float input) {
			AssertToleranceEquals(expectation, wrapStyle.ApplyToTimePoint(input, DefaultDuration), TestTolerance);
		}
			
		AssertInput(0f, MeshAnimationWrapStyle.Loop, 0f);
		AssertInput(0.5f, MeshAnimationWrapStyle.Loop, 0.5f);
		AssertInput(0f, MeshAnimationWrapStyle.Loop, 2f);
		AssertInput(1f, MeshAnimationWrapStyle.Loop, 3f);
		AssertInput(1.5f, MeshAnimationWrapStyle.Loop, -0.5f);
		AssertInput(0f, MeshAnimationWrapStyle.Loop, -2f);
		AssertInput(1f, MeshAnimationWrapStyle.Loop, -3f);
		
		AssertInput(0f, MeshAnimationWrapStyle.Once, 0f);
		AssertInput(0.5f, MeshAnimationWrapStyle.Once, 0.5f);
		AssertInput(2f, MeshAnimationWrapStyle.Once, 2f);
		AssertInput(2f, MeshAnimationWrapStyle.Once, 3f);
		AssertInput(0f, MeshAnimationWrapStyle.Once, -0.5f);
		AssertInput(0f, MeshAnimationWrapStyle.Once, -2f);
		
		AssertInput(0f, MeshAnimationWrapStyle.LoopPingPonged, 0f);
		AssertInput(0.5f, MeshAnimationWrapStyle.LoopPingPonged, 0.5f);
		AssertInput(2f, MeshAnimationWrapStyle.LoopPingPonged, 2f);
		AssertInput(1.5f, MeshAnimationWrapStyle.LoopPingPonged, 2.5f);
		AssertInput(1f, MeshAnimationWrapStyle.LoopPingPonged, 3f);
		AssertInput(0.5f, MeshAnimationWrapStyle.LoopPingPonged, 3.5f);
		AssertInput(0f, MeshAnimationWrapStyle.LoopPingPonged, 4f);
		AssertInput(1f, MeshAnimationWrapStyle.LoopPingPonged, 5f);
		AssertInput(0.5f, MeshAnimationWrapStyle.LoopPingPonged, -0.5f);
		AssertInput(2f, MeshAnimationWrapStyle.LoopPingPonged, -2f);
		AssertInput(1.5f, MeshAnimationWrapStyle.LoopPingPonged, -2.5f);
		AssertInput(1f, MeshAnimationWrapStyle.LoopPingPonged, -3f);
		
		AssertInput(0f, MeshAnimationWrapStyle.OncePingPonged, 0f);
		AssertInput(0.5f, MeshAnimationWrapStyle.OncePingPonged, 0.5f);
		AssertInput(2f, MeshAnimationWrapStyle.OncePingPonged, 2f);
		AssertInput(1.5f, MeshAnimationWrapStyle.OncePingPonged, 2.5f);
		AssertInput(1f, MeshAnimationWrapStyle.OncePingPonged, 3f);
		AssertInput(0f, MeshAnimationWrapStyle.OncePingPonged, 4f);
		AssertInput(0f, MeshAnimationWrapStyle.OncePingPonged, 5f);
		AssertInput(0f, MeshAnimationWrapStyle.OncePingPonged, -2f);
		AssertInput(0f, MeshAnimationWrapStyle.OncePingPonged, -0.5f);
	}
}
