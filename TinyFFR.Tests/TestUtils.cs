// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

static class TestUtils {
	public static void AssertToleranceEquals<T>(T expected, T actual, float tolerance) where T : IToleranceEquatable<T> {
		Assert.IsTrue(
			expected.Equals(actual, tolerance),
			$"Expected and actual value were not within tolerance of {tolerance}" + Environment.NewLine +
			$"Expected value: {expected}" + Environment.NewLine +
			$"Actual value: {actual}"
		);
	}
}