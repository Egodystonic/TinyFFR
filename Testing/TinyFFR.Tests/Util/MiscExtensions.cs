namespace Egodystonic.TinyFFR;

[TestFixture]
class MiscExtensionsTest {
	const float TestTolerance = 0.001f;

	[Test]
	public void ShouldCorrectlyConvertTimeSpanToDeltaTime() {
		AssertToleranceEquals(1f, TimeSpan.FromSeconds(1).AsDeltaTime(), TestTolerance);
		AssertToleranceEquals(0.016f, TimeSpan.FromMilliseconds(16).AsDeltaTime(), TestTolerance);
		AssertToleranceEquals(60f, TimeSpan.FromMinutes(1).AsDeltaTime(), TestTolerance);
		AssertToleranceEquals(0f, TimeSpan.Zero.AsDeltaTime(), TestTolerance);
		AssertToleranceEquals(0.5f, TimeSpan.FromSeconds(0.5).AsDeltaTime(), TestTolerance);
		AssertToleranceEquals(-1.0f, TimeSpan.FromSeconds(-1).AsDeltaTime(), TestTolerance);
	}
}
