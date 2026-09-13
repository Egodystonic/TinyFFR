using System;
using System.Text;

namespace Egodystonic.TinyFFR;

/// <summary>
/// A static class holding miscellaneous helper methods for working with <see cref="Span{T}"/>/<see cref="ReadOnlySpan{T}"/>, primarily garbage-free UTF-8/UTF-16 conversion and span concatenation.
/// </summary>
public static class SpanUtils {
	/// <summary>
	/// Returns the number of UTF-16 <see langword="char"/>s required to represent <paramref name="utf8Src"/>.
	/// </summary>
	/// <param name="utf8Src">A span of UTF-8-encoded bytes.</param>
	public static int GetUtf16Length(ReadOnlySpan<byte> utf8Src) => Encoding.UTF8.GetCharCount(utf8Src);
	/// <summary>
	/// Converts <paramref name="utf8Src"/> from UTF-8 to UTF-16, writing the result into <paramref name="dest"/>.
	/// </summary>
	/// <param name="dest">The destination to write the converted characters into. Must be at least <see cref="GetUtf16Length"/> characters long for <paramref name="utf8Src"/>.</param>
	/// <param name="utf8Src">A span of UTF-8-encoded bytes.</param>
	/// <returns>The portion of <paramref name="dest"/> that was written to.</returns>
	public static ReadOnlySpan<char> ConvertUtf8ToUtf16(Span<char> dest, ReadOnlySpan<byte> utf8Src) => dest[..Encoding.UTF8.GetChars(utf8Src, dest)];

	/// <summary>
	/// Returns the combined length of every given span, i.e. the length required of a <c>dest</c> span passed to the corresponding <see cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/> overload.
	/// </summary>
	/// <param name="a">The first span.</param>
	/// <param name="b">The second span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b) => a.Length + b.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c) => a.Length + b.Length + c.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d) => a.Length + b.Length + c.Length + d.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e) => a.Length + b.Length + c.Length + d.Length + e.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	/// <param name="g">The seventh span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length;
	/// <inheritdoc cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	/// <param name="g">The seventh span.</param>
	/// <param name="h">The eighth span.</param>
	public static int GetConcatenatedLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g, ReadOnlySpan<T> h) => a.Length + b.Length + c.Length + d.Length + e.Length + f.Length + g.Length + h.Length;

	/// <summary>
	/// Writes every given span into <paramref name="dest"/> in order, one after another.
	/// </summary>
	/// <param name="dest">The destination to write into. Must be at least as long as the corresponding <see cref="GetConcatenatedLength{T}(ReadOnlySpan{T},ReadOnlySpan{T})"/> overload would report for the same spans.</param>
	/// <param name="a">The first span.</param>
	/// <param name="b">The second span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest); dest = dest[e.Length..];
		f.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	/// <param name="g">The seventh span.</param>
	public static void Concatenate<T>(Span<T> dest, ReadOnlySpan<T> a, ReadOnlySpan<T> b, ReadOnlySpan<T> c, ReadOnlySpan<T> d, ReadOnlySpan<T> e, ReadOnlySpan<T> f, ReadOnlySpan<T> g) {
		a.CopyTo(dest); dest = dest[a.Length..];
		b.CopyTo(dest); dest = dest[b.Length..];
		c.CopyTo(dest); dest = dest[c.Length..];
		d.CopyTo(dest); dest = dest[d.Length..];
		e.CopyTo(dest); dest = dest[e.Length..];
		f.CopyTo(dest); dest = dest[f.Length..];
		g.CopyTo(dest);
	}
	/// <inheritdoc cref="Concatenate{T}(Span{T},ReadOnlySpan{T},ReadOnlySpan{T})"/>
	/// <param name="c">The third span.</param>
	/// <param name="d">The fourth span.</param>
	/// <param name="e">The fifth span.</param>
	/// <param name="f">The sixth span.</param>
	/// <param name="g">The seventh span.</param>
	/// <param name="h">The eighth span.</param>
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