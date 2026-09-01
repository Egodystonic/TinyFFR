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
}
