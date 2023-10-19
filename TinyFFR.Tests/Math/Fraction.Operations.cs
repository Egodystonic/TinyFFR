// Created on 2023-10-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;

namespace Egodystonic.TinyFFR;

[TestFixture]
partial class FractionTest {
	[Test]
	public void ShouldCorrectlyNegateFractions() {
		Assert.AreEqual(new Fraction(-3f), -new Fraction(3f));
		Assert.AreEqual(new Fraction(3f), -new Fraction(-3f));

		Assert.AreEqual(new Fraction(-3f), new Fraction(3f).Negated);
		Assert.AreEqual(new Fraction(3f), new Fraction(-3f).Negated);
	}

	[Test]
	public void ShouldCorrectlyReturnAbsoluteFractions() {
		Assert.AreEqual(new Fraction(3f), new Fraction(3f).Absolute);
		Assert.AreEqual(new Fraction(3f), new Fraction(-3f).Absolute);
	}

	[Test]
	public void ShouldCorrectlyMultiplyAndDivide() {
		void AssertIteration(Fraction fraction, float operand) {
			Assert.AreEqual(fraction.AsDecimal * operand, (fraction * operand).AsDecimal);
			Assert.AreEqual(fraction.AsDecimal * operand, (operand * fraction).AsDecimal);
			Assert.AreEqual(fraction.AsDecimal * operand, fraction.MultipliedBy(operand).AsDecimal);
			Assert.AreEqual(fraction.AsDecimal / operand, (fraction / operand).AsDecimal);
			Assert.AreEqual(fraction.AsDecimal / operand, fraction.DividedBy(operand).AsDecimal);
			Assert.AreEqual(operand / fraction.AsDecimal, operand / fraction);

			Assert.AreEqual(new Fraction(fraction.AsDecimal * operand), fraction * new Fraction(operand));
			Assert.AreEqual(new Fraction(fraction.AsDecimal * operand), fraction.MultipliedBy(new Fraction(operand)));
			Assert.AreEqual(new Fraction(fraction.AsDecimal / operand), fraction / new Fraction(operand));
			Assert.AreEqual(new Fraction(fraction.AsDecimal / operand), fraction.DividedBy(new Fraction(operand)));

			Assert.AreEqual(new Fraction(fraction.AsDecimal % operand), fraction % operand);
			Assert.AreEqual(new Fraction(fraction.AsDecimal % operand), fraction % new Fraction(operand));
			Assert.AreEqual(operand % fraction.AsDecimal, operand % fraction);
		}

		AssertIteration(0f, 0f);
		AssertIteration(1f, 1f);
		AssertIteration(1f, -1f);
		AssertIteration(-1f, 1f);
		AssertIteration(-1f, -1f);
		AssertIteration(100f, 0.1f);
		AssertIteration(0.1f, 100f);
	}
}