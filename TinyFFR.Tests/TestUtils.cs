// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

static class TestUtils {
	public static void AssertToleranceEquals<T>(T expected, T actual, float tolerance) where T : IToleranceEquatable<T> {
		// This was originally an Assert.IsTrue but the cost of creating the string for each invocation regardless of whether or not we should fail was too much
		if (expected.Equals(actual, tolerance)) return;
		Assert.Fail(
			$"Expected and actual value were not within tolerance of {tolerance}" + System.Environment.NewLine +
			$"\tExpected value: {expected}" + System.Environment.NewLine +
			$"\tActual value: {actual}"
		);
	}
}