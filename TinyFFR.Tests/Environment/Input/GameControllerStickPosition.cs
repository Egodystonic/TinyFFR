// Created on 2024-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024


namespace Egodystonic.TinyFFR.Environment.Input;

[TestFixture]
class GameControllerStickPositionTest {
	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	IReadOnlyDictionary<Orientation2D, GameControllerStickPosition> GetProportionalDirectionals(float proportion) {
		return Enum.GetValues<Orientation2D>().ToDictionary(
			o => o,
			o => {
				var max = GameControllerStickPosition.FromMaxOrientation(o);
				return new GameControllerStickPosition((short) (max.RawDisplacementHorizontal * proportion), (short) (max.RawDisplacementVertical * proportion));
			}
		);
	}

	[Test]
	public void ShouldCorrectlyNormalizeOffset() {
		Assert.AreEqual(0f, new GameControllerStickPosition(0, 0).DisplacementHorizontal);
		Assert.AreEqual(0f, new GameControllerStickPosition(0, 0).DisplacementVertical);

		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).DisplacementHorizontal);
		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).DisplacementVertical);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).DisplacementHorizontal);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).DisplacementVertical);

		Assert.AreEqual(-0.66f, new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.66f)).DisplacementHorizontal, 0.001f);
		Assert.AreEqual(-0.66f, new GameControllerStickPosition((short) (Int16.MinValue * 0.66f), (short) (Int16.MinValue * 0.66f)).DisplacementVertical, 0.001f);
		Assert.AreEqual(0.66f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.66f), (short) (Int16.MaxValue * 0.66f)).DisplacementHorizontal, 0.001f);
		Assert.AreEqual(0.66f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.66f), (short) (Int16.MaxValue * 0.66f)).DisplacementVertical, 0.001f);

		Assert.AreEqual(-0.33f, new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * 0.33f)).DisplacementHorizontal, 0.001f);
		Assert.AreEqual(-0.33f, new GameControllerStickPosition((short) (Int16.MinValue * 0.33f), (short) (Int16.MinValue * 0.33f)).DisplacementVertical, 0.001f);
		Assert.AreEqual(0.33f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.33f)).DisplacementHorizontal, 0.001f);
		Assert.AreEqual(0.33f, new GameControllerStickPosition((short) (Int16.MaxValue * 0.33f), (short) (Int16.MaxValue * 0.33f)).DisplacementVertical, 0.001f);
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
	public void ShouldCorrectlyIncorporateDeadzoneInToNormalizedDisplacement() {
		Assert.AreEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight, (short) AnalogDisplacementLevel.Slight).DisplacementHorizontalWithDeadzone);
		Assert.AreEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight, (short) AnalogDisplacementLevel.Slight).DisplacementVerticalWithDeadzone);
		Assert.AreEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight, (short) AnalogDisplacementLevel.Slight).DisplacementWithDeadzone);

		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight + 1, (short) AnalogDisplacementLevel.Slight + 1).DisplacementHorizontalWithDeadzone);
		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight + 1, (short) AnalogDisplacementLevel.Slight + 1).DisplacementVerticalWithDeadzone);
		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight + 1, (short) AnalogDisplacementLevel.Slight + 1).DisplacementWithDeadzone);

		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).DisplacementHorizontalWithDeadzone);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).DisplacementVerticalWithDeadzone);
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).DisplacementWithDeadzone);
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
			Assert.AreEqual(new XYPair<float>(directional.DisplacementHorizontal, directional.DisplacementVertical), actual);
		}
		foreach (var directional in GetProportionalDirectionals(0.6f).Values) {
			var actual = directional.AsXYPair(0.5f);
			Assert.AreEqual(new XYPair<float>(directional.DisplacementHorizontal, directional.DisplacementVertical), actual);
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
				Orientation2D.None => null,
				Orientation2D.Right => 0f,
				Orientation2D.UpRight => 45f,
				Orientation2D.Up => 90f,
				Orientation2D.UpLeft => 135f,
				Orientation2D.Left => 180f,
				Orientation2D.DownLeft => 225f,
				Orientation2D.Down => 270f,
				Orientation2D.DownRight => 315f,
				_ => throw new ArgumentOutOfRangeException()
			};
			if (expected == null || actual == null) Assert.AreEqual(expected, actual);
			else AssertToleranceEquals(expected.Value, actual.Value, 0.1f);
		}
		foreach (var kvp in GetProportionalDirectionals(0.6f)) {
			var actual = kvp.Value.GetPolarAngle(0.5f);
			Angle? expected = kvp.Key switch {
				Orientation2D.None => null,
				Orientation2D.Right => 0f,
				Orientation2D.UpRight => 45f,
				Orientation2D.Up => 90f,
				Orientation2D.UpLeft => 135f,
				Orientation2D.Left => 180f,
				Orientation2D.DownLeft => 225f,
				Orientation2D.Down => 270f,
				Orientation2D.DownRight => 315f,
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
			Assert.AreEqual(kvp.Key != Orientation2D.None, kvp.Value.IsOutsideDeadzone(0.5f));
			Assert.AreEqual(MathF.Abs(kvp.Value.DisplacementHorizontal) > 0.5f, kvp.Value.IsHorizontalOffsetOutsideDeadzone(0.5f));
			Assert.AreEqual(MathF.Abs(kvp.Value.DisplacementVertical) > 0.5f, kvp.Value.IsVerticalOffsetOutsideDeadzone(0.5f));
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
			Assert.AreEqual(kvp.Key, kvp.Value.GetOrientation(0.5f));
			Assert.AreEqual(((HorizontalOrientation2D) kvp.Key) & (HorizontalOrientation2D.Right | HorizontalOrientation2D.Left), kvp.Value.GetHorizontalOrientation(0.5f));
			Assert.AreEqual(((VerticalOrientation2D) kvp.Key) & (VerticalOrientation2D.Down | VerticalOrientation2D.Up), kvp.Value.GetVerticalOrientation(0.5f));
		}

		foreach (var kvp in GetProportionalDirectionals(0.4f)) {
			Assert.AreEqual(Orientation2D.None, kvp.Value.GetOrientation(0.5f));
			Assert.AreEqual(HorizontalOrientation2D.None, kvp.Value.GetHorizontalOrientation(0.5f));
			Assert.AreEqual(VerticalOrientation2D.None, kvp.Value.GetVerticalOrientation(0.5f));
		}
	}

	[Test]
	public void ShouldCorrectlyExtractRawValues() {
		foreach (var pos in Enum.GetValues<Orientation2D>().Select(GameControllerStickPosition.FromMaxOrientation)) {
			pos.GetRawDisplacementValues(out var actualRawHorizontal, out var actualRawVertical);
			Assert.AreEqual(pos.RawDisplacementHorizontal, actualRawHorizontal);
			Assert.AreEqual(pos.RawDisplacementVertical, actualRawVertical);
		}
	}
}