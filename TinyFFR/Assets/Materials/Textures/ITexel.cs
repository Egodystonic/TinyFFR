// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

/// <summary>
/// Identifies the memory layout a texel type can be copied to and from directly, without conversion.
/// </summary>
/// <remarks>
/// Two texel types sharing a blit type have the same layout in memory, which is what allows a span of one to be reinterpreted
/// as a span of the other. Note that this describes layout only and is not an identity: several distinct texel types can report
/// the same blit type.
/// </remarks>
public enum TexelType {
	/// <summary>
	/// A layout that is neither of the two standard ones, and so can only be converted, never reinterpreted.
	/// </summary>
	Other,
	/// <summary>
	/// Three consecutive bytes: red, green and blue.
	/// </summary>
	Rgb24,
	/// <summary>
	/// Four consecutive bytes: red, green, blue and alpha.
	/// </summary>
	Rgba32
}

/// <summary>
/// The non-generic base of the texel type hierarchy, describing a single element of a texture's data.
/// </summary>
/// <remarks>
/// A "texel" is to a texture what a pixel is to an image; the distinction is that a texel's channels do not necessarily
/// represent colour at all (e.g. in an ORM map they hold three unrelated surface properties).
/// </remarks>
public interface ITexel {
	/// <summary>
	/// The memory layout this texel type can be copied to and from directly.
	/// </summary>
	static abstract TexelType BlitType { get; }
	/// <summary>
	/// How many channels this texel type holds.
	/// </summary>
	static abstract int ChannelCount { get; }
}
/// <summary>
/// A texel type that knows its own concrete type, which is what allows texels to be produced, blended and converted generically.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface ITexel<TSelf> : ITexel, IBlendable<TSelf>, IFixedLengthByteSpanSerializable<TSelf> where TSelf : unmanaged, ITexel<TSelf> {
	/// <summary>
	/// Returns a copy of this texel with the given channel inverted, so that its strongest value becomes its weakest.
	/// </summary>
	/// <remarks>
	/// Asking for a channel this texel type does not have returns the texel unchanged rather than throwing.
	/// </remarks>
	/// <param name="channelIndex">Which channel to invert, counting from <c>0</c> for red.</param>
	TSelf WithInvertedChannelIfPresent(int channelIndex);
	/// <summary>
	/// Returns a copy of this texel with its channels rearranged.
	/// </summary>
	/// <remarks>
	/// Each parameter names the source channel for one output channel, so passing <see cref="ColorChannel.G"/> as
	/// <paramref name="redSource"/> copies green in to red. All four are read before any is written, so channels can be
	/// exchanged. Naming a channel this texel type does not have leaves the corresponding output channel unchanged.
	/// </remarks>
	/// <param name="redSource">Which channel supplies the output's red channel.</param>
	/// <param name="greenSource">Which channel supplies the output's green channel.</param>
	/// <param name="blueSource">Which channel supplies the output's blue channel.</param>
	/// <param name="alphaSource">Which channel supplies the output's alpha channel.</param>
	TSelf SwizzlePresentChannels(ColorChannel redSource, ColorChannel greenSource, ColorChannel blueSource, ColorChannel alphaSource);
	/// <summary>
	/// Returns a copy of this texel with its colour channels multiplied by its alpha channel.
	/// </summary>
	/// <remarks>
	/// Materials that blend with the scene expect their colour data in this form. A texel type with no alpha channel returns
	/// itself unchanged.
	/// </remarks>
	TSelf WithPremultipliedAlpha();
	/// <summary>
	/// Copies a span of these texels in to a span of another texel type, converting as required.
	/// </summary>
	/// <typeparam name="TOther">The texel type to convert to.</typeparam>
	/// <param name="src">The texels to copy from.</param>
	/// <param name="dest">The span to copy in to. Must be at least as long as <paramref name="src"/>.</param>
	/// <returns><see langword="true"/> if the conversion was possible and was performed, or <see langword="false"/> if this
	/// texel type does not know how to produce <typeparamref name="TOther"/>.</returns>
	static abstract bool TryCoerceSpan<TOther>(ReadOnlySpan<TSelf> src, Span<TOther> dest) where TOther : unmanaged, ITexel<TOther>;
	/// <summary>
	/// Copies a span of another texel type in to a span of these texels, converting as required.
	/// </summary>
	/// <typeparam name="TOther">The texel type to convert from.</typeparam>
	/// <param name="src">The texels to copy from.</param>
	/// <param name="dest">The span to copy in to. Must be at least as long as <paramref name="src"/>.</param>
	/// <param name="mergeWithExistingDestinationData">Whether to preserve any channels of the destination that the source
	/// cannot supply. Leaving this <see langword="false"/> overwrites them, which for an alpha channel means making every texel
	/// fully opaque.</param>
	/// <returns><see langword="true"/> if the conversion was possible and was performed, or <see langword="false"/> if this
	/// texel type does not know how to be produced from <typeparamref name="TOther"/>.</returns>
	static abstract bool TryCoerceSpanFrom<TOther>(ReadOnlySpan<TOther> src, Span<TSelf> dest, bool mergeWithExistingDestinationData = false) where TOther : unmanaged, ITexel<TOther>;
}
/// <summary>
/// A texel type that can convert to and from one particular other type.
/// </summary>
/// <remarks>
/// A texel type declares one of these for each type it interoperates with, which is what lets the generic texture machinery
/// move data between representations without knowing either type concretely.
/// </remarks>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
/// <typeparam name="TOther">The type this one converts to and from.</typeparam>
public interface IConversionSupplyingTexel<TSelf, TOther> : ITexel<TSelf> where TSelf : unmanaged, IConversionSupplyingTexel<TSelf, TOther> {
	/// <summary>
	/// Converts a <typeparamref name="TOther"/> in to a <typeparamref name="TSelf"/>.
	/// </summary>
	/// <param name="o">The value to convert.</param>
	static abstract TSelf ConvertFrom(TOther o);
	/// <summary>
	/// Converts this texel in to a <typeparamref name="TOther"/>.
	/// </summary>
	/// <remarks>
	/// Where <typeparamref name="TOther"/> holds fewer channels than this type, the surplus ones are discarded.
	/// </remarks>
	[Pure]
	TOther Convert();
}
/// <summary>
/// A texel type whose channels are all of one particular type, and which can therefore be indexed channel by channel.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
/// <typeparam name="TChannel">The type of each of this texel's channels.</typeparam>
public interface ITexel<TSelf, TChannel> : ITexel<TSelf> where TSelf : unmanaged, ITexel<TSelf, TChannel> where TChannel : struct {
	/// <summary>
	/// Gets the value of the channel at the given position, counting from <c>0</c> for red.
	/// </summary>
	/// <param name="index">Which channel to read. Must be in the range <c>0 &lt;= index &lt; ChannelCount</c>.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> names no channel of this texel
	/// type.</exception>
	TChannel this[int index] { get; }
	/// <summary>
	/// Gets the value of the named channel.
	/// </summary>
	/// <param name="channel">Which channel to read. Must be a channel this texel type actually has.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channel"/> names no channel of this texel
	/// type.</exception>
	TChannel this[ColorChannel channel] { get; }
	/// <summary>
	/// The lowest value a channel of this texel type can take.
	/// </summary>
	static abstract TChannel MinChannelValue { get; }
	/// <summary>
	/// The highest value a channel of this texel type can take.
	/// </summary>
	static abstract TChannel MaxChannelValue { get; }

