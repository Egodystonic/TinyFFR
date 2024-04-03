// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

namespace Egodystonic.TinyFFR;

static class TestUtils {
	public static void AssertToleranceEquals<T>(T expected, T actual, float tolerance) where T : IToleranceEquatable<T> {
		// This was originally an Assert.IsTrue but the cost of creating the string for each invocation regardless of whether or not we should fail was too much
		if (expected.Equals(actual, tolerance)) return;
		var expectedString = (expected as IDescriptiveStringProvider)?.ToStringDescriptive() ?? expected.ToString();
		var actualString = (actual as IDescriptiveStringProvider)?.ToStringDescriptive() ?? actual.ToString();
		Assert.Fail(
			$"Expected and actual value were not within tolerance of {tolerance}" + System.Environment.NewLine +
			$"\tExpected value: {expectedString}" + System.Environment.NewLine +
			$"\tActual value: {actualString}"
		);
	}

	public static void AssertToleranceNotEquals<T>(T expected, T actual, float tolerance) where T : IToleranceEquatable<T> {
		if (!expected.Equals(actual, tolerance)) return;
		var expectedString = (expected as IDescriptiveStringProvider)?.ToStringDescriptive() ?? expected.ToString();
		var actualString = (actual as IDescriptiveStringProvider)?.ToStringDescriptive() ?? actual.ToString();
		Assert.Fail(
			$"Expected and actual value were equal within tolerance of {tolerance}" + System.Environment.NewLine +
			$"\tExpected value: {expectedString}" + System.Environment.NewLine +
			$"\tActual value: {actualString}"
		);
	}

	public static void AssertToleranceEquals<T>(T? expected, T? actual, float tolerance) where T : struct, IToleranceEquatable<T> {
		// This was originally an Assert.IsTrue but the cost of creating the string for each invocation regardless of whether or not we should fail was too much
		if (expected == null && actual == null) return;
		else if (expected != null && actual != null && expected.Value.Equals(actual.Value, tolerance)) return;
		var expectedString = expected.HasValue ? ((expected.Value as IDescriptiveStringProvider)?.ToStringDescriptive() ?? expected.Value.ToString()) : "<null>";
		var actualString = actual.HasValue ? ((actual.Value as IDescriptiveStringProvider)?.ToStringDescriptive() ?? actual.Value.ToString()) : "<null>";
		Assert.Fail(
			$"Expected and actual value were not within tolerance of {tolerance}" + System.Environment.NewLine +
			$"\tExpected value: {expectedString}" + System.Environment.NewLine +
			$"\tActual value: {actualString}"
		);
	}

	public static void AssertToleranceNotEquals<T>(T? expected, T? actual, float tolerance) where T : struct, IToleranceEquatable<T> {
		if (expected.HasValue != actual.HasValue) return;
		if (expected.HasValue && !expected.Value.Equals(actual!.Value, tolerance)) return;
		var expectedString = expected.HasValue ? ((expected.Value as IDescriptiveStringProvider)?.ToStringDescriptive() ?? expected.Value.ToString()) : "<null>";
		var actualString = actual.HasValue ? ((actual.Value as IDescriptiveStringProvider)?.ToStringDescriptive() ?? actual.Value.ToString()) : "<null>";
		Assert.Fail(
			$"Expected and actual value were equal within tolerance of {tolerance}" + System.Environment.NewLine +
			$"\tExpected value: {expectedString}" + System.Environment.NewLine +
			$"\tActual value: {actualString}"
		);
	}
}