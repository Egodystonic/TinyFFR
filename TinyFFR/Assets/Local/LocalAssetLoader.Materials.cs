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

			var group = self._globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: true, name);
			var resolver = new BakedAssetResolver(assetData, group, self);
			try {
				resolver.MaterializeAll();
				group.Add(CreateMaterialFromBakedAsset(self, assetData, name, BakedPoolKind.Root, -1, in resolver));
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

	static Material CreateMaterialFromBakedAsset(LocalAssetLoader self, LoadedBakedAsset assetData, ReadOnlySpan<char> name, BakedPoolKind ownerKind, int ownerIndex, in BakedAssetResolver resolver) {
		var kind = assetData.Extract<MaterialBakingSchema.BakedMaterialKind>(MaterialBakingSchema.Kind);
		var enablePerInstanceEffects = assetData.Extract(MaterialBakingSchema.EnablePerInstanceEffects, false);

		switch (kind) {
			case MaterialBakingSchema.BakedMaterialKind.LightingIgnoring: {
				return self._materialBuilder.CreateLightingIgnoringMaterial(new LightingIgnoringMaterialCreationConfig {
					ColorMap = resolver.ResolveTexture(ownerKind, ownerIndex, BakedReferenceSlot.ColorMap),
					Name = name,
					EnablePerInstanceEffects = enablePerInstanceEffects
				});
			}
			case MaterialBakingSchema.BakedMaterialKind.ColorKeyed: {
				return self._materialBuilder.CreateColorKeyedMaterial(new ColorKeyedMaterialCreationConfig {
					KeyMap = resolver.ResolveTexture(ownerKind, ownerIndex, BakedReferenceSlot.KeyMap),
					BlendOutputAlphaWithScene = assetData.Extract<bool>(MaterialBakingSchema.BlendOutputAlphaWithScene),
					Name = name
				});
			}
			case MaterialBakingSchema.BakedMaterialKind.Standard: {
				return self._materialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
					ColorMap = resolver.ResolveTexture(ownerKind, ownerIndex, BakedReferenceSlot.ColorMap),
					NormalMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.NormalMap),
					OcclusionRoughnessMetallicReflectanceMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.OrmrMap),
					AnisotropyMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.AnisotropyMap),
					EmissiveMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.EmissiveMap),
					ClearCoatMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.ClearCoatMap),
					AlphaMode = assetData.Extract<StandardMaterialAlphaMode>(MaterialBakingSchema.AlphaMode),
					Name = name,
					EnablePerInstanceEffects = enablePerInstanceEffects
				});
			}
			case MaterialBakingSchema.BakedMaterialKind.Transmissive: {
				return self._materialBuilder.CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
					ColorMap = resolver.ResolveTexture(ownerKind, ownerIndex, BakedReferenceSlot.ColorMap),
					AbsorptionTransmissionMap = resolver.ResolveTexture(ownerKind, ownerIndex, BakedReferenceSlot.AbsorptionTransmissionMap),
					NormalMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.NormalMap),
					OcclusionRoughnessMetallicReflectanceMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.OrmrMap),
					AnisotropyMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.AnisotropyMap),
					EmissiveMap = resolver.ResolveOptionalTexture(ownerKind, ownerIndex, BakedReferenceSlot.EmissiveMap),
					RefractionThickness = assetData.Extract<float>(MaterialBakingSchema.RefractionThickness),
					Quality = assetData.Extract<TransmissiveMaterialQuality>(MaterialBakingSchema.TransmissiveQuality),
					AlphaMode = assetData.Extract<TransmissiveMaterialAlphaMode>(MaterialBakingSchema.AlphaMode),
					Name = name,
					EnablePerInstanceEffects = enablePerInstanceEffects
				});
			}
			default:
				throw new AssetBakeException($"Baked material declares unknown material kind '{kind}'.");
		}
	}
	#endregion
}
