// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

sealed unsafe class LocalFontLoader : IFontImplProvider, IResourceDirectory<Font>, IDisposable {
	readonly record struct AtlasRuneData(XYPair<float> NibOffset, XYPair<float> QuadSize);
	readonly record struct FontData(Texture Atlas, ArrayPoolBackedMap<Rune, AtlasRuneData> RuneMap, ArrayPoolBackedMap<nuint, PenData> ActivePens);
	readonly record struct PenData(ResourceHandle<Font> OwningFont, Material Material);

	const string DefaultFontName = "Unnamed Font";
	const int SdfRenderPadding = 5;
	const int AdditionalAtlasGlyphCellPadding = 2; // Any lower risks interference from bilinear filtering
	const byte SdfOnEdgeValue = 180;
	const float SdfPixelDistScale = SdfOnEdgeValue / (float) SdfRenderPadding;
	readonly int[] _atlasSizeTargets = [ 1024, 2048, 4096, 8192 ];
	readonly float[] _sdfRenderHeightTargets = [ 64f, 48f, 32f ];
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalAssetLoaderConfig _config;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalTextureBuilder _textureBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly MapPool<Rune, AtlasRuneData> _coordsMapPool = new(false);
	readonly MapPool<nuint, PenData> _penMapPool = new(false);
	readonly ArrayPoolBackedMap<Rune, int> _fontLoadRuneToGlyphMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Font>, FontData> _activeFonts = new();
	nuint _nextHandleId = 0;
	bool _isDisposed = false;

