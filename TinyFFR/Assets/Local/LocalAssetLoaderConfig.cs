// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxAssetFilePathLengthChars = 1 << 29;
	public const int MaxMaxAnimationAndNodeNameLengthChars = 1 << 29;
	public const int MaxMaxAssetVertexIndexBufferSizeBytes = 1 << 29;
	public const int MaxMaxSkeletalAnimationChannelKeyframeCount = 1 << 29;
	public const int MaxMaxSkeletalAnimationNodeCount = 1 << 29;
	public const int MaxMaxKtxFileBufferSizeBytes = 1 << 29;
	public const int MaxMaxEmbeddedAssetTextureFileSizeBytes = 16_384 * 16_384 * 4; // Matches max in native_impl_asset_loader.cpp
	public const int DefaultMaxAssetFilePathLengthChars = 2048;
	public const int DefaultMaxAnimationAndNodeNameLengthChars = 2048;
	public const int DefaultMaxSkeletalAnimationChannelKeyframeCount = 4096;
	public const int DefaultMaxSkeletalAnimationNodeCount = 32768;
	public const int DefaultMaxAssetVertexIndexBufferSizeBytes = 1_000_000 * MeshVertex.ExpectedSerializedSize; // 1m vertex mesh
	public const int DefaultMaxKtxFileBufferSizeBytes = 256 * 1024 * 1024; // 256 MB
	public const int DefaultMaxEmbeddedAssetTextureFileSizeBytes = 8192 * 8192 * 4; // 8k image; 256MB
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

	public int MaxSkeletalAnimationChannelKeyframeCount {
		get;
		init {
			if (value is <= 0 or > MaxMaxSkeletalAnimationChannelKeyframeCount) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max skeletal animation channel keyframe count must be between 1 and {MaxMaxSkeletalAnimationChannelKeyframeCount}.");
			}
			field = value;
		}
	} = DefaultMaxSkeletalAnimationChannelKeyframeCount;

	public int MaxSkeletalAnimationNodeCount {
		get;
		init {
			if (value is <= 0 or > MaxMaxSkeletalAnimationNodeCount) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max skeletal animation node count must be between 1 and {MaxMaxSkeletalAnimationNodeCount}.");
			}
			field = value;
		}
	} = DefaultMaxSkeletalAnimationNodeCount;

	public int MaxAssetVertexIndexBufferSizeBytes {
		get;
		init {
			if (value is <= 0 or > MaxMaxAssetVertexIndexBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset vertex/index buffer size must be between 1 and {MaxMaxAssetVertexIndexBufferSizeBytes}.");
			}
			field = value;
		}
	} = DefaultMaxAssetVertexIndexBufferSizeBytes;

	public int MaxKtxFileBufferSizeBytes {
		get;
		init {
			if (value is <= 0 or > MaxMaxKtxFileBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max KTX file buffer size must be between 1 and {MaxMaxKtxFileBufferSizeBytes}.");
			}
			field = value;
		}
	} = DefaultMaxKtxFileBufferSizeBytes;

	public int MaxEmbeddedAssetTextureFileSizeBytes {
		get;
		init {
			if (value is <= 0 or > MaxMaxEmbeddedAssetTextureFileSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max embedded asset texture file size must be between 1 and {MaxMaxEmbeddedAssetTextureFileSizeBytes}.");
			}
			field = value;
		}
	} = DefaultMaxEmbeddedAssetTextureFileSizeBytes;

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