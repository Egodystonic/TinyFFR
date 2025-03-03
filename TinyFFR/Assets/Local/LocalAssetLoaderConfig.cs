// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets.Local;

public sealed record LocalAssetLoaderConfig {
	public const int MaxMaxShaderBufferSizeBytes = 1 << 29;
	public const int MaxMaxAssetFilePathLengthChars = 1 << 29;
	public const int MaxMaxAssetVertexIndexBufferSizeBytes = 1 << 29;
	public const int DefaultMaxShaderBufferSizeBytes = 1024 * 1024 * 1; // 1 MB
	public const int DefaultMaxAssetFilePathLengthChars = 2048;
	public const int DefaultMaxAssetVertexIndexBufferSizeBytes = 100_000 * MeshVertex.ExpectedSerializedSize; // 100k vertex mesh

	readonly int _maxShaderBufferSizeBytes = DefaultMaxShaderBufferSizeBytes;
	public int MaxShaderBufferSizeBytes {
		get => _maxShaderBufferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxShaderBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max shader buffer size must be between 1 and {MaxMaxShaderBufferSizeBytes} bytes.");
			}
			_maxShaderBufferSizeBytes = value;
		}
	}

	readonly int _maxAssetFilePathLengthChars = DefaultMaxAssetFilePathLengthChars;
	public int MaxAssetFilePathLengthChars {
		get => _maxAssetFilePathLengthChars;
		init {
			if (value is <= 0 or > MaxMaxAssetFilePathLengthChars) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset file path length must be between 1 and {MaxMaxAssetFilePathLengthChars} chars.");
			}
			_maxAssetFilePathLengthChars = value;
		}
	}

	readonly int _maxAssetVertexIndexBufferSizeBytes = DefaultMaxAssetVertexIndexBufferSizeBytes;
	public int MaxAssetVertexIndexBufferSizeBytes {
		get => _maxAssetVertexIndexBufferSizeBytes;
		init {
			if (value is <= 0 or > MaxMaxAssetVertexIndexBufferSizeBytes) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Max asset file path length must be between 1 and {MaxMaxAssetVertexIndexBufferSizeBytes} chars.");
			}
			_maxAssetVertexIndexBufferSizeBytes = value;
		}
	}
}