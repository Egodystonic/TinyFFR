using System;

namespace Egodystonic.TinyFFR;

public static class SpanUtils {
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) => a.Length + b.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c) => a.Length + b.Length + c.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d) => a.Length + b.Length + c.Length + d.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e) => a.Length + b.Length + c.Length + d.Length + e.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length;
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g, ReadOnlySpan<T> h) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length + h.Length;
	
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest); dest = dest[e.Length..];
		f.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest); dest = dest[e.Length..];
		f.CopyTo(dest); dest = dest[f.Length..];
		g.CopyTo(dest);
	}
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g, ReadOnlySpan<T> h) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest); dest = dest[e.Length..];
		f.CopyTo(dest); dest = dest[f.Length..];
		g.CopyTo(dest); dest = dest[g.Length..];
		h.CopyTo(dest);
	}
}