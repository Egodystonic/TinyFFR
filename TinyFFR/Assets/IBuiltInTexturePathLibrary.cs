// Created on 2025-11-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

namespace Egodystonic.TinyFFR.Assets;

public interface IBuiltInTexturePathLibrary {
	ReadOnlySpan<char> DefaultColorMap { get; }
	ReadOnlySpan<char> DefaultNormalMap { get; }
	ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicMap { get; }
	ReadOnlySpan<char> DefaultOcclusionRoughnessMetallicReflectanceMap { get; }
	ReadOnlySpan<char> DefaultAbsorptionTransmissionMap { get; }
	ReadOnlySpan<char> DefaultEmissiveMap { get; }
	ReadOnlySpan<char> DefaultAnisotropyMap { get; }
	ReadOnlySpan<char> DefaultClearCoatMap { get; }

	ReadOnlySpan<char> Rgba100Percent { get; }
	ReadOnlySpan<char> Rgba90Percent { get; }
	ReadOnlySpan<char> Rgba80Percent { get; }
	ReadOnlySpan<char> Rgba70Percent { get; }
	ReadOnlySpan<char> Rgba60Percent { get; }
	ReadOnlySpan<char> Rgba50Percent { get; }
	ReadOnlySpan<char> Rgba40Percent { get; }
	ReadOnlySpan<char> Rgba30Percent { get; }
	ReadOnlySpan<char> Rgba20Percent { get; }
	ReadOnlySpan<char> Rgba10Percent { get; }
	ReadOnlySpan<char> Rgba0Percent { get; }

	ReadOnlySpan<char> White { get; }
	ReadOnlySpan<char> Black { get; }
	ReadOnlySpan<char> Red { get; }
	ReadOnlySpan<char> Green { get; }
	ReadOnlySpan<char> Blue { get; }
	ReadOnlySpan<char> RedGreen { get; }
	ReadOnlySpan<char> GreenBlue { get; }
	ReadOnlySpan<char> RedBlue { get; }

	ReadOnlySpan<char> WhiteOpaque { get; }
	ReadOnlySpan<char> BlackOpaque { get; }
	ReadOnlySpan<char> RedOpaque { get; }
	ReadOnlySpan<char> GreenOpaque { get; }
	ReadOnlySpan<char> BlueOpaque { get; }
	ReadOnlySpan<char> RedGreenOpaque { get; }
	ReadOnlySpan<char> GreenBlueOpaque { get; }
	ReadOnlySpan<char> RedBlueOpaque { get; }

	ReadOnlySpan<char> WhiteTransparent { get; }
	ReadOnlySpan<char> BlackTransparent { get; }
	ReadOnlySpan<char> RedTransparent { get; }
	ReadOnlySpan<char> GreenTransparent { get; }
	ReadOnlySpan<char> BlueTransparent { get; }
	ReadOnlySpan<char> RedGreenTransparent { get; }
	ReadOnlySpan<char> GreenBlueTransparent { get; }
	ReadOnlySpan<char> RedBlueTransparent { get; }
}