	public LocalFontLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config, LocalMeshBuilder meshBuilder, LocalTextureBuilder textureBuilder, LocalMaterialBuilder materialBuilder) {
		_globals = globals;
		_config = config;
		_meshBuilder = meshBuilder;
		_textureBuilder = textureBuilder;
		_materialBuilder = materialBuilder;
	}
	
	public Font LoadFont(BuiltInFont font, in FontCreationConfig config) {
		
	}
	public Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {

	}
	Font LoadFont(byte* ttfFileStreamPtr, int ttfFileStreamLengthBytes, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		var runes = config.SupportedRunes;

		FontLoad(
			ttfFileStreamPtr,
			ttfFileStreamLengthBytes, 
			0, 
			out var fontHandle
		).ThrowIfFailure();
		try {
			float chosenSdfRenderHeight;
			int chosenAtlasDimension;
			float chosenScalingConstant;

			_fontLoadRuneToGlyphMap.ClearWithoutZeroingMemory();
			for (var i = 0; i < config.SupportedRunes.Length; ++i) {
				FontGetCodepointGlyphIndex(
					fontHandle,
					config.SupportedRunes[i].Value,
					out var glyphIndex
				).ThrowIfFailure();
				_fontLoadRuneToGlyphMap[config.SupportedRunes[i]] = glyphIndex;
			}

			for (var i = 0; i < _atlasSizeTargets.Length; ++i) {
				var isLastSizeTarget = i == _atlasSizeTargets.Length - 1;
				var heightTargetRange = isLastSizeTarget 
					? new Range(0, ^0)
					: new Range(0, 1);

				var heightTargets = _sdfRenderHeightTargets.AsSpan(heightTargetRange);
				for (var j = 0; j < heightTargets.Length; ++j) {
					FontGetVerticalMetrics(
						fontHandle,
						heightTargets[j],
						out var scalingConstant,
						out _,
						out _,
						out _
					).ThrowIfFailure();

					// TODO here use stbtt_GetGlyphBitmapBox -- but need to add padding ourselves
				}

				if (isLastSizeTarget) {
					throw new InvalidOperationException(
						$"Can not load font because the requested rune set " +
						$"({config.SupportedRunes.Length} runes) is too large " +
						$"to fit in a pre-baked font atlas. TinyFFR does not yet " +
						$"support dynamic font atlas writing for larger rune sets."
					);
				}
			}
			









			

			var cellSize = (int) MathF.Ceiling(glyphPixelHeight) + 2 * SdfRenderPadding + AdditionalAtlasGlyphCellPadding;
			var columns = (int) MathF.Ceiling(MathF.Sqrt(count));
			var rows = (count + columns - 1) / columns;
			var atlasWidth = columns * cellSize;
			var atlasHeight = rows * cellSize;
			var area = atlasWidth * atlasHeight;

			using var coveragePool = _globals.HeapPool.Borrow<byte>(area);
			var coverageSpan = coveragePool.Span;
			coverageSpan.Clear();

			var coordsMap = _coordsMapPool.Rent();

			for (var i = 0; i < count; ++i) {
				var codepoint = runes[i].Value;

				FontContainsCodepoint(fontHandle, codepoint, out var isIncluded).ThrowIfFailure();
				if (!isIncluded) continue; // Missing runes are simply left out of the map

				var cellX = (i % columns) * cellSize;
				var cellY = (i / columns) * cellSize;

				FontGenerateSdfBuffer(fontHandle, codepoint, scale, SdfRenderPadding, SdfOnEdgeValue, SdfPixelDistScale, out var glyphWidth, out var glyphHeight, out _, out _, out var sdfPtr).ThrowIfFailure();

				// A null buffer indicates a glyph with no contours (e.g. whitespace); record an empty rect at the cell origin
				if (sdfPtr == null) {
					coordsMap.Add(runes[i], new AtlasRuneData(new((float) cellX / atlasWidth, (float) cellY / atlasHeight), XYPair<float>.Zero));
					continue;
				}

				try {
					// Guard against oversized glyphs bleeding in to neighbouring cells
					var copyWidth = Math.Min(glyphWidth, cellSize);
					var copyHeight = Math.Min(glyphHeight, cellSize);

					for (var row = 0; row < copyHeight; ++row) {
						new ReadOnlySpan<byte>(sdfPtr + row * glyphWidth, copyWidth)
							.CopyTo(coverageSpan.Slice((cellY + row) * atlasWidth + cellX, copyWidth));
					}

					coordsMap.Add(
						runes[i],
						new AtlasRuneData(
							new((float) cellX / atlasWidth, (float) cellY / atlasHeight),
							new((float) copyWidth / atlasWidth, (float) copyHeight / atlasHeight)
						)
					);
				}
				finally {
					FontFreeSdfBuffer(sdfPtr);
				}
			}

			using var rgbPool = _globals.HeapPool.Borrow<TexelRgb24>(area);
			var rgbSpan = rgbPool.Span;
			for (var i = 0; i < area; ++i) {
				var coverage = coverageSpan[i];
				rgbSpan[i] = new TexelRgb24(coverage, coverage, coverage);
			}

			var atlas = _textureBuilder.CreateTexture(
				(ReadOnlySpan<TexelRgb24>) rgbSpan,
				new TextureGenerationConfig { Dimensions = new(atlasWidth, atlasHeight) },
				new TextureCreationConfig {
					IsLinearColorspace = true,
					GenerateMipMaps = false,
					RenderingConfig = new(disableTextureRepeat: true, disableTexelBlending: false, Quality.Standard),
					Name = config.Name
				}
			);

			var pens = _penMapPool.Rent();
			_nextHandleId++;
			var handle = new ResourceHandle<Font>(_nextHandleId);
			_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultFontName);
			_activeFonts.Add(handle, new FontData(atlas, coordsMap, pens));
			return HandleToInstance(handle);
		}
		finally {
			FontDispose(fontHandle).ThrowIfFailure();
		}
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
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_load")]
	static extern InteropResult FontLoad(
		byte* fontData,
		int fontDataLength,
		int fontIndex,
		out UIntPtr outFontHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_vertical_metrics")]
	static extern InteropResult FontGetVerticalMetrics(
		UIntPtr fontHandle,
		float pixelHeight,
		out float outScalingConstant,
		out int outAscent,
		out int outDescent,
		out int outLineGap
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_codepoint_glyph_index")]
	[SuppressGCTransition]
	static extern InteropResult FontGetCodepointGlyphIndex(
		UIntPtr fontHandle,
		int codepoint,
		out int outGlyphIndex
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_generate_sdf_buffer")]
	static extern InteropResult FontGenerateSdfBuffer(
		UIntPtr fontHandle,
		int glyphIndex,
		float scalingConstant,
		int padding,
		byte onedgeValue,
		float pixelDistScale,
		out int outWidth,
		out int outHeight,
		out int outXOff,
		out int outYOff,
		out byte* outPotentialBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_free_sdf_buffer")]
	static extern InteropResult FontFreeSdfBuffer(
		byte* bufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_dispose")]
	static extern InteropResult FontDispose(
		UIntPtr fontHandle
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
			_fontLoadRuneToGlyphMap.Dispose();

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