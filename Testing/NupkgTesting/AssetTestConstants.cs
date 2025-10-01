// Created on 2025-05-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using Egodystonic.TinyFFR.Assets.Materials;

namespace Egodystonic.TinyFFR.Testing.Nupkg;

public static class AssetTestConstants {
	// These values were sampled/taken from an external paint program
	public static readonly Dictionary<int, TexelRgb24> _expectedSampledAlbedoPixelValues = new() {
		[1024 * 0000 + 0000] = new(0, 0, 0),
		[1024 * 1023 + 0000] = new(66, 56, 41),
		[1024 * 1023 + 1023] = new(122, 110, 89),
		[1024 * 0000 + 1023] = new(0, 0, 0),
	};
	public static readonly Dictionary<int, TexelRgb24> _expectedSampledNormalPixelValues = new() {
		[1024 * 0180 + 0374] = new(127, 127, 255),
		[1024 * 0345 + 0786] = new(127, 228, 205),
		[1024 * 0412 + 1001] = new(133, 126, 255),
		[1024 * 0497 + 0284] = new(245, 123, 176),
	};
	public static readonly Dictionary<int, TexelRgb24> _expectedSampledSpecularPixelValues = new() {
		[1024 * 0187 + 0344] = new(237, 237, 237),
		[1024 * 0270 + 0360] = new(23, 23, 23),
		[1024 * 0803 + 0373] = new(159, 159, 159),
		[1024 * 1023 + 1023] = new(0, 0, 0),
	};

	public static void AssertTexelSamples(Span<TexelRgb24> readData, Dictionary<int, TexelRgb24> expectation) {
		foreach (var kvp in expectation) {
			if (kvp.Value != readData[kvp.Key]) throw new InvalidOperationException($"Failed at texel {kvp.Key}.");
		}
	}
}