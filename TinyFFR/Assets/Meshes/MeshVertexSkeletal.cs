// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
public readonly record struct MeshVertexSkeletal : IMeshVertex {
#pragma warning disable CA1034 // "Nested types should not be visible" -- I prefer these being namespaced very specifically to MeshVertexSkeletal
	[InlineArray(MaxBonesPerVertex)]
	public struct BoneIndexArray : IEquatable<BoneIndexArray> {
		byte _;
		
		public static BoneIndexArray Create(byte a, byte b, byte c, byte d) => Create([a, b, c, d]);
		public static BoneIndexArray Create(ReadOnlySpan<byte> indices) {
			var result = new BoneIndexArray();
			indices.CopyTo(result);
			return result;
		}

		public bool Equals(BoneIndexArray other) => ((ReadOnlySpan<byte>) this).SequenceEqual(other);
		public override bool Equals(object? obj) => obj is BoneIndexArray other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(this[0], this[1], this[2], this[3]);
		public static bool operator ==(BoneIndexArray left, BoneIndexArray right) => left.Equals(right);
		public static bool operator !=(BoneIndexArray left, BoneIndexArray right) => !left.Equals(right);
	}

	[InlineArray(MaxBonesPerVertex)]
	public struct BoneWeightArray : IEquatable<BoneWeightArray> {
		float _;
		
		public static BoneWeightArray Create(float a, float b, float c, float d) => Create([a, b, c, d]);
		public static BoneWeightArray Create(ReadOnlySpan<float> weights) {
			var result = new BoneWeightArray();
			weights.CopyTo(result);
			return result;
		}
		
		public bool Equals(BoneWeightArray other) => ((ReadOnlySpan<float>) this).SequenceEqual(other);
		public override bool Equals(object? obj) => obj is BoneWeightArray other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(this[0], this[1], this[2], this[3]);
		public static bool operator ==(BoneWeightArray left, BoneWeightArray right) => left.Equals(right);
		public static bool operator !=(BoneWeightArray left, BoneWeightArray right) => !left.Equals(right);
	}
#pragma warning restore CA1034
	
	public const int MaxBonesPerVertex = 4;
	internal const int ExpectedSerializedSize = 56;
	readonly MeshVertex _baseVertex;
	readonly BoneIndexArray _boneIndices;
	readonly BoneWeightArray _boneWeights;

	public Location Location {
		get => _baseVertex.Location;
		init => _baseVertex = _baseVertex with { Location = value };
	}
	public XYPair<float> TextureCoords {
		get => _baseVertex.TextureCoords;
		init => _baseVertex = _baseVertex with { TextureCoords = value };
	}
	public Quaternion TangentRotation {
		get => _baseVertex.TangentRotation;
		init => _baseVertex = _baseVertex with { TangentRotation = value };
	}

	public BoneIndexArray BoneIndices {
		get => _boneIndices;
		init => _boneIndices = value;
	}
	public BoneWeightArray BoneWeights {
		get => _boneWeights;
		init => _boneWeights = value;
	}
	
	public MeshVertexSkeletal(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal, BoneIndexArray boneIndices, BoneWeightArray boneWeights)
		: this(location, textureCoords, CalculateTangentRotation(tangent, bitangent, normal), boneIndices, boneWeights) { }

	public MeshVertexSkeletal(Location location, XYPair<float> textureCoords, Quaternion tangentRotation, BoneIndexArray boneIndices, BoneWeightArray boneWeights) {
		_baseVertex = new MeshVertex(location, textureCoords, tangentRotation);
		_boneIndices = boneIndices;
		_boneWeights = boneWeights;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) => IMeshVertex.CalculateTangentRotation(tangent, bitangent, normal);
}
