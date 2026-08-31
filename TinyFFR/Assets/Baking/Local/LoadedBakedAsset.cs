// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Baking;

readonly record struct BakedAssetStreamSection(Type Type, int DataStartIndex, int DataLength, LoadedBakedAsset? SubAsset);

sealed unsafe class LoadedBakedAsset : IDisposable {
	readonly LocalAssetBakery _owningBakery;
	readonly delegate* managed<LocalAssetBakery, LoadedBakedAsset, void> _disposalFunc;
	
	public Type Type { get; set; } = null!;
	public ArrayPoolBackedStringKeyMap<BakedAssetStreamSection> Sections { get; } = new();
	public PooledHeapMemory<byte> Stream { get; set; } = default;

	public LoadedBakedAsset(LocalAssetBakery owningBakery, delegate*<LocalAssetBakery, LoadedBakedAsset, void> disposalFunc) {
		_owningBakery = owningBakery;
		_disposalFunc = disposalFunc;
	}
	
	public T Extract<T>(ReadOnlySpan<char> sectionTitle) where T : unmanaged {
		if (!Sections.TryGetValue(sectionTitle, out var section)) throw new AssetBakeException($"Missing required section title '{sectionTitle}'.");
		if (section.Type != typeof(T)) throw new AssetBakeException($"Section '{sectionTitle}' was required to represent a value of type '{typeof(T).Name}' but instead represented a value of type '{section.Type.Name}'.");
		try {
			return MemoryMarshal.Cast<byte, T>(Stream.Span[section.DataStartIndex..(section.DataStartIndex + section.DataLength)])[0];
		}
		catch (Exception e) {
			throw new AssetBakeException($"Required section '{sectionTitle}' data was corrupted.", e);
		}
	}
	public T Extract<T>(ReadOnlySpan<char> sectionTitle, T fallback) where T : unmanaged {
		if (!Sections.TryGetValue(sectionTitle, out var section) || section.Type != typeof(T)) return fallback;
		try {
			return MemoryMarshal.Cast<byte, T>(Stream.Span[section.DataStartIndex..(section.DataStartIndex + section.DataLength)])[0];
		}
		catch (Exception e) when (e is ArgumentException or OverflowException) {
			return fallback;
		}
	}
	
	public LoadedBakedAsset ExtractSubAsset<TResource>(ReadOnlySpan<char> sectionTitle) where TResource : IResource {
		if (!Sections.TryGetValue(sectionTitle, out var section)) throw new AssetBakeException($"Missing required section title '{sectionTitle}'.");
		if (section.Type != typeof(TResource)) throw new AssetBakeException($"Section '{sectionTitle}' was required to represent a value of type '{typeof(TResource).Name}' but instead represented a value of type '{section.Type.Name}'.");
		return section.SubAsset ?? throw new AssetBakeException($"Required section '{sectionTitle}' contained empty sub-asset data.");
	}
	public LoadedBakedAsset? TryExtractSubAsset<TResource>(ReadOnlySpan<char> sectionTitle) where TResource : IResource {
		if (!Sections.TryGetValue(sectionTitle, out var section)) return null;
		if (section.Type != typeof(TResource)) return null;
		return section.SubAsset;
	}
	
	public void Dispose() => _disposalFunc(_owningBakery, this);
}