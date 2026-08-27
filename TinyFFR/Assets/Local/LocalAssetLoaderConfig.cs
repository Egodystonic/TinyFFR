// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxAssetFilePathLengthChars = 1 << 29;
	public const int MaxMaxAnimationAndNodeNameLengthChars = 1 << 29;
	public const int DefaultMaxAssetFilePathLengthChars = 2048;
	public const int DefaultMaxAnimationAndNodeNameLengthChars = 2048;
	public const int DefaultMaxCachedTextMeshesPerFont = 64;
	public static readonly TimeSpan DefaultMaxHdrProcessingTime = TimeSpan.FromMinutes(15d);

	public int MaxAssetFilePathLengthChars {
		get;
		init {
			if (value is <= 0 or > MaxMaxAssetFilePathLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset file path length must be between 1 and {MaxMaxAssetFilePathLengthChars}.");
			}
			field = value;
		}
	} = DefaultMaxAssetFilePathLengthChars;

	public int MaxAnimationAndNodeNameLengthChars {
		get;
		init {
			if (value is <= 0 or > MaxMaxAnimationAndNodeNameLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max animation/bone name length must be between 1 and {MaxMaxAnimationAndNodeNameLengthChars}.");
			}
			field = value;
		}
	} = DefaultMaxAnimationAndNodeNameLengthChars;

	public int MaxCachedTextMeshesPerFont {
		get;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max cached text meshes per font must be non-negative.");
			}
			field = value;
		}
	} = DefaultMaxCachedTextMeshesPerFont;

	public TimeSpan MaxHdrProcessingTime {
		get;
		init {
			if (value < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max HDR processing time must be positive or zero.");
			}
			field = value;
		}
	} = DefaultMaxHdrProcessingTime;
}
