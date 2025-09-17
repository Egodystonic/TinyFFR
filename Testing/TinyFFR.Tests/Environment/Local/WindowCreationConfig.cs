using System;
using Egodystonic.TinyFFR.Resources;
using NSubstitute;
using static Egodystonic.TinyFFR.ConfigStructTestUtils;

namespace Egodystonic.TinyFFR.Environment.Local;

[TestFixture]
class WindowCreationConfigTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyConvertToAndFromHeapStorageFormat() {
		var displayAImplSub = Substitute.For<IDisplayImplProvider>();
		displayAImplSub.IsValid(Arg.Any<ResourceHandle<Display>>()).Returns(true);
		var displayBImplSub = Substitute.For<IDisplayImplProvider>();
		displayBImplSub.IsValid(Arg.Any<ResourceHandle<Display>>()).Returns(true);
		var displayA = new Display(123U, displayAImplSub);
		var displayB = new Display(456U, displayBImplSub);

		var testConfigA = new WindowCreationConfig {
			Display = displayA,
			FullscreenStyle = WindowFullscreenStyle.Fullscreen,
			Position = (123, 456),
			Size = (100, 200),
			Title = "Aa Aa"
		};
		var testConfigB = new WindowCreationConfig {
			Display = displayB,
			FullscreenStyle = WindowFullscreenStyle.NotFullscreen,
			Position = (-4, 0),
			Size = (10, 20),
			Title = "BBBbbb"
		};

		static void ComparisonFunc(WindowCreationConfig expected, WindowCreationConfig actual) {
			Assert.AreEqual(expected.Display, actual.Display);
			Assert.AreEqual(expected.FullscreenStyle, actual.FullscreenStyle);
			Assert.AreEqual(expected.Position, actual.Position);
			Assert.AreEqual(expected.Size, actual.Size);
			Assert.AreEqual(expected.Title.ToString(), actual.Title.ToString());
		}

		AssertRoundTripHeapStorage(testConfigA, ComparisonFunc);
		AssertRoundTripHeapStorage(testConfigB, ComparisonFunc);

		AssertHeapSerializationWithObjects<WindowCreationConfig>()
			.Resource(displayA)
			.String("Aa Aa")
			.Obj(new XYPair<int>(123, 456))
			.Obj(new XYPair<int>(100, 200))
			.Int((int) WindowFullscreenStyle.Fullscreen)
			.For(testConfigA);

		AssertHeapSerializationWithObjects<WindowCreationConfig>()
			.Resource(displayB)
			.String("BBBbbb")
			.Obj(new XYPair<int>(-4, 0))
			.Obj(new XYPair<int>(10, 20))
			.Int((int) WindowFullscreenStyle.NotFullscreen)
			.For(testConfigB);

		AssertPropertiesAccountedFor<WindowCreationConfig>()
			.Including(nameof(WindowCreationConfig.Display))
			.Including(nameof(WindowCreationConfig.FullscreenStyle))
			.Including(nameof(WindowCreationConfig.Position))
			.Including(nameof(WindowCreationConfig.Size))
			.Including(nameof(WindowCreationConfig.Title))
			.End();
	}
}