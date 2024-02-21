// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024


namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class GameControllerTriggerPositionTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	[Test]
	public void ShouldCorrectlyNormalizeDisplacement() {
		Assert.AreEqual(0f, new GameControllerTriggerPosition(0).NormalizedDisplacement);

		Assert.AreEqual(0f, new GameControllerTriggerPosition(Int16.MinValue).NormalizedDisplacement);
		Assert.AreEqual(1f, new GameControllerTriggerPosition(Int16.MaxValue).NormalizedDisplacement);

		Assert.AreEqual(0f, new GameControllerTriggerPosition((short) (Int16.MinValue * 0.66f)).NormalizedDisplacement, 0.001f);
		Assert.AreEqual(0.66f, new GameControllerTriggerPosition((short) (Int16.MaxValue * 0.66f)).NormalizedDisplacement, 0.001f);

		Assert.AreEqual(0f, new GameControllerTriggerPosition((short) (Int16.MinValue * 0.33f)).NormalizedDisplacement, 0.001f);
		Assert.AreEqual(0.33f, new GameControllerTriggerPosition((short) (Int16.MaxValue * 0.33f)).NormalizedDisplacement, 0.001f);
	}

	[Test]
	public void ShouldReturnCorrectDisplacementLevel() {
		Assert.AreEqual(AnalogDisplacementLevel.None, GameControllerTriggerPosition.Zero.DisplacementLevel);
		
		Assert.AreEqual(AnalogDisplacementLevel.None, new GameControllerTriggerPosition((short) (AnalogDisplacementLevel.Slight - 1)).DisplacementLevel);
		Assert.AreEqual(AnalogDisplacementLevel.Slight, new GameControllerTriggerPosition((short) AnalogDisplacementLevel.Slight).DisplacementLevel);

		Assert.AreEqual(AnalogDisplacementLevel.Slight, new GameControllerTriggerPosition((short) (AnalogDisplacementLevel.Moderate - 1)).DisplacementLevel);
		Assert.AreEqual(AnalogDisplacementLevel.Moderate, new GameControllerTriggerPosition((short) AnalogDisplacementLevel.Moderate).DisplacementLevel);

		Assert.AreEqual(AnalogDisplacementLevel.Moderate, new GameControllerTriggerPosition((short) (AnalogDisplacementLevel.Full - 1)).DisplacementLevel);
		Assert.AreEqual(AnalogDisplacementLevel.Full, new GameControllerTriggerPosition((short) AnalogDisplacementLevel.Full).DisplacementLevel);

		Assert.AreEqual(AnalogDisplacementLevel.Full, GameControllerTriggerPosition.Max.DisplacementLevel);
	}
}