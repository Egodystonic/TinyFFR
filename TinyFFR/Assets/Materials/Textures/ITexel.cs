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
}
public interface IConversionSupplyingTexel<TSelf, in TOther> : ITexel<TSelf> where TSelf : unmanaged, IConversionSupplyingTexel<TSelf, TOther> {
	static abstract TSelf ConvertFrom(TOther o);
}
public interface ITexel<TSelf, out TChannel> : ITexel<TSelf> where TSelf : unmanaged, ITexel<TSelf, TChannel> {
	TChannel this[int index] { get; }
}
public interface IThreeChannelTexel<TSelf, out TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IThreeChannelTexel<TSelf, TChannel> {
	static int ITexel.ChannelCount => 3;
}
public interface IFourChannelTexel<TSelf, out TChannel> : ITexel<TSelf, TChannel> where TSelf : unmanaged, IFourChannelTexel<TSelf, TChannel> {
	static int ITexel.ChannelCount => 4;
}
public interface IThreeByteChannelTexel<TSelf> : IThreeChannelTexel<TSelf, byte> where TSelf : unmanaged, IThreeByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgb24;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 3;
}
public interface IFourByteChannelTexel<TSelf> : IFourChannelTexel<TSelf, byte> where TSelf : unmanaged, IFourByteChannelTexel<TSelf> {
	static TexelType ITexel.BlitType => TexelType.Rgba32;
	static int IFixedLengthByteSpanSerializable<TSelf>.SerializationByteSpanLength => 4;
}