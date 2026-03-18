namespace Egodystonic.TinyFFR;

[TestFixture]
class SpanUtilsTest {
	[Test]
	public void ShouldCorrectlyCalculateConcatenatedLength() {
		ReadOnlySpan<int> a = [1, 2, 3];
		ReadOnlySpan<int> b = [4, 5];
		ReadOnlySpan<int> c = [6];
		ReadOnlySpan<int> d = [7, 8, 9, 10];
		ReadOnlySpan<int> e = [11];
		ReadOnlySpan<int> f = [12, 13];
		ReadOnlySpan<int> g = [14];
		ReadOnlySpan<int> h = [15, 16];
		ReadOnlySpan<int> empty = [];

		Assert.AreEqual(5, SpanUtils.GetConcatenatedLength(a, b));
		Assert.AreEqual(6, SpanUtils.GetConcatenatedLength(a, b, c));
		Assert.AreEqual(10, SpanUtils.GetConcatenatedLength(a, b, c, d));
		Assert.AreEqual(11, SpanUtils.GetConcatenatedLength(a, b, c, d, e));
		Assert.AreEqual(13, SpanUtils.GetConcatenatedLength(a, b, c, d, e, f));
		Assert.AreEqual(14, SpanUtils.GetConcatenatedLength(a, b, c, d, e, f, g));
		Assert.AreEqual(16, SpanUtils.GetConcatenatedLength(a, b, c, d, e, f, g, h));

		Assert.AreEqual(3, SpanUtils.GetConcatenatedLength(a, empty));
		Assert.AreEqual(0, SpanUtils.GetConcatenatedLength(empty, empty));
		Assert.AreEqual(3, SpanUtils.GetConcatenatedLength(empty, empty, a));
	}

	[Test]
	public void ShouldCorrectlyConcatenateSpans() {
		ReadOnlySpan<int> a = [1, 2];
		ReadOnlySpan<int> b = [3];
		ReadOnlySpan<int> c = [4, 5];
		ReadOnlySpan<int> d = [6];
		ReadOnlySpan<int> e = [7, 8];
		ReadOnlySpan<int> f = [9];
		ReadOnlySpan<int> g = [10];
		ReadOnlySpan<int> h = [11, 12];
		ReadOnlySpan<int> empty = [];

		Span<int> dest2 = stackalloc int[3];
		SpanUtils.Concatenate(dest2, a, b);
		Assert.AreEqual(1, dest2[0]);
		Assert.AreEqual(2, dest2[1]);
		Assert.AreEqual(3, dest2[2]);

		Span<int> dest3 = stackalloc int[5];
		SpanUtils.Concatenate(dest3, a, b, c);
		Assert.AreEqual(1, dest3[0]);
		Assert.AreEqual(2, dest3[1]);
		Assert.AreEqual(3, dest3[2]);
		Assert.AreEqual(4, dest3[3]);
		Assert.AreEqual(5, dest3[4]);

		Span<int> dest4 = stackalloc int[6];
		SpanUtils.Concatenate(dest4, a, b, c, d);
		Assert.AreEqual(1, dest4[0]);
		Assert.AreEqual(6, dest4[5]);

		Span<int> dest5 = stackalloc int[8];
		SpanUtils.Concatenate(dest5, a, b, c, d, e);
		Assert.AreEqual(1, dest5[0]);
		Assert.AreEqual(8, dest5[7]);

		Span<int> dest6 = stackalloc int[9];
		SpanUtils.Concatenate(dest6, a, b, c, d, e, f);
		Assert.AreEqual(1, dest6[0]);
		Assert.AreEqual(9, dest6[8]);

		Span<int> dest7 = stackalloc int[10];
		SpanUtils.Concatenate(dest7, a, b, c, d, e, f, g);
		Assert.AreEqual(1, dest7[0]);
		Assert.AreEqual(10, dest7[9]);

		Span<int> dest8 = stackalloc int[12];
		SpanUtils.Concatenate(dest8, a, b, c, d, e, f, g, h);
		Assert.AreEqual(1, dest8[0]);
		Assert.AreEqual(12, dest8[11]);

		Span<int> destWithEmpty = stackalloc int[2];
		SpanUtils.Concatenate(destWithEmpty, empty, a);
		Assert.AreEqual(1, destWithEmpty[0]);
		Assert.AreEqual(2, destWithEmpty[1]);

		Span<int> destAllEmpty = stackalloc int[0];
		SpanUtils.Concatenate(destAllEmpty, empty, empty);
	}
}
