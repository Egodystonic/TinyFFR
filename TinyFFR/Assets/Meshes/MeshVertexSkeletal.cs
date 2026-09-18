// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using System;

namespace Egodystonic.TinyFFR.Assets.Meshes;

/// <summary>
/// One vertex of an animated mesh: everything an ordinary vertex carries, plus which bones move it and by how much.
/// </summary>
/// <remarks>
/// <para>
/// A vertex is positioned by up to four bones at once, each pulling it by its own weight. Blending several bones is what lets a
/// surface bend smoothly across a joint instead of creasing sharply at it.
/// </para>
/// <para>
/// The position stored here is the mesh's <i>bind pose</i>: where the vertex sits before any animation is applied.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = ExpectedSerializedSize)]
public readonly record struct MeshVertexSkeletal : IMeshVertex {
#pragma warning disable CA1034 // "Nested types should not be visible" -- I prefer these being namespaced very specifically to MeshVertexSkeletal
	/// <summary>
	/// The four bones that may move one vertex, as indices in to the skeleton's bone list.
	/// </summary>
	[InlineArray(MaxBonesPerVertex)]
	public struct BoneIndexArray : IEquatable<BoneIndexArray> {
		byte _;
		
		/// <summary>
		/// Creates a <see cref="BoneIndexArray"/> from four bone indices.
		/// </summary>
		/// <param name="a">The first bone index.</param>
		/// <param name="b">The second bone index.</param>
		/// <param name="c">The third bone index.</param>
		/// <param name="d">The fourth bone index.</param>
		public static BoneIndexArray Create(byte a, byte b, byte c, byte d) => Create([a, b, c, d]);
		/// <summary>
		/// Creates a <see cref="BoneIndexArray"/> from a span of bone indices.
		/// </summary>
		/// <param name="indices">The bone indices. Must hold no more than four entries; any not supplied are left at <c>0</c>.</param>
		public static BoneIndexArray Create(ReadOnlySpan<byte> indices) {
			var result = new BoneIndexArray();
			indices.CopyTo(result);
			return result;
		}

		/// <inheritdoc />
		public bool Equals(BoneIndexArray other) => ((ReadOnlySpan<byte>) this).SequenceEqual(other);
		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is BoneIndexArray other && Equals(other);
		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(this[0], this[1], this[2], this[3]);
		/// <summary>
		/// Returns whether the two given arrays hold the same four bone indices.
		/// </summary>
		/// <param name="left">The first array to compare.</param>
		/// <param name="right">The second array to compare.</param>
		public static bool operator ==(BoneIndexArray left, BoneIndexArray right) => left.Equals(right);
		/// <summary>
		/// Returns whether the two given arrays differ in any of their four bone indices.
		/// </summary>
		/// <param name="left">The first array to compare.</param>
		/// <param name="right">The second array to compare.</param>
		public static bool operator !=(BoneIndexArray left, BoneIndexArray right) => !left.Equals(right);

		/// <inheritdoc />
		public override string ToString() => $"[{this[0]}, {this[1]}, {this[2]}, {this[3]}]";
	}

	/// <summary>
	/// How strongly each of a vertex's four bones pulls it.
	/// </summary>
	[InlineArray(MaxBonesPerVertex)]
	public struct BoneWeightArray : IEquatable<BoneWeightArray> {
		float _;
		
		/// <summary>
		/// Creates a <see cref="BoneWeightArray"/> from four bone weights.
		/// </summary>
		/// <param name="a">The first bone weight.</param>
		/// <param name="b">The second bone weight.</param>
		/// <param name="c">The third bone weight.</param>
		/// <param name="d">The fourth bone weight.</param>
		public static BoneWeightArray Create(float a, float b, float c, float d) => Create([a, b, c, d]);
		/// <summary>
		/// Creates a <see cref="BoneWeightArray"/> from a span of bone weights.
		/// </summary>
		/// <param name="weights">The bone weights. Must hold no more than four entries; any not supplied are left at <c>0f</c>.</param>
		public static BoneWeightArray Create(ReadOnlySpan<float> weights) {
			var result = new BoneWeightArray();
			weights.CopyTo(result);
			return result;
		}
		
		/// <inheritdoc />
		public bool Equals(BoneWeightArray other) => ((ReadOnlySpan<float>) this).SequenceEqual(other);
		/// <inheritdoc />
		public override bool Equals(object? obj) => obj is BoneWeightArray other && Equals(other);
		/// <inheritdoc />
		public override int GetHashCode() => HashCode.Combine(this[0], this[1], this[2], this[3]);
		/// <summary>
		/// Returns whether the two given arrays hold the same four bone weights.
		/// </summary>
		/// <param name="left">The first array to compare.</param>
		/// <param name="right">The second array to compare.</param>
		public static bool operator ==(BoneWeightArray left, BoneWeightArray right) => left.Equals(right);
		/// <summary>
		/// Returns whether the two given arrays differ in any of their four bone weights.
		/// </summary>
		/// <param name="left">The first array to compare.</param>
		/// <param name="right">The second array to compare.</param>
		public static bool operator !=(BoneWeightArray left, BoneWeightArray right) => !left.Equals(right);
		
		/// <inheritdoc />
		public override string ToString() => $"[{this[0]}, {this[1]}, {this[2]}, {this[3]}]";
	}
#pragma warning restore CA1034
	
	/// <summary>
	/// How many bones may influence a single vertex: <c>4</c>.
	/// </summary>
	public const int MaxBonesPerVertex = 4;
	internal const int ExpectedSerializedSize = 56;
	readonly MeshVertex _baseVertex;
	readonly BoneIndexArray _boneIndices;
	readonly BoneWeightArray _boneWeights;

	/// <inheritdoc />
	public Location Location {
		get => _baseVertex.Location;
		init => _baseVertex = _baseVertex with { Location = value };
	}
	/// <inheritdoc />
	public XYPair<float> TextureCoords {
		get => _baseVertex.TextureCoords;
		init => _baseVertex = _baseVertex with { TextureCoords = value };
	}
	/// <inheritdoc />
	public Quaternion TangentRotation {
		get => _baseVertex.TangentRotation;
		init => _baseVertex = _baseVertex with { TangentRotation = value };
	}

	/// <summary>
	/// Which bones of the skeleton move this vertex, given as indices in to the skeleton's bone list.
	/// </summary>
	/// <remarks>
	/// Each entry is paired with the entry at the same position in <see cref="BoneWeights"/>. A bone whose weight is zero has
	/// no effect, so fewer than four bones can be used by leaving the surplus weights at zero.
	/// </remarks>
	public BoneIndexArray BoneIndices {
		get => _boneIndices;
		init => _boneIndices = value;
	}
	/// <summary>
	/// How strongly each of this vertex's bones pulls it, paired with <see cref="BoneIndices"/>.
	/// </summary>
	/// <remarks>
	/// The four weights should sum to <c>1f</c>; a vertex rigidly attached to one bone therefore has weights of
	/// <c>(1, 0, 0, 0)</c>. Weights that do not sum to <c>1f</c> leave the vertex over- or under-moved relative to its bones.
	/// </remarks>
	public BoneWeightArray BoneWeights {
		get => _boneWeights;
		init => _boneWeights = value;
	}
	
	/// <summary>
	/// Constructs a new <see cref="MeshVertexSkeletal"/>, deriving its tangent rotation from the three surface directions.
	/// </summary>
	/// <param name="location">Where the vertex sits, relative to the mesh's own origin.</param>
	/// <param name="textureCoords">Where on a texture this vertex maps to, with <c>(0, 0)</c> the bottom-left corner.</param>
	/// <param name="tangent">The direction in which the texture's horizontal coordinate increases across the surface.</param>
	/// <param name="bitangent">The direction in which the texture's vertical coordinate increases across the surface.</param>
	/// <param name="normal">The direction pointing directly out of the front of the surface.</param>
	/// <param name="boneIndices">Which bones move this vertex.</param>
	/// <param name="boneWeights">How strongly each of those bones pulls it.</param>
	public MeshVertexSkeletal(Location location, XYPair<float> textureCoords, Direction tangent, Direction bitangent, Direction normal, BoneIndexArray boneIndices, BoneWeightArray boneWeights)
		: this(location, textureCoords, CalculateTangentRotation(tangent, bitangent, normal), boneIndices, boneWeights) { }

	/// <summary>
	/// Constructs a new <see cref="MeshVertexSkeletal"/> from an already-derived tangent rotation.
	/// </summary>
	/// <param name="location">Where the vertex sits, relative to the mesh's own origin.</param>
	/// <param name="textureCoords">Where on a texture this vertex maps to, with <c>(0, 0)</c> the bottom-left corner.</param>
	/// <param name="tangentRotation">The combined tangent, bitangent and normal, as a single rotation.</param>
	/// <param name="boneIndices">Which bones move this vertex.</param>
	/// <param name="boneWeights">How strongly each of those bones pulls it.</param>
	public MeshVertexSkeletal(Location location, XYPair<float> textureCoords, Quaternion tangentRotation, BoneIndexArray boneIndices, BoneWeightArray boneWeights) {
		_baseVertex = new MeshVertex(location, textureCoords, tangentRotation);
		_boneIndices = boneIndices;
		_boneWeights = boneWeights;
	}

	/// <summary>
	/// Calculates the tangent rotation for a vertex from the three directions that describe its surface.
	/// </summary>
	/// <remarks>
	/// The <i>tangent</i> points along the direction in which a texture's horizontal coordinate increases across the surface;
	/// the <i>bitangent</i> along the direction in which its vertical coordinate increases; and the <i>normal</i> points
	/// directly out of the front of the surface.
	/// </remarks>
	/// <param name="tangent">The direction in which the texture's horizontal coordinate increases across the surface.</param>
	/// <param name="bitangent">The direction in which the texture's vertical coordinate increases across the surface.</param>
	/// <param name="normal">The direction pointing directly out of the front of the surface.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Quaternion CalculateTangentRotation(Direction tangent, Direction bitangent, Direction normal) => IMeshVertex.CalculateTangentRotation(tangent, bitangent, normal);
}
