// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Globalization;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets.Baking;

static class BakedResourceSchemata {
	public const int VersionMajor = 1;
	public const int VersionMinor = 0;

	public enum BakedPoolKind {
		Root = -1,
		Texture = 0,
		Material = 1,
		Mesh = 2,
		Model = 3
	}
	
	public static BakedPoolKind GetPoolKindForType(Type type) {
		if (type == typeof(Texture)) return BakedPoolKind.Texture;
		if (type == typeof(Material)) return BakedPoolKind.Material;
		if (type == typeof(Mesh)) return BakedPoolKind.Mesh;
		if (type == typeof(Model)) return BakedPoolKind.Model;
		return BakedPoolKind.Root;
	}
	
	public static Type GetTypeForPoolKind(BakedPoolKind kind) => kind switch {
		BakedPoolKind.Texture => typeof(Texture),
		BakedPoolKind.Material => typeof(Material),
		BakedPoolKind.Mesh => typeof(Mesh),
		BakedPoolKind.Model => typeof(Model),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	public enum BakedReferenceSlot {
		ColorMap = 0,
		NormalMap = 1,
		OrmrMap = 2,
		AnisotropyMap = 3,
		EmissiveMap = 4,
		ClearCoatMap = 5,
		KeyMap = 6,
		AbsorptionTransmissionMap = 7,
		ModelMesh = 8,
		ModelMaterial = 9,
		FontAtlas = 10
	}

	public static class AssetPoolSchema {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public readonly record struct BakedReferenceEntry(
			int OwnerKind,
			int OwnerIndex,
			int Slot,
			int TargetKind,
			int TargetIndex
		);

		public const string TextureCount = "pool_texture_count";
		public const string MaterialCount = "pool_material_count";
		public const string MeshCount = "pool_mesh_count";
		public const string ModelCount = "pool_model_count";

		public const string TexturePrefix = "pool_texture_";
		public const string MaterialPrefix = "pool_material_";
		public const string MeshPrefix = "pool_mesh_";
		public const string ModelPrefix = "pool_model_";

		public const string ReferenceTable = "reference_table";

		public const int MaxPoolSectionNameLength = 32;

		public static ReadOnlySpan<char> GetCountSectionName(BakedPoolKind kind) => kind switch {
			BakedPoolKind.Texture => TextureCount,
			BakedPoolKind.Material => MaterialCount,
			BakedPoolKind.Mesh => MeshCount,
			BakedPoolKind.Model => ModelCount,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
		};

		public static ReadOnlySpan<char> GetEntryPrefix(BakedPoolKind kind) => kind switch {
			BakedPoolKind.Texture => TexturePrefix,
			BakedPoolKind.Material => MaterialPrefix,
			BakedPoolKind.Mesh => MeshPrefix,
			BakedPoolKind.Model => ModelPrefix,
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
		};

		public static ReadOnlySpan<char> WriteEntrySectionName(Span<char> destination, BakedPoolKind kind, int index) {
			var prefix = GetEntryPrefix(kind);
			prefix.CopyTo(destination);
			if (!index.TryFormat(destination[prefix.Length..], out var indexCharsWritten, provider: CultureInfo.InvariantCulture)) {
				throw new InvalidOperationException($"Could not format pool section name for index {index} (this is a bug in TinyFFR).");
			}
			return destination[..(prefix.Length + indexCharsWritten)];
		}
	}

	public static class BackdropTextureBakingSchema {
		public const string SkyboxData = "skybox";
		public const string IblData = "ibl";
	}

	public static class TextureBakingSchema {
		public const string DimensionsX = "dimensions_x";
		public const string DimensionsY = "dimensions_y";
		public const string IsRgba = "is_rgba";
		public const string MipMapsEnabled = "mipmaps_enabled";
		public const string AllowsDynamicWrites = "allows_dynamic_writes";
		public const string DataType = "data_type";
		public const string CompressionFormat = "compression_format";
		public const string CompressedLevelCount = "compressed_level_count";
		public const string DisableTextureRepeat = "disable_texture_repeat";
		public const string DisableTexelBlending = "disable_texel_blending";
		public const string AnisotropicFilteringQuality = "anisotropic_filtering_quality";
		public const string AnisotropyLevel = "anisotropy_level";
		public const string TexelData = "texel_data";
	}

	public static class MaterialBakingSchema {
		public enum BakedMaterialKind {
			LightingIgnoring,
			ColorKeyed,
			Standard,
			Transmissive
		}

		public const string Kind = "material_kind";
		public const string EnablePerInstanceEffects = "enable_per_instance_effects";
		public const string AlphaMode = "alpha_mode";
		public const string BlendOutputAlphaWithScene = "blend_output_alpha_with_scene";
		public const string RefractionThickness = "refraction_thickness";
		public const string TransmissiveQuality = "transmissive_quality";
	}

	public static class FontBakingSchema {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public readonly record struct BakedRuneEntry(
			int RuneValue,
			float AtlasUvOffsetX,
			float AtlasUvOffsetY,
			float AtlasUvSizeX,
			float AtlasUvSizeY,
			float NibOffsetX,
			float NibOffsetY,
			float AdvanceWidth
		);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public readonly record struct BakedKerningEntry(ulong PackedRunePair, float Advance);

		public const string Ascent = "ascent";
		public const string Descent = "descent";
		public const string LineAdvance = "line_advance";
		public const string LineBreakRune = "line_break_rune";
		public const string RuneMap = "rune_map";
		public const string KerningMap = "kerning_map";
	}

	public static class MeshBakingSchema {
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public readonly record struct BakedAnimationEntry(
			int ScalingStart, int ScalingCount,
			int RotationStart, int RotationCount,
			int TranslationStart, int TranslationCount,
			int MutationStart, int MutationCount,
			int NameStart, int NameLength,
			float DefaultCompletionTimeSeconds
		);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public readonly record struct BakedNodeNameEntry(int NodeIndex, int NameStart, int NameLength);

		public const string IsSkeletal = "is_skeletal";
		public const string VertexData = "vertex_data";
		public const string IndexData = "index_data";
		public const string VertexCount = "vertex_count";
		public const string TriangleCount = "triangle_count";
		public const string BoundingBox = "bounding_box";
		public const string BoneCount = "bone_count";
		public const string AllowsPerInstanceVertexMutation = "allows_per_instance_vertex_mutation";
		public const string GeneratesWireframeData = "generates_wireframe_data";

		public const string SkeletonNodeCount = "skeleton_node_count";
		public const string SkeletonFirstParentedNodeIndex = "skeleton_first_parented_node_index";
		public const string SkeletonModelImportTransform = "skeleton_model_import_transform";
		public const string SkeletonDefaultLocalTransforms = "skeleton_default_local_transforms";
		public const string SkeletonBindPoseInversions = "skeleton_bind_pose_inversions";
		public const string SkeletonParentIndices = "skeleton_parent_indices";
		public const string SkeletonBoneToNodeMap = "skeleton_bone_to_node_map";
		public const string SkeletonMutationTargetIndexMap = "skeleton_mutation_target_index_map";

		public const string AnimationTable = "animation_table";
		public const string AnimationScalingKeyframes = "animation_scaling_keyframes";
		public const string AnimationRotationKeyframes = "animation_rotation_keyframes";
		public const string AnimationTranslationKeyframes = "animation_translation_keyframes";
		public const string AnimationMutationDescriptors = "animation_mutation_descriptors";
		public const string AnimationNameChars = "animation_name_chars";

		public const string NodeNameTable = "node_name_table";
		public const string NodeNameChars = "node_name_chars";
	}
}