	/// <summary>
	/// Constructs a texel from the given channel values, ignoring any beyond the number this type holds.
	/// </summary>
	/// <remarks>
	/// This lets one piece of generic code supply four channel values and have them used correctly by three-channel and
	/// four-channel texel types alike.
	/// </remarks>
	/// <param name="channelArgs">The channel values, in the order red, green, blue, alpha. Must hold at least as many entries as
	/// this texel type has channels.</param>
	static abstract TSelf ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs);
	/// <summary>
	/// Gets the value of the channel at the given position, or <see langword="null"/> if this texel type has no such channel.
	/// </summary>
	/// <param name="index">Which channel to read, counting from <c>0</c> for red.</param>
	TChannel? TryGetChannel(int index) => index < 0 || index >= TSelf.ChannelCount ? null : this[index];
	/// <summary>
	/// Gets the value of the named channel, or <see langword="null"/> if this texel type has no such channel.
	/// </summary>
	/// <param name="channel">Which channel to read.</param>
	TChannel? TryGetChannel(ColorChannel channel) {
		return channel switch {
			ColorChannel.R when TSelf.ChannelCount >= 1 => this[channel],
			ColorChannel.G when TSelf.ChannelCount >= 2 => this[channel],
			ColorChannel.B when TSelf.ChannelCount >= 3 => this[channel],
			ColorChannel.A when TSelf.ChannelCount >= 4 => this[channel],
			_ => null
		};
	}
}
/// <summary>
/// A texel type holding exactly three channels: red, green and blue.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
/// <typeparam name="TChannel">The type of each of this texel's channels.</typeparam>
public interface IThreeChannelTexel<TSelf, TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IThreeChannelTexel<TSelf, TChannel> where TChannel : struct {
	static int ITexel.ChannelCount => 3;
	/// <summary>
	/// Constructs a texel from the given three channel values.
	/// </summary>
	/// <param name="r">The red channel's value.</param>
	/// <param name="g">The green channel's value.</param>
	/// <param name="b">The blue channel's value.</param>
	static abstract TSelf ConstructFrom(TChannel r, TChannel g, TChannel b);
	static TSelf ITexel<TSelf, TChannel>.ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs) {
		return TSelf.ConstructFrom(channelArgs[0], channelArgs[1], channelArgs[2]);
	}
}
/// <summary>
/// A texel type holding exactly four channels: red, green, blue and alpha.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
/// <typeparam name="TChannel">The type of each of this texel's channels.</typeparam>
public interface IFourChannelTexel<TSelf, TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IFourChannelTexel<TSelf, TChannel> where TChannel : struct {
	static int ITexel.ChannelCount => 4;
	/// <summary>
	/// Constructs a texel from the given four channel values.
	/// </summary>
	/// <param name="r">The red channel's value.</param>
	/// <param name="g">The green channel's value.</param>
	/// <param name="b">The blue channel's value.</param>
	/// <param name="a">The alpha channel's value.</param>
	static abstract TSelf ConstructFrom(TChannel r, TChannel g, TChannel b, TChannel a);
	static TSelf ITexel<TSelf, TChannel>.ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs) {
		return TSelf.ConstructFrom(channelArgs[0], channelArgs[1], channelArgs[2], channelArgs[3]);
	}
}
/// <summary>
/// A three-channel texel type whose channels are each a single byte, and which therefore has the
/// <see cref="TexelType.Rgb24"/> layout.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface IThreeByteChannelTexel<TSelf> : IThreeChannelTexel<TSelf, byte> where TSelf : unmanaged, IThreeByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgb24;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 3;
	static byte ITexel<TSelf, byte>.MinChannelValue => Byte.MinValue;
	static byte ITexel<TSelf, byte>.MaxChannelValue => Byte.MaxValue;
}
/// <summary>
/// A four-channel texel type whose channels are each a single byte, and which therefore has the
/// <see cref="TexelType.Rgba32"/> layout.
/// </summary>
/// <typeparam name="TSelf">The implementing type itself.</typeparam>
public interface IFourByteChannelTexel<TSelf> : IFourChannelTexel<TSelf, byte> where TSelf : unmanaged, IFourByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgba32;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 4;
	static byte ITexel<TSelf, byte>.MinChannelValue => Byte.MinValue;
	static byte ITexel<TSelf, byte>.MaxChannelValue => Byte.MaxValue;
}
