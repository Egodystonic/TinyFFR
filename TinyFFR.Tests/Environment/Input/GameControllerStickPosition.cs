// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024


namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class GameControllerStickPositionTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	IReadOnlyDictionary<CardinalDirection, GameControllerStickPosition> GetProportionalDirectionals(float proportion) {
		return GameControllerStickPosition.AllCardinals.ToDictionary(
			kvp => kvp.Key,
			kvp => new GameControllerStickPosition((short) (kvp.Value.HorizontalOffsetRaw * proportion), (short) (kvp.Value.VerticalOffsetRaw * proportion))
		);
	}

	[Test]
	public void ShouldCorrectlyNormalizeOffset() {
		Assert.AreEqual(0f, new GameControllerStickPosition(0, 0).HorizontalOffset);
		Assert.AreEqual(0f, new GameControllerStickPosition(0, 0).VerticalOffset);

		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).HorizontalOffset);
		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).VerticalOffset);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).HorizontalOffset);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).VerticalOffset);

		Assert.AreEqual(-0.66f, new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.66f)).HorizontalOffset, 0.001f);
		Assert.AreEqual(-0.66f, new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.66f)).VerticalOffset, 0.001f);
		Assert.AreEqual(0.66f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.66f), (short) (Int16.MaxValue * 0.66f)).HorizontalOffset, 0.001f);
		Assert.AreEqual(0.66f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.66f), (short) (Int16.MaxValue * 0.66f)).VerticalOffset, 0.001f);

		Assert.AreEqual(-0.33f, new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * 0.33f)).HorizontalOffset, 0.001f);
		Assert.AreEqual(-0.33f, new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * 0.33f)).VerticalOffset, 0.001f);
		Assert.AreEqual(0.33f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.33f)).HorizontalOffset, 0.001f);
		Assert.AreEqual(0.33f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.33f)).VerticalOffset, 0.001f);
	}

	[Test]
	public void ShouldCorrectlyNormalizeDisplacement() {
		Assert.AreEqual(0f, new GameControllerStickPosition(0, 0).Displacement);

		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).Displacement);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MinValue, 0).Displacement);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).Displacement);
		Assert.AreEqual(1f, new GameControllerStickPosition(0, Int16.MaxValue).Displacement);

		Assert.AreEqual(MathF.Sqrt(0.66f * 0.66f * 2f), new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.66f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.66f * 0.66f + 0.33f * 0.33f), new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.33f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.66f * 0.66f * 2f), new GameControllerStickPosition((short) (Int16.MaxValue * 0.66f), (short) (Int16.MaxValue * 0.66f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.66f * 0.66f + 0.33f * 0.33f), new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.66f)).Displacement, 0.001f);

		Assert.AreEqual(MathF.Sqrt(0.33f * 0.33f * 2f), new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * 0.33f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.33f * 0.33f * 2f), new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * -0.33f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.33f * 0.33f * 2f), new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.33f)).Displacement, 0.001f);
		Assert.AreEqual(MathF.Sqrt(0.33f * 0.33f * 2f), new GameControllerStickPosition((short) (Int16.MaxValue * -0.33f), (short) (Int16.MaxValue * 0.33f)).Displacement, 0.001f);
	}

	[Test]
	public void ShouldCorrectlyCalculateDisplacementLevel() {
		void AssertLevel(AnalogDisplacementLevel level, AnalogDisplacementLevel prevLevel) {
			Assert.AreEqual(level, new GameControllerStickPosition((short) level, 0).DisplacementLevel);
			Assert.AreEqual(level, new GameControllerStickPosition(0, (short) level).DisplacementLevel);
			Assert.AreEqual(level, new GameControllerStickPosition((short) -(short) level, 0).DisplacementLevel);
			Assert.AreEqual(level, new GameControllerStickPosition(0, (short) -(short) level).DisplacementLevel);

			Assert.AreEqual(prevLevel, new GameControllerStickPosition((short) (((short) level) - 1), 0).DisplacementLevel);
			Assert.AreEqual(prevLevel, new GameControllerStickPosition(0, (short) (((short) level) - 1)).DisplacementLevel);
		}

		Assert.AreEqual(AnalogDisplacementLevel.None, new GameControllerStickPosition(0, 0).DisplacementLevel);

		AssertLevel(AnalogDisplacementLevel.Slight, AnalogDisplacementLevel.None);
		AssertLevel(AnalogDisplacementLevel.Moderate, AnalogDisplacementLevel.Slight);
		AssertLevel(AnalogDisplacementLevel.Full, AnalogDisplacementLevel.Moderate);
	}

	[Test]
	public void ShouldCorrectlyConvertToXYPair() {
		foreach (var directional in GetProportionalDirectionals(1f).Values) {
			var actual = directional.AsXYPair(0.5f);
			Assert.AreEqual(new XYPair<float>(directional.HorizontalOffset, directional.VerticalOffset), actual);
		}
		foreach (var directional in GetProportionalDirectionals(0.6f).Values) {
			var actual = directional.AsXYPair(0.5f);
			Assert.AreEqual(new XYPair<float>(directional.HorizontalOffset, directional.VerticalOffset), actual);
		}
		foreach (var directional in GetProportionalDirectionals(0.4f).Values) {
			var actual = directional.AsXYPair(0.5f);
			Assert.AreEqual(XYPair<float>.Zero, actual);
		}
		foreach (var directional in GetProportionalDirectionals(0f).Values) {
			var actual = directional.AsXYPair(0.5f);
			Assert.AreEqual(XYPair<float>.Zero, actual);
		}
	}

	[Test]
	public void ShouldCorrectlyReturnPolarAngle() {
		foreach (var kvp in GetProportionalDirectionals(1f)) {
			var actual = kvp.Value.GetPolarAngle(0.5f);
			Angle? expected = kvp.Key switch {
				CardinalDirection.None => null,
				CardinalDirection.Right => 0f,
				CardinalDirection.UpRight => 45f,
				CardinalDirection.Up => 90f,
				CardinalDirection.UpLeft => 135f,
				CardinalDirection.Left => 180f,
				CardinalDirection.DownLeft => 225f,
				CardinalDirection.Down => 270f,
				CardinalDirection.DownRight => 315f,
				_ => throw new ArgumentOutOfRangeException()
			};
			if (expected == null || actual == null) Assert.AreEqual(expected, actual);
			else AssertToleranceEquals(expected.Value, actual.Value, 0.1f);
		}
		foreach (var kvp in GetProportionalDirectionals(0.6f)) {
			var actual = kvp.Value.GetPolarAngle(0.5f);
			Angle? expected = kvp.Key switch {
				CardinalDirection.None => null,
				CardinalDirection.Right => 0f,
				CardinalDirection.UpRight => 45f,
				CardinalDirection.Up => 90f,
				CardinalDirection.UpLeft => 135f,
				CardinalDirection.Left => 180f,
				CardinalDirection.DownLeft => 225f,
				CardinalDirection.Down => 270f,
				CardinalDirection.DownRight => 315f,
				_ => throw new ArgumentOutOfRangeException()
			};
			if (expected == null || actual == null) Assert.AreEqual(expected, actual);
			else AssertToleranceEquals(expected.Value, actual.Value, 0.1f);
		}
		foreach (var kvp in GetProportionalDirectionals(0.4f)) {
			var actual = kvp.Value.GetPolarAngle(0.5f);
			Assert.AreEqual(null, actual);
		}
		foreach (var kvp in GetProportionalDirectionals(0f)) {
			var actual = kvp.Value.GetPolarAngle(0.5f);
			Assert.AreEqual(null, actual);
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineDeadzoneCondition() {
		foreach (var kvp in GetProportionalDirectionals(1f)) {
			Assert.AreEqual(kvp.Key != CardinalDirection.None, kvp.Value.IsOutsideDeadzone(0.5f));
			Assert.AreEqual(MathF.Abs(kvp.Value.HorizontalOffset) > 0.5f, kvp.Value.IsHorizontalOffsetOutsideDeadzone(0.5f));
			Assert.AreEqual(MathF.Abs(kvp.Value.VerticalOffset) > 0.5f, kvp.Value.IsVerticalOffsetOutsideDeadzone(0.5f));
		}

		foreach (var kvp in GetProportionalDirectionals(0.4f)) {
			Assert.AreEqual(false, kvp.Value.IsOutsideDeadzone(0.5f));
			Assert.AreEqual(false, kvp.Value.IsHorizontalOffsetOutsideDeadzone(0.5f));
			Assert.AreEqual(false, kvp.Value.IsVerticalOffsetOutsideDeadzone(0.5f));
		}
	}

	[Test]
	public void ShouldCorrectlyDetermineDirection() {
		foreach (var kvp in GetProportionalDirectionals(1f)) {
			Assert.AreEqual(kvp.Key, kvp.Value.GetDirection(0.5f));
			Assert.AreEqual(((HorizontalDirection) kvp.Key) & (HorizontalDirection.Right | HorizontalDirection.Left), kvp.Value.GetHorizontalDirection(0.5f));
			Assert.AreEqual(((VerticalDirection) kvp.Key) & (VerticalDirection.Down | VerticalDirection.Up), kvp.Value.GetVerticalDirection(0.5f));
		}

		foreach (var kvp in GetProportionalDirectionals(0.4f)) {
			Assert.AreEqual(CardinalDirection.None, kvp.Value.GetDirection(0.5f));
			Assert.AreEqual(HorizontalDirection.None, kvp.Value.GetHorizontalDirection(0.5f));
			Assert.AreEqual(VerticalDirection.None, kvp.Value.GetVerticalDirection(0.5f));
		}
	}

	[Test]
	public void ShouldCorrectlyExtractRawValues() {
		foreach (var kvp in GameControllerStickPosition.AllCardinals) {
			kvp.Value.GetRawOffsetValues(out var actualRawHorizontal, out var actualRawVertical);
			Assert.AreEqual(kvp.Value.HorizontalOffsetRaw, actualRawHorizontal);
			Assert.AreEqual(kvp.Value.VerticalOffsetRaw, actualRawVertical);
		}
	}
}