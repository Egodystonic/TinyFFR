// Created on 2024-01-16 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Egodystonic.TinyFFR.Assets.Materials;

public enum TexelType {
	Other,
	Rgb24,
	Rgba32
}

public interface ITexel {
	static abstract TexelType BlitType { get; }
	static abstract int ChannelCount { get; }
}
public interface ITexel<TSelf> : ITexel, IFixedLengthByteSpanSerializable<TSelf> where TSelf : unmanaged, ITexel<TSelf> {
	TSelf WithInvertedChannelIfPresent(int channelIndex);
	TSelf SwizzlePresentChannels(ColorChannel redSource, ColorChannel greenSource, ColorChannel blueSource, ColorChannel alphaSource);
}
public interface IConversionSupplyingTexel<TSelf, in TOther> : ITexel<TSelf> where TSelf : unmanaged, IConversionSupplyingTexel<TSelf, TOther> {
	static abstract TSelf ConvertFrom(TOther o);
}
public interface ITexel<TSelf, TChannel> : ITexel<TSelf> where TSelf : unmanaged, ITexel<TSelf, TChannel> where TChannel : struct {
	TChannel this[int index] { get; }
	TChannel this[ColorChannel channel] { get; }
	static abstract TChannel MinChannelValue { get; }
	static abstract TChannel MaxChannelValue { get; }

	static abstract TSelf ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs);
	TChannel? TryGetChannel(int index) => index < 0 || index >= TSelf.ChannelCount ? null : this[index];
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
public interface IThreeChannelTexel<TSelf, TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IThreeChannelTexel<TSelf, TChannel> where TChannel : struct {
	static int ITexel.ChannelCount => 3;
	static abstract TSelf ConstructFrom(TChannel r, TChannel g, TChannel b);
	static TSelf ITexel<TSelf, TChannel>.ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs) {
		return TSelf.ConstructFrom(channelArgs[0], channelArgs[1], channelArgs[2]);
	} 
}
public interface IFourChannelTexel<TSelf, TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IFourChannelTexel<TSelf, TChannel> where TChannel : struct {
	static int ITexel.ChannelCount => 4;
	static abstract TSelf ConstructFrom(TChannel r, TChannel g, TChannel b, TChannel a);
	static TSelf ITexel<TSelf, TChannel>.ConstructFromIgnoringExcessArguments(params ReadOnlySpan<TChannel> channelArgs) {
		return TSelf.ConstructFrom(channelArgs[0], channelArgs[1], channelArgs[2], channelArgs[3]);
	}
}
public interface IThreeByteChannelTexel<TSelf> : IThreeChannelTexel<TSelf, byte> where TSelf : unmanaged, IThreeByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgb24;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 3;
	static byte ITexel<TSelf, byte>.MinChannelValue => Byte.MinValue;
	static byte ITexel<TSelf, byte>.MaxChannelValue => Byte.MaxValue;
}
public interface IFourByteChannelTexel<TSelf> : IFourChannelTexel<TSelf, byte> where TSelf : unmanaged, IFourByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgba32;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 4;
	static byte ITexel<TSelf, byte>.MinChannelValue => Byte.MinValue;
	static byte ITexel<TSelf, byte>.MaxChannelValue => Byte.MaxValue;
}