// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Baking;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Rendering.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;
using static Egodystonic.TinyFFR.Assets.Baking.BakedResourceSchemata;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader : IResourceDirectory<Font> {
	readonly LocalFontLoader _fontLoader;

	public Font LoadFont(BuiltInFont font, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFont(font, in config);
	}
	public Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFont(fontFilePath, in config);
	}

	public TinyFfrAsyncOperation<Font> LoadFontAsync(BuiltInFont font, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFontAsync(font, in config);
	}
	public TinyFfrAsyncOperation<Font> LoadFontAsync(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		return _fontLoader.LoadFontAsync(fontFilePath, in config);
	}

	IndirectEnumerable<object, Font> IResourceDirectory<Font>.AllActiveInstances {
		get {
			ThrowIfThisIsDisposed();
			return _fontLoader.AllActiveInstances;
		}
	}
	bool IResourceDirectory<Font>.ResourceNameMatchIsMatching(Font resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		ThrowIfThisIsDisposed();
		return _fontLoader.ResourceNameMatchIsMatching(resource, name, allowPartialMatch, comparisonType);
	}
	
	#region Baking
	public Font LoadBakedFont(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.Load<Font, Font, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedFontCore);
	}

	public TinyFfrAsyncOperation<Font> LoadBakedFontAsync(ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> name = default) {
		return _globals.Bakery.LoadAsync<Font, Font, LocalAssetLoader>(this, bakedAssetFilePath, name, &LoadBakedFontCore);
	}

	static Font LoadBakedFontCore(LocalAssetBakery.AssetLoadContext ctx) {
		static Font Finalize(LocalAssetBakery.AssetLoadContext ctx) {
			ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

			var assetData = ctx.AssetData;
			var self = ctx.Invoker<LocalAssetLoader>();
			var name = ctx.StoredOrOverridingName;

			var atlasAsset = assetData.ExtractSubAsset<Texture>(FontBakingSchema.Atlas);
			var atlas = CreateTextureFromBakedAsset(self, atlasAsset, atlasAsset.ExtractString(LocalAssetBakery.ResourceNameSectionName, default));

			try {
				return self._fontLoader.CreateFontFromBakedData(
					atlas,
					assetData.Extract<float>(FontBakingSchema.Ascent),
					assetData.Extract<float>(FontBakingSchema.Descent),
					assetData.Extract<float>(FontBakingSchema.LineAdvance),
					new Rune(assetData.Extract<int>(FontBakingSchema.LineBreakRune)),
					assetData.ExtractSpan<FontBakingSchema.BakedRuneEntry>(FontBakingSchema.RuneMap),
					assetData.ExtractSpan<FontBakingSchema.BakedKerningEntry>(FontBakingSchema.KerningMap),
					name
				);
			}
			catch {
				atlas.Dispose();
				throw;
			}
		}

		return ctx.GenerateResourceOnPrimaryAndWait(&Finalize);
	}
	#endregion
}