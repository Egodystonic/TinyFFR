using System;

namespace Egodystonic.TinyFFR;

[TestFixture]
class AnimationWrapStyleTest {
	const float TestTolerance = 0.001f;
	
	[Test]
	public void ShouldCorrectlyApplyWrapping() {
		const float DefaultDuration = 2f;
		
		void AssertInput(float expectation, AnimationWrapStyle wrapStyle, float input) {
			AssertToleranceEquals(expectation, wrapStyle.ApplyToTimePoint(input, DefaultDuration), TestTolerance);
		}
			
		AssertInput(0f, AnimationWrapStyle.Loop, 0f);
		AssertInput(0.5f, AnimationWrapStyle.Loop, 0.5f);
		AssertInput(0f, AnimationWrapStyle.Loop, 2f);
		AssertInput(1f, AnimationWrapStyle.Loop, 3f);
		AssertInput(1.5f, AnimationWrapStyle.Loop, -0.5f);
		AssertInput(0f, AnimationWrapStyle.Loop, -2f);
		AssertInput(1f, AnimationWrapStyle.Loop, -3f);
		
		AssertInput(0f, AnimationWrapStyle.Once, 0f);
		AssertInput(0.5f, AnimationWrapStyle.Once, 0.5f);
		AssertInput(2f, AnimationWrapStyle.Once, 2f);
		AssertInput(2f, AnimationWrapStyle.Once, 3f);
		AssertInput(0f, AnimationWrapStyle.Once, -0.5f);
		AssertInput(0f, AnimationWrapStyle.Once, -2f);
		
		AssertInput(0f, AnimationWrapStyle.LoopPingPonged, 0f);
		AssertInput(0.5f, AnimationWrapStyle.LoopPingPonged, 0.5f);
		AssertInput(2f, AnimationWrapStyle.LoopPingPonged, 2f);
		AssertInput(1.5f, AnimationWrapStyle.LoopPingPonged, 2.5f);
		AssertInput(1f, AnimationWrapStyle.LoopPingPonged, 3f);
		AssertInput(0.5f, AnimationWrapStyle.LoopPingPonged, 3.5f);
		AssertInput(0f, AnimationWrapStyle.LoopPingPonged, 4f);
		AssertInput(1f, AnimationWrapStyle.LoopPingPonged, 5f);
		AssertInput(0.5f, AnimationWrapStyle.LoopPingPonged, -0.5f);
		AssertInput(2f, AnimationWrapStyle.LoopPingPonged, -2f);
		AssertInput(1.5f, AnimationWrapStyle.LoopPingPonged, -2.5f);
		AssertInput(1f, AnimationWrapStyle.LoopPingPonged, -3f);
		
		AssertInput(0f, AnimationWrapStyle.OncePingPonged, 0f);
		AssertInput(0.5f, AnimationWrapStyle.OncePingPonged, 0.5f);
		AssertInput(2f, AnimationWrapStyle.OncePingPonged, 2f);
		AssertInput(1.5f, AnimationWrapStyle.OncePingPonged, 2.5f);
		AssertInput(1f, AnimationWrapStyle.OncePingPonged, 3f);
		AssertInput(0f, AnimationWrapStyle.OncePingPonged, 4f);
		AssertInput(0f, AnimationWrapStyle.OncePingPonged, 5f);
		AssertInput(0f, AnimationWrapStyle.OncePingPonged, -2f);
		AssertInput(0f, AnimationWrapStyle.OncePingPonged, -0.5f);
	}
}
