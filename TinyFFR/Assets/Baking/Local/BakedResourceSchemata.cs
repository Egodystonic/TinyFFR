// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Assets.Baking;

static class BakedResourceSchemata {
	public const int VersionMajor = 1;
	public const int VersionMinor = 0;
	
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

		public const string ColorMap = "color_map";
		public const string NormalMap = "normal_map";
		public const string OrmrMap = "ormr_map";
		public const string AnisotropyMap = "anisotropy_map";
		public const string EmissiveMap = "emissive_map";
		public const string ClearCoatMap = "clear_coat_map";
		public const string KeyMap = "key_map";
		public const string AbsorptionTransmissionMap = "absorption_transmission_map";
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

		public const string Atlas = "atlas";
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
