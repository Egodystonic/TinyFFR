// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Assets.Local;

/// <summary>
/// Configures the asset loader's internal limits and caches.
/// </summary>
/// <remarks>
/// These exist to bound the memory the loader reserves up front. The defaults are ample for ordinary use; raise one only if you
/// are using especially large asset files.
/// </remarks>
public sealed record LocalAssetLoaderConfig {
	/// <summary>
	/// The largest permitted value for <see cref="MaxAssetFilePathLengthChars"/>: <c>536,870,912</c>.
	/// </summary>
	public const int MaxMaxAssetFilePathLengthChars = 1 << 29;
	/// <summary>
	/// The largest permitted value for <see cref="MaxAnimationAndNodeNameLengthChars"/>: <c>536,870,912</c>.
	/// </summary>
	public const int MaxMaxAnimationAndNodeNameLengthChars = 1 << 29;
	/// <summary>
	/// The default value for <see cref="MaxAssetFilePathLengthChars"/>: <c>2048</c>.
	/// </summary>
	public const int DefaultMaxAssetFilePathLengthChars = 2048;
	/// <summary>
	/// The default value for <see cref="MaxAnimationAndNodeNameLengthChars"/>: <c>2048</c>.
	/// </summary>
	public const int DefaultMaxAnimationAndNodeNameLengthChars = 2048;
	/// <summary>
	/// The default value for <see cref="MaxCachedTextMeshesPerFont"/>: <c>64</c>.
	/// </summary>
	public const int DefaultMaxCachedTextMeshesPerFont = 64;
	/// <summary>
	/// The default value for <see cref="MaxHdrProcessingTime"/>: 15 minutes.
	/// </summary>
	public static readonly TimeSpan DefaultMaxHdrProcessingTime = TimeSpan.FromMinutes(15d);

	/// <summary>
	/// The longest file path the asset loader will accept, in characters. Defaults to <see cref="DefaultMaxAssetFilePathLengthChars"/>: <c>2048</c>.
	/// </summary>
	/// <remarks>
	/// This sizes the buffer paths are copied in to before being handed to the native layer. Must be positive and no greater
	/// than <see cref="MaxMaxAssetFilePathLengthChars"/>.
	/// </remarks>
	public int MaxAssetFilePathLengthChars {
		get;
		init {
			if (value is <= 0 or > MaxMaxAssetFilePathLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset file path length must be between 1 and {MaxMaxAssetFilePathLengthChars}.");
			}
			field = value;
		}
	} = DefaultMaxAssetFilePathLengthChars;

	/// <summary>
	/// The longest animation or node name the asset loader will accept, in characters. Defaults to <see cref="DefaultMaxAnimationAndNodeNameLengthChars"/>: <c>2048</c>.
	/// </summary>
	/// <remarks>
	/// Must be positive and no greater than <see cref="MaxMaxAnimationAndNodeNameLengthChars"/>.
	/// </remarks>
	public int MaxAnimationAndNodeNameLengthChars {
		get;
		init {
			if (value is <= 0 or > MaxMaxAnimationAndNodeNameLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max animation/bone name length must be between 1 and {MaxMaxAnimationAndNodeNameLengthChars}.");
			}
			field = value;
		}
	} = DefaultMaxAnimationAndNodeNameLengthChars;

	/// <summary>
	/// How many prepared text meshes each font keeps before the least recently used are discarded. Defaults to <see cref="DefaultMaxCachedTextMeshesPerFont"/>: <c>64</c>.
	/// </summary>
	/// <remarks>
	/// Preparing a string builds geometry for it, so caching lets text that reappears from frame to frame be reused rather than
	/// rebuilt. Raising this trades memory for fewer rebuilds (and therefore can reduce frame stuttering in high-text-churn scenarios). Must be positive.
	/// </remarks>
	public int MaxCachedTextMeshesPerFont {
		get;
		init {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max cached text meshes per font must be non-negative.");
			}
			field = value;
		}
	} = DefaultMaxCachedTextMeshesPerFont;

	/// <summary>
	/// How long to allow for converting a high-dynamic-range image in to a backdrop before giving up. Defaults to <see cref="DefaultMaxHdrProcessingTime"/>: 15 minutes.
	/// </summary>
	/// <remarks>
	/// That conversion is slow (minutes rather than seconds for a large image), which is why the limit is generous. Must be
	/// positive.
	/// </remarks>
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
