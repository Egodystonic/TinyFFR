// Created on 2023-10-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Reflection;
using System.Runtime.InteropServices;

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

	public static unsafe void AssertStructLayout<T>() where T : unmanaged {
		var expectedSize = typeof(T).StructLayoutAttribute!.Size;
		Assert.AreEqual(expectedSize, sizeof(T));
		var stackValue = default(T);
		Assert.AreEqual(expectedSize, MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in stackValue)).Length);
	}

	public static void AssertMirrorMethod<TSelf, TOther>(Func<TSelf, TOther, object?> a, Func<TOther, TSelf, object?> b) where TSelf : IRandomizable<TSelf> where TOther : IRandomizable<TOther> {
		const int NumIterations = 100;
		for (var i = 0; i < NumIterations; ++i) {
			var self = TSelf.Random();
			var other = TOther.Random();
			try {
				try {
					Assert.AreEqual(a(self, other), b(other, self));
				}
				catch (Exception e) when (e is not AssertionException) {
					Exception? aException = null;
					try {
						a(self, other);
					}
					catch (Exception aE) {
						aException = aE;
					}

					Exception? bException = null;
					try {
						b(other, self);
					}
					catch (Exception bE) {
						bException = bE;
					}

					if (aException == null || bException == null) throw;
					if (aException.GetType() != bException.GetType()) throw;
				}
			}
			catch {
				Console.WriteLine($"Self: {self}");
				Console.WriteLine($"Other: {other}");
				throw;
			}
		}
	}

	public static void AssertMirrorMethod<TSelf, TOther, TSelfInterface, TOtherInterface>(Func<TSelfInterface, TOther, object?> a, Func<TOtherInterface, TSelf, object?> b) where TSelf : TSelfInterface, IRandomizable<TSelf> where TOther : TOtherInterface, IRandomizable<TOther> {
		AssertMirrorMethod<TSelf, TOther>((s, o) => a(s, o), (o, s) => b(o, s));
	}

	public static void AssertMirrorMethod<TSelf, TOther>(Func<dynamic, dynamic, object?> func) where TSelf : IRandomizable<TSelf> where TOther : IRandomizable<TOther> {
		AssertMirrorMethod<TSelf, TOther>((s, o) => func(s, o), (o, s) => func(o, s));
	}

	public static void AssertMirrorMethodWithTolerance<TSelf, TOther>(Func<TSelf, TOther, object?> a, Func<TOther, TSelf, object?> b, float tolerance) where TSelf : IRandomizable<TSelf> where TOther : IRandomizable<TOther> {
		const int NumIterations = 100;
		for (var i = 0; i < NumIterations; ++i) {
			var self = TSelf.Random();
			var other = TOther.Random();
			try {
				try {
					var aResult = a(self, other);
					var bResult = b(other, self);
					Assert.AreEqual(aResult?.GetType(), bResult?.GetType());
					if (aResult == null && bResult == null) continue;
					else if (aResult is float aFloat && bResult is float bFloat) Assert.AreEqual(aFloat, bFloat, tolerance);
					else if (aResult is double aDouble && bResult is double bDouble) Assert.AreEqual(aDouble, bDouble, tolerance);
					else AssertToleranceEquals((dynamic?) aResult, (dynamic?) bResult, tolerance); 
				}
				catch (Exception e) when (e is not AssertionException) {
					Exception? aException = null;
					try {
						a(self, other);
					}
					catch (Exception aE) {
						aException = aE;
					}

					Exception? bException = null;
					try {
						b(other, self);
					}
					catch (Exception bE) {
						bException = bE;
					}

					if (aException == null || bException == null) throw;
					if (aException.GetType() != bException.GetType()) throw;
				}
			}
			catch {
				Console.WriteLine($"Self: {self}");
				Console.WriteLine($"Other: {other}");
				throw;
			}
		}
	}

	public static void AssertMirrorMethodWithTolerance<TSelf, TOther, TSelfInterface, TOtherInterface>(Func<TSelfInterface, TOther, object?> a, Func<TOtherInterface, TSelf, object?> b, float tolerance) where TSelf : TSelfInterface, IRandomizable<TSelf> where TOther : TOtherInterface, IRandomizable<TOther> {
		AssertMirrorMethodWithTolerance<TSelf, TOther>((s, o) => a(s, o), (o, s) => b(o, s), tolerance);
	}

	public static void AssertMirrorMethodWithTolerance<TSelf, TOther>(Func<dynamic, dynamic, object?> func, float tolerance) where TSelf : IRandomizable<TSelf> where TOther : IRandomizable<TOther> {
		AssertMirrorMethodWithTolerance<TSelf, TOther>((s, o) => func(s, o), (o, s) => func(o, s), tolerance);
	}
}