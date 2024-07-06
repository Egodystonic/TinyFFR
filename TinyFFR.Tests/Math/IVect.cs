// Created on 2023-10-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2023

using System.Globalization;
using System.Numerics;

namespace Egodystonic.TinyFFR;

class VectTestStubType : IVect {
	public float X { get; init; }
	public float Y { get; init; }
	public float Z { get; init; }
	public Vector3 ToVector3() => new(X, Y, Z);
	public void Deconstruct(out float x, out float y, out float z) {
		x = X;
		y = Y;
		z = Z;
	}

	public static Vector3 InvokeParseVector3String(ReadOnlySpan<char> s, IFormatProvider? provider) => IVect.ParseVector3String(s, provider);
	public static bool InvokeTryParseVector3String(ReadOnlySpan<char> s, IFormatProvider? provider, out Vector3 result) => IVect.TryParseVector3String(s, provider, out result);
	public static implicit operator VectTestStubType((float X, float Y, float Z) tuple) => new() { X = tuple.X, Y = tuple.Y, Z = tuple.Z };

	public float this[Axis axis] => throw new NotImplementedException();
	public XYPair<float> this[Axis first, Axis second] => throw new NotImplementedException();
	public Vect AsVect() => new(X, Y, Z);
}

[TestFixture]
// ReSharper disable once InconsistentNaming Tests for IVect interface
class IVectTest {
	[Test]
	public void ShouldCorrectlyParseVector3Strings() {
		var testProvider = CultureInfo.InvariantCulture;

		void AssertSuccess(string input, Vector3 expectedValue) {
			Assert.AreEqual(expectedValue, VectTestStubType.InvokeParseVector3String(input, testProvider));
			Assert.IsTrue(VectTestStubType.InvokeTryParseVector3String(input, testProvider, out var result));
			Assert.AreEqual(expectedValue, result);
		}

		void AssertFail(string input) {
			Assert.Catch(() => VectTestStubType.InvokeParseVector3String(input, testProvider));
			Assert.IsFalse(VectTestStubType.InvokeTryParseVector3String(input, testProvider, out _));
		}

		AssertFail("");
		AssertFail("<>");
		AssertFail("1, 2, 3");
		AssertFail("<1, 2, 3");
		AssertFail("1, 2, 3>");
		AssertFail("<1, 2>");
		AssertFail("<1, 2,>");
		AssertFail("<1, 2, >");
		AssertFail("<1 2 3>");
		AssertFail("<a, 1, 2>");
		AssertFail("<, 1, 2>");
		AssertFail("<1, c, 2>");
		AssertFail("<1, 2, ->");
		AssertSuccess("<1, 2, 3>", new(1f, 2f, 3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<1.1, 2.2, 3.3>", new(1.1f, 2.2f, 3.3f));
		AssertSuccess("<1,2,3>", new(1f, 2f, 3f));
		AssertSuccess("<-1.1, 2.2,3.3>", new(-1.1f, 2.2f, 3.3f));
	}
}