// Created on 2024-08-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Buffers;

namespace Egodystonic.TinyFFR.Assets.Materials;

static class TextureFactory {
	public readonly struct TemporaryTextureWriteSpace<TTexel> : IDisposable, IEquatable<TemporaryTextureWriteSpace<TTexel>> where TTexel : unmanaged, ITexel<TTexel> {
		readonly byte[] _borrowedPool;
		readonly int _sizeBytes;

		public Span<TTexel> Buffer => MemoryMarshal.Cast<byte, TTexel>(_borrowedPool.AsSpan(0, _sizeBytes));

		internal TemporaryTextureWriteSpace(byte[] borrowedPool, int sizeBytes) {
			_borrowedPool = borrowedPool;
			_sizeBytes = sizeBytes;
		}

		public void Dispose() => _bytePool.Return(_borrowedPool);

		public bool Equals(TemporaryTextureWriteSpace<TTexel> other) {
			return _borrowedPool.Equals(other._borrowedPool) && _sizeBytes == other._sizeBytes;
		}
		public override bool Equals(object? obj) => obj is TemporaryTextureWriteSpace<TTexel> other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(_borrowedPool, _sizeBytes);
		public static bool operator ==(TemporaryTextureWriteSpace<TTexel> left, TemporaryTextureWriteSpace<TTexel> right) => left.Equals(right);
		public static bool operator !=(TemporaryTextureWriteSpace<TTexel> left, TemporaryTextureWriteSpace<TTexel> right) => !left.Equals(right);
	}

	static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

	public static TemporaryTextureWriteSpace<TTexel> AllocateTemporaryTexelBuffer<TTexel>(int width, int height) where TTexel : unmanaged, ITexel<TTexel> {
		if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), width, $"Width must be positive.");
		if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), height, $"Height must be positive.");
		var numBytesRequired = TTexel.SerializationByteSpanLength * width * height;
		return new(_bytePool.Rent(numBytesRequired), numBytesRequired);
	}

	public static Texture GenerateSolidColorTexture(IMaterialBuilder builder, ColorVect color, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(builder);

		var config = new TextureCreationConfig {
			GenerateMipMaps = false,
			Width = 1,
			Height = 1,
			Name = name
		};

		// ReSharper disable once CompareOfFloatsByEqualityOperator In this case we want an explicit comparison
		if (color.Alpha == 1f) {
			var texel = (TexelRgb24) color;
			return builder.CreateTexture(new ReadOnlySpan<TexelRgb24>(in texel), config);
		}
		else {
			var texel = (TexelRgba32) color;
			return builder.CreateTexture(new ReadOnlySpan<TexelRgba32>(in texel), config);
		}
	}
}