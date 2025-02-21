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
	static abstract TexelType Type { get; }
}
public interface ITexel<TSelf> : ITexel, IByteSpanSerializable<TSelf> where TSelf : unmanaged, ITexel<TSelf> {
	public TSelf WithInvertedChannelIfPresent(int channelIndex);
}
public interface IConversionSupplyingTexel<TSelf, in TOther> : ITexel<TSelf> where TSelf : unmanaged, IConversionSupplyingTexel<TSelf, TOther> {
	public static abstract TSelf ConvertFrom(TOther o);
}