// Created on 2026-09-01 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	#region Baking
	public ResourceGroup LoadBakedMaterial(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.Load<Material, ResourceGroup, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedMaterialCore);
	}

	public TinyFfrAsyncOperation<ResourceGroup> LoadBakedMaterialAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.LoadAsync<Material, ResourceGroup, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedMaterialCore);
	}

	static ResourceGroup LoadBakedMaterialCore(LocalAssetBakery.AssetLoadContext ctx) {
		static ResourceGroup Finalize(LocalAssetBakery.AssetLoadContext ctx) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

			var assetData = ctx.AssetData;
			var self = ctx.Invoker<LocalAssetLoader>();
			var name = ctx.StoredOrOverridingName;

			var kind = assetData.Extract<MaterialBakingSchema.BakedMaterialKind>(MaterialBakingSchema.Kind);
			var enablePerInstanceEffects = assetData.Extract(MaterialBakingSchema.EnablePerInstanceEffects, false);

			var group = self._globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, name);

			try {
				switch (kind) {
					case MaterialBakingSchema.BakedMaterialKind.LightingIgnoring: {
						var colorMap = RebuildRequiredMap(self, group, assetData, MaterialBakingSchema.ColorMap);
						var material = self._materialBuilder.CreateLightingIgnoringMaterial(new LightingIgnoringMaterialCreationConfig {
							ColorMap = colorMap,
							Name = name,
							EnablePerInstanceEffects = enablePerInstanceEffects
						});
						group.Add(material);
						break;
					}
					case MaterialBakingSchema.BakedMaterialKind.ColorKeyed: {
						var keyMap = RebuildRequiredMap(self, group, assetData, MaterialBakingSchema.KeyMap);
						var material = self._materialBuilder.CreateColorKeyedMaterial(new ColorKeyedMaterialCreationConfig {
							KeyMap = keyMap,
							BlendOutputAlphaWithScene = assetData.Extract<bool>(MaterialBakingSchema.BlendOutputAlphaWithScene),
							Name = name
						});
						group.Add(material);
						break;
					}
					case MaterialBakingSchema.BakedMaterialKind.Standard: {
						var colorMap = RebuildRequiredMap(self, group, assetData, MaterialBakingSchema.ColorMap);
						var material = self._materialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
							ColorMap = colorMap,
							NormalMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.NormalMap),
							OcclusionRoughnessMetallicReflectanceMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.OrmrMap),
							AnisotropyMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.AnisotropyMap),
							EmissiveMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.EmissiveMap),
							ClearCoatMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.ClearCoatMap),
							AlphaMode = assetData.Extract<StandardMaterialAlphaMode>(MaterialBakingSchema.AlphaMode),
							Name = name,
							EnablePerInstanceEffects = enablePerInstanceEffects
						});
						group.Add(material);
						break;
					}
					case MaterialBakingSchema.BakedMaterialKind.Transmissive: {
						var colorMap = RebuildRequiredMap(self, group, assetData, MaterialBakingSchema.ColorMap);
						var absorptionTransmissionMap = RebuildRequiredMap(self, group, assetData, MaterialBakingSchema.AbsorptionTransmissionMap);
						var material = self._materialBuilder.CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
							ColorMap = colorMap,
							AbsorptionTransmissionMap = absorptionTransmissionMap,
							NormalMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.NormalMap),
							OcclusionRoughnessMetallicReflectanceMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.OrmrMap),
							AnisotropyMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.AnisotropyMap),
							EmissiveMap = RebuildOptionalMap(self, group, assetData, MaterialBakingSchema.EmissiveMap),
							RefractionThickness = assetData.Extract<float>(MaterialBakingSchema.RefractionThickness),
							Quality = assetData.Extract<TransmissiveMaterialQuality>(MaterialBakingSchema.TransmissiveQuality),
							AlphaMode = assetData.Extract<TransmissiveMaterialAlphaMode>(MaterialBakingSchema.AlphaMode),
							Name = name,
							EnablePerInstanceEffects = enablePerInstanceEffects
						});
						group.Add(material);
						break;
					}
					default:
						throw new AssetBakeException($"Baked material declares unknown material kind '{kind}'.");
				}
			}
			catch {
				group.Dispose();
				throw;
			}

			group.Seal();
			return group;
		}

		return ctx.GenerateResourceOnPrimaryAndWait(&Finalize);
	}

	static Texture RebuildRequiredMap(LocalAssetLoader self, ResourceGroup group, LoadedBakedAsset assetData, ReadOnlySpan<char> sectionName) {
		return RebuildMap(self, group, assetData.ExtractSubAsset<Texture>(sectionName));
	}

	static Texture? RebuildOptionalMap(LocalAssetLoader self, ResourceGroup group, LoadedBakedAsset assetData, ReadOnlySpan<char> sectionName) {
		if (assetData.TryExtractSubAsset<Texture>(sectionName) is not { } subAsset) return null;
		return RebuildMap(self, group, subAsset);
	}

	static Texture RebuildMap(LocalAssetLoader self, ResourceGroup group, LoadedBakedAsset subAsset) {
		var result = CreateTextureFromBakedAsset(self, subAsset, subAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default));
		group.Add(result);
		return result;
	}
	#endregion
}
