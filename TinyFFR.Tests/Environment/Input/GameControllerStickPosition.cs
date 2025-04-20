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
		short ConvertNormalizedDisplacement(float normalizedValue) => (short) (Int16.MaxValue * normalizedValue);
		// This method is trying to create a short value to put in to horizontal/vertical displacement that
		// will result in the given normalizedValue for Displacement (i.e. not the horizontal/vertical one).
		// Does not take in to account the Min()
		short GetNonCardinalToCardinalDisplacementRaw(float normalizedValue, bool negate = false) => (short) (ConvertNormalizedDisplacement(MathF.Sqrt((normalizedValue * normalizedValue) / 2f)) * (negate ? -1 : 1));

		Assert.AreEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight, (short) AnalogDisplacementLevel.Slight).GetDisplacementHorizontalWithDeadzone());
		Assert.AreEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight, (short) AnalogDisplacementLevel.Slight).GetDisplacementVerticalWithDeadzone());
		Assert.AreEqual(0f, new GameControllerStickPosition(GetNonCardinalToCardinalDisplacementRaw(0.15f), GetNonCardinalToCardinalDisplacementRaw(0.15f)).GetDisplacementWithDeadzone());

		Assert.AreEqual(0f, new GameControllerStickPosition(-(short) AnalogDisplacementLevel.Slight, -(short) AnalogDisplacementLevel.Slight).GetDisplacementHorizontalWithDeadzone());
		Assert.AreEqual(0f, new GameControllerStickPosition(-(short) AnalogDisplacementLevel.Slight, -(short) AnalogDisplacementLevel.Slight).GetDisplacementVerticalWithDeadzone());
		Assert.AreEqual(0f, new GameControllerStickPosition(GetNonCardinalToCardinalDisplacementRaw(0.15f, true), GetNonCardinalToCardinalDisplacementRaw(0.15f, true)).GetDisplacementWithDeadzone());

		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight + 1, (short) AnalogDisplacementLevel.Slight + 1).GetDisplacementHorizontalWithDeadzone());
		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) AnalogDisplacementLevel.Slight + 1, (short) AnalogDisplacementLevel.Slight + 1).GetDisplacementVerticalWithDeadzone());
		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) (GetNonCardinalToCardinalDisplacementRaw(0.15f) + 1), (short) (GetNonCardinalToCardinalDisplacementRaw(0.15f) + 1)).GetDisplacementWithDeadzone());

		Assert.AreNotEqual(0f, new GameControllerStickPosition(-((short) AnalogDisplacementLevel.Slight + 1), -((short) AnalogDisplacementLevel.Slight + 1)).GetDisplacementHorizontalWithDeadzone());
		Assert.AreNotEqual(0f, new GameControllerStickPosition(-((short) AnalogDisplacementLevel.Slight + 1), -((short) AnalogDisplacementLevel.Slight + 1)).GetDisplacementVerticalWithDeadzone());
		Assert.AreNotEqual(0f, new GameControllerStickPosition((short) (GetNonCardinalToCardinalDisplacementRaw(0.15f, true) - 1), (short) (GetNonCardinalToCardinalDisplacementRaw(0.15f, true) - 1)).GetDisplacementWithDeadzone());

		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).GetDisplacementHorizontalWithDeadzone());
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).GetDisplacementVerticalWithDeadzone());
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MaxValue, Int16.MaxValue).GetDisplacementWithDeadzone());

		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).GetDisplacementHorizontalWithDeadzone());
		Assert.AreEqual(-1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).GetDisplacementVerticalWithDeadzone());
		Assert.AreEqual(1f, new GameControllerStickPosition(Int16.MinValue, Int16.MinValue).GetDisplacementWithDeadzone());

		void TestAllThreeDisplacementTypes(float expectation, float normalizedDisplacement, float deadzone) {
			Assert.AreEqual(expectation, new GameControllerStickPosition(ConvertNormalizedDisplacement(normalizedDisplacement), 0).GetDisplacementHorizontalWithDeadzone(deadzone), 1E-3f);
			Assert.AreEqual(expectation, new GameControllerStickPosition(0, ConvertNormalizedDisplacement(normalizedDisplacement)).GetDisplacementVerticalWithDeadzone(deadzone), 1E-3f);
			Assert.AreEqual(Math.Abs(expectation), new GameControllerStickPosition(GetNonCardinalToCardinalDisplacementRaw(normalizedDisplacement), GetNonCardinalToCardinalDisplacementRaw(normalizedDisplacement)).GetDisplacementWithDeadzone(deadzone), 1E-3f);
		}

		TestAllThreeDisplacementTypes(0f, 0.4f, 0.5f);
		TestAllThreeDisplacementTypes(1f / 6f, 0.5f, 0.4f);
		TestAllThreeDisplacementTypes(0.6666f, 0.8f, 0.4f);
		TestAllThreeDisplacementTypes(0.5f, 0.5f, 0f);
		TestAllThreeDisplacementTypes(1f, 1f, 0f);
		TestAllThreeDisplacementTypes(1f, 1f, 0.9f);

		TestAllThreeDisplacementTypes(0f, -0.4f, 0.5f);
		TestAllThreeDisplacementTypes(-1f / 6f, -0.5f, 0.4f);
		TestAllThreeDisplacementTypes(-0.6666f, -0.8f, 0.4f);
		TestAllThreeDisplacementTypes(-0.5f, -0.5f, 0f);
		TestAllThreeDisplacementTypes(-1f, -1f, 0f);
		TestAllThreeDisplacementTypes(-1f, -1f, 0.9f);
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


			Assert.AreEqual(level, new GameControllerStickPosition((short) level, 0).DisplacementLevelHorizontal);
			Assert.AreEqual(level, new GameControllerStickPosition(0, (short) level).DisplacementLevelVertical);
			Assert.AreEqual(level, new GameControllerStickPosition((short) -(short) level, 0).DisplacementLevelHorizontal);
			Assert.AreEqual(level, new GameControllerStickPosition(0, (short) -(short) level).DisplacementLevelVertical);

			Assert.AreEqual(prevLevel, new GameControllerStickPosition((short) (((short) level) - 1), 0).DisplacementLevelHorizontal);
			Assert.AreEqual(prevLevel, new GameControllerStickPosition(0, (short) (((short) level) - 1)).DisplacementLevelVertical);
		}

		Assert.AreEqual(AnalogDisplacementLevel.None, new GameControllerStickPosition(0, 0).DisplacementLevel);

		AssertLevel(AnalogDisplacementLevel.Slight, AnalogDisplacementLevel.None);
		AssertLevel(AnalogDisplacementLevel.Moderate, AnalogDisplacementLevel.Slight);
		AssertLevel(AnalogDisplacementLevel.Full, AnalogDisplacementLevel.Moderate);
	}

	[Test]
	public void ShouldCorrectlyConvertToXYPair() {
		foreach (var directional in GetProportionalDirectionals(0.6f).Values) {
			Assert.AreEqual(new XYPair<float>(directional.GetDisplacementHorizontalWithDeadzone(0.4f), directional.GetDisplacementVerticalWithDeadzone(0.4f)), directional.AsXYPair(0.4f));
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
			Assert.AreEqual(MathF.Abs(kvp.Value.DisplacementHorizontal) > 0.5f, kvp.Value.IsOutsideDeadzoneHorizontal(0.5f));
			Assert.AreEqual(MathF.Abs(kvp.Value.DisplacementVertical) > 0.5f, kvp.Value.IsOutsideDeadzoneVertical(0.5f));
		}

		foreach (var kvp in GetProportionalDirectionals(0.4f)) {
			Assert.AreEqual(false, kvp.Value.IsOutsideDeadzone(MathF.Sqrt(0.32f) + 0.01f));
			Assert.AreEqual(false, kvp.Value.IsOutsideDeadzoneHorizontal(0.5f));
			Assert.AreEqual(false, kvp.Value.IsOutsideDeadzoneVertical(0.5f));
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