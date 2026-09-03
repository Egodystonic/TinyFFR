// Created on 2026-09-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

namespace Egodystonic.TinyFFR.Assets.Local;

partial class LocalAssetLoader {
	readonly ref struct BakedAssetResolver {
		public readonly ReadOnlySpan<AssetPoolSchema.BakedReferenceEntry> References;
		public readonly ResourceGroup Resources;
		public readonly LoadedBakedAsset AssetData;
		public readonly LocalAssetLoader Self;

		public BakedAssetResolver(LoadedBakedAsset assetData, ResourceGroup resourceGroup, LocalAssetLoader self) {
			AssetData = assetData;
			References = assetData.ExtractSpan<AssetPoolSchema.BakedReferenceEntry>(AssetPoolSchema.ReferenceTable, default);
			Resources = resourceGroup;
			Self = self;
		}
		
		public void MaterializeAll() {
			Span<char> sectionNameBuffer = stackalloc char[AssetPoolSchema.MaxPoolSectionNameLength];

			var textureCount = AssetData.Extract(AssetPoolSchema.TextureCount, 0);
			for (var i = 0; i < textureCount; ++i) {
				var subAsset = AssetData.ExtractSubAsset<Texture>(AssetPoolSchema.WriteEntrySectionName(sectionNameBuffer, BakedPoolKind.Texture, i));
				var texture = CreateTextureFromBakedAsset(Self, subAsset, subAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default));
				Resources.Add(texture);
			}

			var materialCount = AssetData.Extract(AssetPoolSchema.MaterialCount, 0);
			for (var i = 0; i < materialCount; ++i) {
				var subAsset = AssetData.ExtractSubAsset<Material>(AssetPoolSchema.WriteEntrySectionName(sectionNameBuffer, BakedPoolKind.Material, i));
				var material = CreateMaterialFromBakedAsset(Self, subAsset, subAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default), BakedPoolKind.Material, i, in this);
				Resources.Add(material);
			}

			var meshCount = AssetData.Extract(AssetPoolSchema.MeshCount, 0);
			for (var i = 0; i < meshCount; ++i) {
				var subAsset = AssetData.ExtractSubAsset<Mesh>(AssetPoolSchema.WriteEntrySectionName(sectionNameBuffer, BakedPoolKind.Mesh, i));
				var mesh = CreateMeshFromBakedAsset(Self, subAsset, subAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default));
				Resources.Add(mesh);
			}

			var modelCount = AssetData.Extract(AssetPoolSchema.ModelCount, 0);
			for (var i = 0; i < modelCount; ++i) {
				var subAsset = AssetData.ExtractSubAsset<Model>(AssetPoolSchema.WriteEntrySectionName(sectionNameBuffer, BakedPoolKind.Model, i));
				var model = CreateModelFromBakedAsset(Self, subAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default), BakedPoolKind.Model, i, in this);
				Resources.Add(model);
			}
		}

		bool TryResolveBakedReference(BakedPoolKind ownerKind, int ownerIndex, BakedReferenceSlot slot, out AssetPoolSchema.BakedReferenceEntry result) {
			foreach (var entry in References) {
				if (entry.OwnerKind != (int) ownerKind || entry.OwnerIndex != ownerIndex || entry.Slot != (int) slot) continue;
				result = entry;
				return true;
			}
			result = default;
			return false;
		}

		static int GetValidatedBakeTargetIndex(AssetPoolSchema.BakedReferenceEntry entry, BakedPoolKind expectedKind, int poolCount, BakedReferenceSlot slot) {
			if (entry.TargetKind != (int) expectedKind) {
				throw new AssetBakeException($"Baked asset reference '{slot}' targets a resource of kind '{(BakedPoolKind) entry.TargetKind}' but kind '{expectedKind}' was expected.");
			}
			if (entry.TargetIndex < 0 || entry.TargetIndex >= poolCount) {
				throw new AssetBakeException($"Baked asset reference '{slot}' targets index {entry.TargetIndex} which is outside the '{expectedKind}' pool of {poolCount} resource(s).");
			}
			return entry.TargetIndex;
		}

		public Texture? ResolveOptionalTexture(BakedPoolKind ownerKind, int ownerIndex, BakedReferenceSlot slot) {
			if (!TryResolveBakedReference(ownerKind, ownerIndex, slot, out var entry)) return null;
			return Resources.Textures[GetValidatedBakeTargetIndex(entry, BakedPoolKind.Texture, Resources.Textures.Count, slot)];
		}

		public Texture ResolveTexture(BakedPoolKind ownerKind, int ownerIndex, BakedReferenceSlot slot) {
			return ResolveOptionalTexture(ownerKind, ownerIndex, slot)
				?? throw new AssetBakeException($"Baked asset is missing required texture reference '{slot}'.");
		}

		public Mesh ResolveMesh(BakedPoolKind ownerKind, int ownerIndex, BakedReferenceSlot slot) {
			if (!TryResolveBakedReference(ownerKind, ownerIndex, slot, out var entry)) {
				throw new AssetBakeException($"Baked asset is missing required mesh reference '{slot}'.");
			}
			return Resources.Meshes[GetValidatedBakeTargetIndex(entry, BakedPoolKind.Mesh, Resources.Meshes.Count, slot)];
		}

		public Material ResolveMaterial(BakedPoolKind ownerKind, int ownerIndex, BakedReferenceSlot slot) {
			if (!TryResolveBakedReference(ownerKind, ownerIndex, slot, out var entry)) {
				throw new AssetBakeException($"Baked asset is missing required material reference '{slot}'.");
			}
			return Resources.Materials[GetValidatedBakeTargetIndex(entry, BakedPoolKind.Material, Resources.Materials.Count, slot)];
		}
	}
}