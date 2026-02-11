// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxAssetFilePathLengthChars = 1 << 29;
	public const int MaxMaxAssetVertexIndexBufferSizeBytes = 1 << 29;
	public const int MaxMaxKtxFileBufferSizeBytes = 1 << 29;
	public const int MaxMaxEmbeddedAssetTextureFileSizeBytes = 16_384 * 16_384 * 4; // Matches max in native_impl_asset_loader.cpp
	public const int DefaultMaxAssetFilePathLengthChars = 2048;
	public const int DefaultMaxAssetVertexIndexBufferSizeBytes = 1_000_000 * MeshVertex.ExpectedSerializedSize; // 1m vertex mesh
	public const int DefaultMaxKtxFileBufferSizeBytes = 256 * 1024 * 1024; // 256 MB
	public const int DefaultMaxEmbeddedAssetTextureFileSizeBytes = 8192 * 8192 * 4; // 8k image; 256MB
	public static readonly TimeSpan DefaultMaxHdrProcessingTime = TimeSpan.FromMinutes(2d);

	readonly int _maxAssetFilePathLengthChars = DefaultMaxAssetFilePathLengthChars;
	public int MaxAssetFilePathLengthChars {
		get => _maxAssetFilePathLengthChars;
		init {
			if (value is <= 0 or > MaxMaxAssetFilePathLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset file path length must be between 1 and {MaxMaxAssetFilePathLengthChars}.");
			}
			_maxAssetFilePathLengthChars = value;
		}
	}

	readonly int _maxAssetVertexIndexBufferSizeBytes = DefaultMaxAssetVertexIndexBufferSizeBytes;
	public int MaxAssetVertexIndexBufferSizeBytes {
		get => _maxAssetVertexIndexBufferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxAssetVertexIndexBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset vertex/index buffer size must be between 1 and {MaxMaxAssetVertexIndexBufferSizeBytes}.");
			}
			_maxAssetVertexIndexBufferSizeBytes = value;
		}
	}

	readonly int _maxKtxFileBufferSizeBytes = DefaultMaxKtxFileBufferSizeBytes;
	public int MaxKtxFileBufferSizeBytes {
		get => _maxKtxFileBufferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxKtxFileBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max KTX file buffer size must be between 1 and {MaxMaxKtxFileBufferSizeBytes}.");
			}
			_maxKtxFileBufferSizeBytes = value;
		}
	}
	
	readonly int _maxEmbeddedAssetTextureFileSizeBytes = DefaultMaxEmbeddedAssetTextureFileSizeBytes;
	public int MaxEmbeddedAssetTextureFileSizeBytes {
		get => _maxEmbeddedAssetTextureFileSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxEmbeddedAssetTextureFileSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max embedded asset texture file size must be between 1 and {MaxMaxEmbeddedAssetTextureFileSizeBytes}.");
			}
			_maxEmbeddedAssetTextureFileSizeBytes = value;
		}
	}

	readonly TimeSpan _maxHdrProcessingTime = DefaultMaxHdrProcessingTime;
	public TimeSpan MaxHdrProcessingTime {
		get => _maxHdrProcessingTime;
		init {
			if (value < TimeSpan.Zero) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max HDR processing time must be positive or zero.");
			}
			_maxHdrProcessingTime = value;
		}
	}
}