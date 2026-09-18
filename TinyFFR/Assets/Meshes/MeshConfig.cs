// Created on 2024-08-14 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets.Materials;
using System;
using Egodystonic.TinyFFR.World;
using static Egodystonic.TinyFFR.IConfigStruct;

namespace Egodystonic.TinyFFR.Assets.Meshes;

// Read Config for just how to read the file in (e.g. any preprocessing and the file path)
// Generation Config for live generation of new ones
// Creation Config for general processing in the local builder when creating the resource

/// <summary>
/// Controls how a mesh file's contents are interpreted as they are read from disc.
/// </summary>
public readonly ref struct MeshReadConfig : IConfigStruct<MeshReadConfig> {
	/// <summary>
	/// Whether to attempt to repair common faults in exported mesh data. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Exported meshes frequently carry small defects — normals pointing inwards, triangles with no area — which this cleans up
	/// as the file is read. Turn it off only if the repairs are themselves damaging a mesh.
	/// </remarks>
	public bool FixCommonExportErrors { get; init; } = true;
	/// <summary>
	/// Whether to spend extra time reordering the mesh data so that the GPU draws it faster. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Turning this off shortens load times at the potential cost of framerate depending on how well-optimised the target mesh already is.
	/// </remarks>
	public bool OptimizeForGpu { get; init; } = true;
	/// <summary>
	/// Whether to correct the mesh's facing if it appears to have been exported inside-out. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// A mesh exported through a mirroring or negative-scaling transform ends up with its winding, normals and tangents all
	/// reversed, which makes it invisible from the outside. Turn this off to see the data exactly as exported.
	/// </remarks>
	public bool CorrectFlippedOrientation { get; init; } = true;
	/// <summary>
	/// Whether to load skeletal animation data where the file contains it. Defaults to <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// Turning this off loads the mesh as a static one, ignoring its skeleton and animations.
	/// </remarks>
	public bool LoadSkeletalAnimationDataIfPresent { get; init; } = true;
	/// <summary>
	/// Which single sub-mesh to load, or <see langword="null"/> to load them all as one combined mesh. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The number of sub-meshes a file holds is reported by <see cref="MeshReadMetadata.SubMeshCount"/>. Must index a sub-mesh
	/// the file actually has. <c>0</c> is always a valid value.
	/// </remarks>
	public int? SubMeshIndex { get; init; } = null;
	/// <summary>
	/// The keyframe tick rate to assume for this file's animations, or <see langword="null"/> to take it from the file. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// An animation's keyframes are timed in the file's own ticks, and the tick rate is what converts those to seconds. Formats
	/// differ, and some do not record it at all, in which case 25 ticks per second is assumed. Set this where that assumption
	/// makes an animation run at the wrong speed.
	/// </remarks>
	public float? AnimationTicksPerSecondOverride { get; init; } = null;

	/// <summary>
	/// Constructs a new <see cref="MeshReadConfig"/> with default values for every property.
	/// </summary>
	public MeshReadConfig() { }

	internal void ThrowIfInvalid() {
		if (SubMeshIndex < 0) throw new ArgumentOutOfRangeException(nameof(SubMeshIndex), SubMeshIndex, $"Sub-mesh index must be null or non-negative.");
	}

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in MeshReadConfig src) {
		return	SerializationSizeOfBool() // FixCommonExportErrors
			+	SerializationSizeOfBool() // OptimizeForGpu
			+	SerializationSizeOfBool() // CorrectFlippedOrientation
			+	SerializationSizeOfBool() // LoadSkeletalAnimationDataIfPresent
			+	SerializationSizeOfNullableInt() // SubMeshIndex
			+	SerializationSizeOfNullableFloat(); // AnimationTicksPerSecondOverride
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshReadConfig src) {
		SerializationWriteBool(ref dest, src.FixCommonExportErrors);
		SerializationWriteBool(ref dest, src.OptimizeForGpu);
		SerializationWriteBool(ref dest, src.CorrectFlippedOrientation);
		SerializationWriteBool(ref dest, src.LoadSkeletalAnimationDataIfPresent);
		SerializationWriteNullableInt(ref dest, src.SubMeshIndex);
		SerializationWriteNullableFloat(ref dest, src.AnimationTicksPerSecondOverride);
	}
	/// <inheritdoc />
	public static MeshReadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshReadConfig {
			FixCommonExportErrors = SerializationReadBool(ref src),
			OptimizeForGpu = SerializationReadBool(ref src),
			CorrectFlippedOrientation = SerializationReadBool(ref src),
			LoadSkeletalAnimationDataIfPresent = SerializationReadBool(ref src),
			SubMeshIndex = SerializationReadNullableInt(ref src),
			AnimationTicksPerSecondOverride = SerializationReadNullableFloat(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Controls how vertices are produced when a mesh is generated from a shape description or a set of polygons.
/// </summary>
public readonly ref struct MeshGenerationConfig : IConfigStruct<MeshGenerationConfig> {
	/// <summary>
	/// How to adjust every generated vertex's texture coordinates. Defaults to <see cref="Transform2D.None"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is how a texture is tiled, shifted or turned across generated geometry without altering the texture itself.
	/// </para>
	/// <para>
	/// The transform's translation is added to each coordinate pair; its rotation turns each pair anticlockwise about
	/// <c>(0, 0)</c>; and its scaling is applied as its <i>reciprocal</i>, so that a scaling of <c>2f</c> makes the texture
	/// appear twice as large rather than tiling twice as often. Supply the reciprocal yourself if you want the other behaviour.
	/// </para>
	/// </remarks>
	public Transform2D TextureTransform { get; init; } = Transform2D.None;

	/// <summary>
	/// Constructs a new <see cref="MeshGenerationConfig"/> with default values for every property.
	/// </summary>
	public MeshGenerationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in MeshGenerationConfig src) {
		return SerializationSizeOf<Transform2D>(); // TextureTransform
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshGenerationConfig src) {
		SerializationWrite(ref dest, src.TextureTransform);
	}
	/// <inheritdoc />
	public static MeshGenerationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshGenerationConfig {
			TextureTransform = SerializationRead<Transform2D>(ref src)
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}

/// <summary>
/// Controls how a mesh is created, whether it was read from a file or generated from scratch.
/// </summary>
/// <remarks>
/// The defaults suit a mesh authored in metres under TinyFFR's own conventions. The rest exist to reconcile a mesh authored
/// under different conventions, and to enable the optional features — vertex mutation and wireframe drawing — that cost memory
/// when switched on.
/// </remarks>
public readonly ref struct MeshCreationConfig : IConfigStruct<MeshCreationConfig> {
	/// <summary>
	/// The default value for <see cref="BoundingBoxAdditionalMargin"/>: <c>0.03f</c>.
	/// </summary>
	public static readonly float DefaultBoundingBoxAdditionalMargin = 0.03f;

	/// <summary>
	/// Whether to reverse the winding order of every triangle, turning the mesh inside out. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// TinyFFR only draws the front of a surface, and which side is the front is decided by the order its triangles' vertices
	/// are given in: Anticlockwise when viewed from the front. A mesh exported under the opposite convention is invisible from
	/// outside until this is set.
	/// </remarks>
	public bool FlipTriangles { get; init; } = false;
	/// <summary>
	/// Whether to invert every vertex's horizontal texture coordinate. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Inverting turns <c>0f</c> in to <c>1f</c> and <c>0.3f</c> in to <c>0.7f</c>, which mirrors the texture across the
	/// surface. Use this for meshes exported under the opposite texture convention; it is often needed alongside
	/// <see cref="InvertTextureV"/> and/or <see cref="FlipTriangles"/>.
	/// </remarks>
	public bool InvertTextureU { get; init; } = false;
	/// <summary>
	/// Whether to invert every vertex's vertical texture coordinate. Defaults to <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// Inverting turns <c>0f</c> in to <c>1f</c> and <c>0.3f</c> in to <c>0.7f</c>, which mirrors the texture across the
	/// surface. Use this for meshes exported under the opposite texture convention; it is often needed alongside
	/// <see cref="InvertTextureU"/> and/or <see cref="FlipTriangles"/>.
	/// </remarks>
	public bool InvertTextureV { get; init; } = false;
	/// <summary>
	/// Where to move the mesh's origin to, relative to where it currently is. Defaults to <see cref="Vect.Zero"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A mesh's origin is the point that ends up exactly where an object using it is positioned, and the point it rotates and
	/// scales around by default. A model exported with its origin at its feet, for instance, hangs above wherever you place it
	/// unless the origin is moved (which may be deliberate or accidental depending on the use-case of the mesh).
	/// </para>
	/// <para>
	/// The vertices move by the <i>inverse</i> of this value, so passing <c>(1, 2, 3)</c> shifts the origin to that point by
	/// moving every vertex by <c>(-1, -2, -3)</c>.
	/// </para>
	/// </remarks>
	public Vect OriginTranslation { get; init; } = Vect.Zero;
	/// <summary>
	/// A factor to resize the whole mesh by. Defaults to <c>1f</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is for reconciling the units a mesh was authored in with the metres TinyFFR works in. For example, a mesh exported in feet is
	/// brought to metres with a factor of <c>1f / 3.28084f</c>.
	/// </para>
	/// <para>
	/// Only one factor is offered, applying to every axis, because the per-vertex tangent and normal data baked in to a mesh
	/// stays correct under uniform scaling and does not under per-axis scaling. Objects can still be scaled per-axis once
	/// created. A negative value turns the mesh inside out.
	/// </para>
	/// </remarks>
	public float LinearRescalingFactor { get; init; } = 1f;
	/// <summary>
	/// A bounding box to use instead of the one derived from the mesh's own geometry, or <see langword="null"/> to derive it. Defaults to <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// The derived box covers the mesh as supplied. That is not always enough for a mesh you animate yourself; animations are
	/// attached after the mesh is created, so a pose that moves vertices outside the original box would leave the object liable
	/// to vanish when it should still be on screen. Supply a box large enough to cover every pose in that case.
	/// <see cref="BoundingBoxAdditionalMargin"/> is still applied.
	/// </remarks>
	public PositionedCuboid? BoundingBoxOverride { get; init; } = null;
	/// <summary>
	/// How much to enlarge the derived bounding box by, in metres. Defaults to <see cref="DefaultBoundingBoxAdditionalMargin"/>: <c>0.03f</c>.
	/// </summary>
	/// <remarks>
	/// A small margin absorbs the rounding error in the box's derivation, so that geometry right at the edge of a mesh is never
	/// wrongly judged to be off screen.
	/// </remarks>
	public float BoundingBoxAdditionalMargin { get; init; } = DefaultBoundingBoxAdditionalMargin;
	/// <summary>
	/// Whether objects using this mesh may alter their own copy of its vertices at runtime. Defaults to <see langword="false"/>.
	/// Setting this to <c>true</c> enables the <see cref="Mesh.AllowsPerInstanceVertexMutation"/> flag and <see cref="ModelInstance.BorrowVerticesSpan(bool)"/> API.
	/// </summary>
	/// <remarks>
	/// This is needed for effects that reshape geometry as the program runs. It costs ordinary memory in addition to the usual
	/// video memory, since the vertices must then be kept on both sides, and each altering object keeps its own copy as well.
	/// </remarks>
	public bool AllowsPerInstanceVertexMutation { get; init; } = false;
	/// <summary>
	/// Whether to prepare the extra data needed to draw this mesh as a wireframe. Defaults to <see langword="false"/>.
	/// Setting this to <c>true</c> enables setting the <see cref="DefaultMaterialShadingStyle.Wireframe"/> shading type
	/// on the <see cref="IMaterialBuilder.DefaultMaterial"/> for <see cref="ModelInstance"/>s using this mesh.
	/// </summary>
	/// <remarks>
	/// Wireframe drawing shows a mesh's triangle edges rather than its surfaces, which is useful diagnostically. The extra data
	/// costs video memory whether or not it is ever drawn.
	/// </remarks>
	public bool GenerateWireframeData { get; init; } = false;
	/// <summary>
	/// The name to give the mesh. May be left empty.
	/// </summary>
	public ReadOnlySpan<char> Name { get; init; }

	/// <summary>
	/// Constructs a new <see cref="MeshCreationConfig"/> with default values for every property.
	/// </summary>
	public MeshCreationConfig() { }

#pragma warning disable CA1822 // "Could be static" -- Placeholder method for future
	internal void ThrowIfInvalid() {
		/* no op */
	}
#pragma warning restore CA1822

	/// <inheritdoc />
	public static int GetHeapStorageFormattedLength(in MeshCreationConfig src) {
		return	SerializationSizeOfBool() // FlipTriangles
			+	SerializationSizeOfBool() // InvertTextureU
			+	SerializationSizeOfBool() // InvertTextureV
			+	SerializationSizeOf<Vect>() // OriginTranslation
			+	SerializationSizeOfFloat() // LinearRescalingFactor
			+	SerializationSizeOfNullable<PositionedCuboid>() // BoundingBoxOverride
			+	SerializationSizeOfFloat() // BoundingBoxAdditionalMargin
			+	SerializationSizeOfBool() // AllowPerInstanceVertexMutation
			+	SerializationSizeOfBool() // GenerateWireframeData
			+	SerializationSizeOfString(src.Name); // Name
	}
	/// <inheritdoc />
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshCreationConfig src) {
		SerializationWriteBool(ref dest, src.FlipTriangles);
		SerializationWriteBool(ref dest, src.InvertTextureU);
		SerializationWriteBool(ref dest, src.InvertTextureV);
		SerializationWrite(ref dest, src.OriginTranslation);
		SerializationWriteFloat(ref dest, src.LinearRescalingFactor);
		SerializationWriteNullable(ref dest, src.BoundingBoxOverride);
		SerializationWriteFloat(ref dest, src.BoundingBoxAdditionalMargin);
		SerializationWriteBool(ref dest, src.AllowsPerInstanceVertexMutation);
		SerializationWriteBool(ref dest, src.GenerateWireframeData);
		SerializationWriteString(ref dest, src.Name);
	}
	/// <inheritdoc />
	public static MeshCreationConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshCreationConfig {
			FlipTriangles = SerializationReadBool(ref src),
			InvertTextureU = SerializationReadBool(ref src),
			InvertTextureV = SerializationReadBool(ref src),
			OriginTranslation = SerializationRead<Vect>(ref src),
			LinearRescalingFactor = SerializationReadFloat(ref src),
			BoundingBoxOverride = SerializationReadNullable<PositionedCuboid>(ref src),
			BoundingBoxAdditionalMargin = SerializationReadFloat(ref src),
			AllowsPerInstanceVertexMutation = SerializationReadBool(ref src),
			GenerateWireframeData = SerializationReadBool(ref src),
			Name = SerializationReadString(ref src),
		};
	}
	/// <inheritdoc />
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		/* no-op */
	}
}
readonly ref struct MeshLoadConfig : IConfigStruct<MeshLoadConfig> {
	public MeshCreationConfig CreationConfig { get; init; } = new();
	public MeshReadConfig ReadConfig { get; init; } = new();

	public MeshLoadConfig() { }

	internal void ThrowIfInvalid() {
		CreationConfig.ThrowIfInvalid();
		ReadConfig.ThrowIfInvalid();
	}

	public static int GetHeapStorageFormattedLength(in MeshLoadConfig src) {
		return	SerializationSizeOfSubConfig(src.CreationConfig) // CreationConfig
			+	SerializationSizeOfSubConfig(src.ReadConfig); // ReadConfig
	}
	public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in MeshLoadConfig src) {
		SerializationWriteSubConfig(ref dest, src.CreationConfig);
		SerializationWriteSubConfig(ref dest, src.ReadConfig);
	}
	public static MeshLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		return new MeshLoadConfig {
			CreationConfig = SerializationReadSubConfig<MeshCreationConfig>(ref src),
			ReadConfig = SerializationReadSubConfig<MeshReadConfig>(ref src)
		};
	}
	public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) {
		SerializationDisposeSubConfig<MeshCreationConfig>(ref src);
		SerializationDisposeSubConfig<MeshReadConfig>(ref src);
	}
}
