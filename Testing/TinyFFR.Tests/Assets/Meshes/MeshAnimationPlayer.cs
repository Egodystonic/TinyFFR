namespace Egodystonic.TinyFFR.Assets.Meshes;

[TestFixture]
class MeshAnimationPlayerTest {
	const float TestTolerance = 0.001f;
	
	[Test]
	public void ShouldCorrectlyApplyWrapping() {
		const float DefaultDuration = 2f;
		
		void AssertInput(float expectation, MeshAnimationTimestampWrapStyle wrapStyle, float input) {
			AssertToleranceEquals(expectation, MeshAnimationPlayer.ApplyWrapping(input, wrapStyle, DefaultDuration), TestTolerance);
		}
		
		AssertInput(0f, MeshAnimationTimestampWrapStyle.None, 0f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.None, 1f);
		AssertInput(3f, MeshAnimationTimestampWrapStyle.None, 3f);
		AssertInput(-1f, MeshAnimationTimestampWrapStyle.None, -1f);
		
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Loop, 0f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.Loop, 0.5f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Loop, 2f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.Loop, 3f);
		AssertInput(1.5f, MeshAnimationTimestampWrapStyle.Loop, -0.5f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Loop, -2f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.Loop, -3f);
		
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Clamp, 0f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.Clamp, 0.5f);
		AssertInput(2f, MeshAnimationTimestampWrapStyle.Clamp, 2f);
		AssertInput(2f, MeshAnimationTimestampWrapStyle.Clamp, 3f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Clamp, -0.5f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.Clamp, -2f);
		
		AssertInput(0f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 0f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 0.5f);
		AssertInput(2f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 2f);
		AssertInput(1.5f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 2.5f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 3f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 3.5f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 4f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.LoopPingPonged, 5f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.LoopPingPonged, -0.5f);
		AssertInput(2f, MeshAnimationTimestampWrapStyle.LoopPingPonged, -2f);
		AssertInput(1.5f, MeshAnimationTimestampWrapStyle.LoopPingPonged, -2.5f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.LoopPingPonged, -3f);
		
		AssertInput(0f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 0f);
		AssertInput(0.5f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 0.5f);
		AssertInput(2f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 2f);
		AssertInput(1.5f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 2.5f);
		AssertInput(1f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 3f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 4f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.ClampPingPonged, 5f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.ClampPingPonged, -2f);
		AssertInput(0f, MeshAnimationTimestampWrapStyle.ClampPingPonged, -0.5f);
	}
}
