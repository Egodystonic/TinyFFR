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
	readonly record struct AtlasRuneData(XYPair<float> AtlasUVOffset, XYPair<float> AtlasUVSize, XYPair<float> NibOffset);
	readonly record struct FontData(Texture Atlas, ArrayPoolBackedMap<Rune, AtlasRuneData> RuneMap, ArrayPoolBackedMap<nuint, PenData> ActivePens);
	readonly record struct PenData(ResourceHandle<Font> OwningFont, Material Material);

	const string DefaultFontName = "Unnamed Font";
	const int SdfRenderPadding = 5;
	const int AdditionalAtlasGlyphCellPadding = 2; // Any lower risks interference from bilinear filtering
	const byte SdfOnEdgeValue = 180;
	const float SdfPixelDistScale = SdfOnEdgeValue / (float) SdfRenderPadding;
	readonly int[] _atlasSizeTargets = [ 1024, 2048, 4096, 8192 ];
	readonly int[] _sdfRenderHeightTargets = [ 64, 48, 32 ];
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalAssetLoaderConfig _config;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalTextureBuilder _textureBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly MapPool<Rune, AtlasRuneData> _runeMapPool = new(false);
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
	int GetAtlasCellHeight(int renderHeight) => renderHeight + SdfRenderPadding * 2 + AdditionalAtlasGlyphCellPadding;
	(int RenderHeight, float ScalingConstant, int AtlasDimension) DetermineAppropriateFontRenderMetrics(UIntPtr fontHandle, in FontCreationConfig config) {
		// Assumes _fontLoadRuneToGlyphMap has been populated
		
		for (var i = 0; i < _atlasSizeTargets.Length; ++i) {
			var heightTargetRange = i == _atlasSizeTargets.Length - 1 
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
				
				var cellHeight = GetAtlasCellHeight(heightTargets[j]);
				
				var numRowsRemaining = _atlasSizeTargets[i] / cellHeight;
				var texelsRemainingInCurrentRow = _atlasSizeTargets[i];
				
				for (var r = 0; r < config.SupportedRunes.Length; ++r) {
					FontGetSdfBufferDimensions(
						fontHandle,
						_fontLoadRuneToGlyphMap[config.SupportedRunes[r]],
						scalingConstant,
						SdfRenderPadding,
						out var width,
						out _
					).ThrowIfFailure();
					
					if (width <= 0) continue;
					
					texelsRemainingInCurrentRow -= width;
					while (texelsRemainingInCurrentRow < 0) {
						--numRowsRemaining;
						if (numRowsRemaining == 0) goto atlasTooSmall;
						texelsRemainingInCurrentRow += _atlasSizeTargets[i];
					}
				}
				
				return (heightTargets[j], scalingConstant, _atlasSizeTargets[i]);
				atlasTooSmall: continue;
			}
		}
		
		throw new InvalidOperationException(
			$"Can not load font because the requested rune set " +
			$"({config.SupportedRunes.Length} runes) is too large " +
			$"to fit in a pre-baked font atlas. TinyFFR does not yet " +
			$"support dynamic font atlas writing for larger rune sets."
		);
	}
	Font LoadFont(byte* ttfFileStreamPtr, int ttfFileStreamLengthBytes, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();

		FontLoad(
			ttfFileStreamPtr,
			ttfFileStreamLengthBytes, 
			0, 
			out var fontHandle
		).ThrowIfFailure();
		try {
			var runes = config.SupportedRunes;
		
			_fontLoadRuneToGlyphMap.ClearWithoutZeroingMemory();
			for (var r = 0; r < runes.Length; ++r) {
				FontGetCodepointGlyphIndex(
					fontHandle,
					runes[r].Value,
					out var glyphIndex
				).ThrowIfFailure();
				_fontLoadRuneToGlyphMap[runes[r]] = glyphIndex;
			}
			
			var runeMap = _runeMapPool.Rent();
			var (renderHeight, scalingConstant, atlasDimension) = DetermineAppropriateFontRenderMetrics(fontHandle, in config);
			var atlasDimensionReciprocal = 1f / atlasDimension;
			var cellHeight = GetAtlasCellHeight(renderHeight);
			using var texelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(atlasDimension * atlasDimension);
			var nib = XYPair<int>.Zero;
			
			for (var r = 0; r < runes.Length; ++r) {
				byte* potentialBufferPtr = null;
				try {
					FontGenerateSdfBuffer(
						fontHandle,
						_fontLoadRuneToGlyphMap[runes[r]],
						scalingConstant,
						SdfRenderPadding,
						SdfOnEdgeValue,
						SdfPixelDistScale,
						out var bufferWidth,
						out var bufferHeight,
						out var xOffset,
						out var yOffset,
						out potentialBufferPtr
					).ThrowIfFailure();
					
					if (potentialBufferPtr == null || bufferWidth == 0 || bufferHeight == 0) continue;
					if (nib.X + bufferWidth > atlasDimension) {
						nib = (0, nib.Y + cellHeight);
						if (nib.Y + cellHeight > atlasDimension) {
							throw new InvalidOperationException("Ran out of atlas space when generating font (this is a bug in TinyFFR).");
						}
					}
					
					bufferHeight = Int32.Min(bufferHeight, cellHeight);
					for (var row = 0; row < bufferHeight; ++row) {
						var verticalNibOffset = bufferHeight - (row + 1); // Because stb returns bitmap in top-to-bottom order
						var spanStartIndex = (verticalNibOffset + nib.Y) * atlasDimension + nib.X;
						var bufferStartIndex = row * bufferWidth;
						for (var column = 0; column < bufferWidth; ++column) {
							var sdfValue = potentialBufferPtr[bufferStartIndex + column];
							texelBuffer.Span[spanStartIndex + column] = new TexelRgb24(sdfValue, sdfValue, sdfValue);
						}
					}
					
					runeMap[runes[r]] = new AtlasRuneData(
						nib.Cast<float>() * atlasDimensionReciprocal,
						new XYPair<float>(bufferWidth, bufferHeight) * atlasDimensionReciprocal, 
						new XYPair<float>(xOffset, yOffset) * atlasDimensionReciprocal
					);
					
					nib = nib with { X = nib.X + bufferWidth };
				}
				finally {
					if (potentialBufferPtr != null) FontFreeSdfBuffer(potentialBufferPtr).ThrowIfFailure();
				}
			}

			var atlas = _textureBuilder.CreateTexture(
				texelBuffer.Span,
				new TextureGenerationConfig {
					Dimensions = new(atlasDimension, atlasDimension)
				},
				new TextureCreationConfig {
					IsLinearColorspace = true,
					GenerateMipMaps = true,
					RenderingConfig = new(disableTextureRepeat: true, disableTexelBlending: false, Quality.Standard),
					Name = config.Name
				}
			);

			_nextHandleId++;
			var handle = new ResourceHandle<Font>(_nextHandleId);
			_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultFontName);
			_activeFonts.Add(handle, new FontData(atlas, runeMap, _penMapPool.Rent()));
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_sdf_buffer_dimensions")]
	static extern InteropResult FontGetSdfBufferDimensions(
		UIntPtr fontHandle,
		int glyphIndex,
		float scalingConstant,
		int padding,
		out int outWidth,
		out int outHeight
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
		_runeMapPool.Return(data.RuneMap);
		
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
			_runeMapPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Font> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Font));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}