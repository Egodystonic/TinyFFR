// Created on 2024-02-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
class GeometryUtilsTest {
	const float TestTolerance = 0.01f;
	readonly string _typeName = "Curie";
	readonly (string Name, float Value) _paramA = ("AgeMonths", 6f);
	readonly (string Name, float Value) _paramB = ("WeightKg", 11.5f);
	readonly (string Name, float Value) _paramC = ("Paws", 4f);

	[SetUp]
	public void SetUpTest() { }

	[TearDown]
	public void TearDownTest() { }

	string CreateExpectedString(int paramsCount, string? format, IFormatProvider? provider) {
		if (paramsCount is < 1 or > 3) throw new ArgumentException();
		var result = _typeName + GeometryUtils.ParameterStartToken +
					_paramA.Name + GeometryUtils.ParameterKeyValueSeparatorToken + _paramA.Value.ToString(format, provider);
		if (paramsCount > 1) {
			result += GeometryUtils.ParameterSeparatorToken + _paramB.Name + GeometryUtils.ParameterKeyValueSeparatorToken + _paramB.Value.ToString(format, provider);
		}
		if (paramsCount > 2) {
			result += GeometryUtils.ParameterSeparatorToken + _paramC.Name + GeometryUtils.ParameterKeyValueSeparatorToken + _paramC.Value.ToString(format, provider);
		}
		result += GeometryUtils.ParameterEndToken;
		return result;
	}

	[Test]
	public void ShouldCorrectlyCreateStandardToString() {
		void AssertAllArities(string? format, IFormatProvider? provider) {
			Assert.AreEqual(
				CreateExpectedString(1, format, provider),
				GeometryUtils.StandardizedToString(format, provider, _typeName, _paramA)
			);
			Assert.AreEqual(
				CreateExpectedString(2, format, provider),
				GeometryUtils.StandardizedToString(format, provider, _typeName, _paramA, _paramB)
			);
			Assert.AreEqual(
				CreateExpectedString(3, format, provider),
				GeometryUtils.StandardizedToString(format, provider, _typeName, _paramA, _paramB, _paramC)
			);
		}

		AssertAllArities(null, null);
		AssertAllArities("N0", null);
		AssertAllArities("N10", null);
		AssertAllArities(null, CultureInfo.InvariantCulture);
		AssertAllArities(null, CultureInfo.CreateSpecificCulture("tr-TR"));
		AssertAllArities("####", CultureInfo.CreateSpecificCulture("tr-TR"));
	}

	[Test]
	public void ShouldCorrectlyImplementStandardTryFormat() {
		void AssertArity(int numParams, string? format, IFormatProvider? provider) {
			bool Invoke(Span<char> d, out int c) {
				switch (numParams) {
					case 1:
						return GeometryUtils.StandardizedTryFormat(d, out c, format, provider, _typeName, _paramA);
					case 2:
						return GeometryUtils.StandardizedTryFormat(d, out c, format, provider, _typeName, _paramA, _paramB);
					case 3:
						return GeometryUtils.StandardizedTryFormat(d, out c, format, provider, _typeName, _paramA, _paramB, _paramC);
					default: throw new ArgumentException();
				}
			}

			var expectedStr = CreateExpectedString(numParams, format, provider);
			var dest = new char[expectedStr.Length * 2];
			Assert.AreEqual(false, Invoke(dest[..(expectedStr.Length - 1)], out var charsWritten));
			Assert.LessOrEqual(charsWritten, expectedStr.Length - 1);
			Assert.AreEqual(true, Invoke(dest, out charsWritten));
			Assert.AreEqual(charsWritten, expectedStr.Length);
			Assert.AreEqual(expectedStr, new String(dest[..expectedStr.Length]));
		}

		void AssertAllArities(string? format, IFormatProvider? provider) {
			AssertArity(1, format, provider);
			AssertArity(2, format, provider);
			AssertArity(3, format, provider);
		}

		AssertAllArities(null, null);
		AssertAllArities("N0", null);
		AssertAllArities("N10", null);
		AssertAllArities(null, CultureInfo.InvariantCulture);
		AssertAllArities(null, CultureInfo.CreateSpecificCulture("tr-TR"));
		AssertAllArities("####", CultureInfo.CreateSpecificCulture("tr-TR"));
	}

	[Test]
	public void ShouldCorrectlyImplementStandardParse() {
		void AssertAllArities(string? format, IFormatProvider? provider) {
			GeometryUtils.StandardizedParse(CreateExpectedString(1, format, provider), provider, out float oneArityA);
			GeometryUtils.StandardizedParse(CreateExpectedString(2, format, provider), provider, out float twoArityA, out float twoArityB);
			GeometryUtils.StandardizedParse(CreateExpectedString(3, format, provider), provider, out float threeArityA, out float threeArityB, out float threeArityC);

			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), oneArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), twoArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), threeArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramB.Value.ToString(format, provider), provider), twoArityB, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramB.Value.ToString(format, provider), provider), threeArityB, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramC.Value.ToString(format, provider), provider), threeArityC, TestTolerance);
		}

		AssertAllArities(null, null);
		AssertAllArities("N0", null);
		AssertAllArities("N10", null);
		AssertAllArities(null, CultureInfo.InvariantCulture);
		AssertAllArities(null, CultureInfo.CreateSpecificCulture("tr-TR"));
		AssertAllArities("####", CultureInfo.CreateSpecificCulture("tr-TR"));
	}

	[Test]
	public void ShouldCorrectlyImplementStandardTryParse() {
		void AssertAllArities(string? format, IFormatProvider? provider) {
			var oneAritySuccess = GeometryUtils.StandardizedTryParse(CreateExpectedString(1, format, provider), provider, out float oneArityA);
			var twoAritySuccess = GeometryUtils.StandardizedTryParse(CreateExpectedString(2, format, provider), provider, out float twoArityA, out float twoArityB);
			var threeAritySuccess = GeometryUtils.StandardizedTryParse(CreateExpectedString(3, format, provider), provider, out float threeArityA, out float threeArityB, out float threeArityC);

			Assert.AreEqual(true, oneAritySuccess);
			Assert.AreEqual(true, twoAritySuccess);
			Assert.AreEqual(true, threeAritySuccess);
			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), oneArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), twoArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramA.Value.ToString(format, provider), provider), threeArityA, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramB.Value.ToString(format, provider), provider), twoArityB, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramB.Value.ToString(format, provider), provider), threeArityB, TestTolerance);
			Assert.AreEqual(Single.Parse(_paramC.Value.ToString(format, provider), provider), threeArityC, TestTolerance);
		}

		AssertAllArities(null, null);
		AssertAllArities("N0", null);
		AssertAllArities("N10", null);
		AssertAllArities(null, CultureInfo.InvariantCulture);
		AssertAllArities(null, CultureInfo.CreateSpecificCulture("tr-TR"));
		AssertAllArities("####", CultureInfo.CreateSpecificCulture("tr-TR"));
	}
}