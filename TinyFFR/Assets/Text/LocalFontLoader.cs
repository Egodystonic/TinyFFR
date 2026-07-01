// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Text;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

sealed class LocalFontLoader : IFontImplProvider, IResourceDirectory<Font>, IDisposable {
	readonly record struct FontData(Texture Atlas, ArrayPoolBackedMap<Rune, XYPair<float>> CoordsMap, ArrayPoolBackedMap<nuint, PenData> ActivePens);
	readonly record struct PenData(ResourceHandle<Font> OwningFont, Material Material);
	
	const string DefaultFontName = "Unnamed Font";
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalAssetLoaderConfig _config;
	readonly MapPool<Rune, XYPair<float>> _coordsMapPool = new(false);
	readonly MapPool<nuint, PenData> _penMapPool = new(false);
	readonly ArrayPoolBackedMap<ResourceHandle<Font>, FontData> _activeFonts = new();
	bool _isDisposed = false;

	public LocalFontLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		_globals = globals;
		_config = config;
	}
	
	public Font LoadFont(BuiltInFont font, in FontCreationConfig config) {
		
	}
	public Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		
	}

	public FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessMultiplier, ColorVect glowColor, float glowSizeMultiplier) {
		
	}

	public Material GetPenMaterial(ResourceHandle<Font> handle, UIntPtr penHandle) {
		
	}

	public XYPair<int> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> str) {
		
	}

	public TextInstance CreateTextInstance(ResourceHandle<Font> handle) {
		
	}
	
	public void DisposeTextInstance(ResourceHandle<Font> handle, TextInstance instance) {
		
	}

	public string GetNameAsNewStringObject(ResourceHandle<Font> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultFontName));
	}
	public int GetNameLength(ResourceHandle<Font> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultFontName).Length;
	}
	public void CopyName(ResourceHandle<Font> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultFontName, destinationBuffer);
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "dispose_shader_package")]
	static extern InteropResult DisposeShaderPackage(
		UIntPtr packageHandle
	);
	#endregion
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Font HandleToInstance(ResourceHandle<Font> h) => new(h, this);
	
	#region Resource Directory
	public unsafe IndirectEnumerable<object, Font> AllActiveInstances {
		get {
			static LocalFontLoader CastSelf(object self) => self as LocalFontLoader ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) => CastSelf(self)._activeFonts.Count;
			static int GetVersion(object self) => CastSelf(self)._activeFonts.Version;
			static Font GetItem(object self, int index) => CastSelf(self).HandleToInstance(CastSelf(self)._activeFonts.GetPairAtIndex(index).Key);

			ThrowIfThisIsDisposed();
			return new(
				this,
				GetVersion(this),
				&GetCount,
				&GetVersion,
				&GetItem
			);
		}
	}
	public bool ResourceNameMatchIsMatching(Font resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		ThrowIfThisOrHandleIsDisposed(handle);
		return allowPartialMatch
			? _globals.GetResourceName(handle.Ident, DefaultFontName).Contains(name, comparisonType)
			: _globals.GetResourceName(handle.Ident, DefaultFontName).Equals(name, comparisonType);
	}
	#endregion
	
	#region Disposal
	public bool IsDisposed(ResourceHandle<Font> handle) => _isDisposed || !_activeFonts.ContainsKey(handle);
	
	public void Dispose(ResourceHandle<Font> handle) => Dispose(handle, true);
	public void Dispose(ResourceHandle<Font> handle, nuint penHandle) => Dispose(handle, penHandle, true);
	
	void Dispose(ResourceHandle<Font> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var data = _activeFonts[handle];
		
		foreach (var penHandle in data.ActivePens.Keys) {
			Dispose(handle, penHandle, false);
		}
		
		data.Atlas.Dispose();
		_penMapPool.Return(data.ActivePens);
		_coordsMapPool.Return(data.CoordsMap);
		
		if (removeFromMap) _activeFonts.Remove(handle);
	}
	void Dispose(ResourceHandle<Font> handle, nuint penHandle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var fontData = _activeFonts[handle];
		if (!fontData.ActivePens.TryGetValue(penHandle, out var penData)) return;
		
		penData.Material.Dispose();
		if (removeFromMap) fontData.ActivePens.Remove(penHandle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			foreach (var kvp in _activeFonts) { 
				Dispose(kvp.Key, false);
			}
			
			_activeFonts.Dispose();
			
			_penMapPool.Dispose();
			_coordsMapPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Font> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Font));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}