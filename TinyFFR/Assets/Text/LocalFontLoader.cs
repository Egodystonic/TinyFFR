// Created on 2026-06-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Assets.Text;

sealed unsafe class LocalFontLoader : IFontImplProvider, IResourceDirectory<Font>, IDisposable {
	readonly record struct RenderedTextData(ManagedStringPool.RentedStringHandle Text, PooledHeapMemory<MeshVertex> Vertices, PooledHeapMemory<VertexTriangle> Triangles, XYPair<float> Size);
	readonly record struct AtlasRuneData(XYPair<float> AtlasUVOffset, XYPair<float> AtlasUVSize, XYPair<float> NibOffset, float AdvanceWidth);
	readonly record struct FontData(Texture Atlas, float Ascent, float Descent, ArrayPoolBackedMap<Rune, AtlasRuneData> RuneMap, ArrayPoolBackedMap<ulong, float> KerningMap, ArrayPoolBackedMap<nuint, PenData> ActivePens, ArrayPoolBackedMap<nuint, StringData> ActiveStrings, ArrayPoolBackedLruCache<int, RenderedTextData> RenderedTextCache);
	readonly record struct PenData(ResourceHandle<Font> OwningFont, Material Material);
	readonly record struct StringData(ResourceHandle<Font> OwningFont, Mesh Mesh, XYPair<float> Size);

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
	readonly MapPool<ulong, float> _kerningMapPool = new(false);
	readonly MapPool<nuint, PenData> _penMapPool = new(false);
	readonly MapPool<nuint, StringData> _stringMapPool = new(false);
	readonly ObjectPool<ArrayPoolBackedLruCache<int, RenderedTextData>, LocalFontLoader> _renderedTextCachePool;
	readonly ArrayPoolBackedMap<Rune, int> _fontLoadRuneToGlyphMap = new();
	readonly ArrayPoolBackedMap<ResourceHandle<Font>, FontData> _activeFonts = new();
	nuint _prevHandleId = 0U;
	bool _isDisposed = false;
	
	static ArrayPoolBackedLruCache<int, RenderedTextData> CreateNewTextCache(LocalFontLoader @this) => new(@this._config.MaxCachedTextMeshesPerFont, &TextCacheEvictionHandler, @this);
	static void TextCacheEvictionHandler(object? localFontLoader, int textHash, RenderedTextData data) {
		var @this = (LocalFontLoader) localFontLoader!;
		@this._globals.StringPool.Return(data.Text);
		data.Vertices.Dispose();
		data.Triangles.Dispose();
	}

	static ulong PackRunePair(Rune first, Rune second) => ((ulong) (uint) first.Value << 32) | (uint) second.Value;

	public LocalFontLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config, LocalMeshBuilder meshBuilder, LocalTextureBuilder textureBuilder, LocalMaterialBuilder materialBuilder) {
		_globals = globals;
		_config = config;
		_meshBuilder = meshBuilder;
		_textureBuilder = textureBuilder;
		_materialBuilder = materialBuilder;
		_renderedTextCachePool = new(&CreateNewTextCache, this);
	}
	
	public Font LoadFont(BuiltInFont font, in FontCreationConfig config) {
		throw new NotImplementedException();
	}
	public Font LoadFont(ReadOnlySpan<char> fontFilePath, in FontCreationConfig config) {
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		try {
			var fontFileDataStream = new FileStream(fontFilePath.ToString(), FileMode.Open, FileAccess.Read, FileShare.Read);
			var streamLengthBytes = checked((int) fontFileDataStream.Length);
			using var fileBuffer = _globals.HeapPool.Borrow<byte>(streamLengthBytes);
			fontFileDataStream.ReadExactly(fileBuffer.Span);
			fontFileDataStream.Dispose();
			
			fixed (byte* bufferPtr = fileBuffer.Span) {
				return config.Name.IsEmpty
					? LoadFont(bufferPtr, streamLengthBytes, config with { Name = Path.GetFileName(fontFilePath) })
					: LoadFont(bufferPtr, streamLengthBytes, in config);
			}
		}
		catch (Exception e) {
			if (!File.Exists(fontFilePath.ToString())) throw new InvalidOperationException($"File '{fontFilePath}' does not exist.", e);
			throw new InvalidOperationException("Error occured when reading and/or loading font file.", e);
		}
	}
	int GetAtlasCellHeight(int renderHeight) => renderHeight + SdfRenderPadding * 2 + AdditionalAtlasGlyphCellPadding;
	(int RenderHeight, float ScalingConstant, int AtlasDimension, float Ascent, float Descent) DetermineAppropriateFontRenderMetrics(UIntPtr fontHandle, in FontCreationConfig config) {
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
					out var ascent,
					out var descent,
					out _
				).ThrowIfFailure();
				
				var cellHeight = GetAtlasCellHeight(heightTargets[j]);
				
				var numRowsRemaining = _atlasSizeTargets[i] / cellHeight;
				var texelsRemainingInCurrentRow = _atlasSizeTargets[i];
				
				for (var r = 0; r < config.SupportedRunes.Length; ++r) {
					var glyphIndex = _fontLoadRuneToGlyphMap[config.SupportedRunes[r]];
					if (glyphIndex == 0) continue;

					FontGetSdfBufferDimensions(
						fontHandle,
						glyphIndex,
						scalingConstant,
						SdfRenderPadding,
						out var width,
						out _
					).ThrowIfFailure();
					
					if (width <= 0) continue;
					if (width > _atlasSizeTargets[i]) goto atlasTooSmall;
					
					if (width > texelsRemainingInCurrentRow) {
						--numRowsRemaining;
						if (numRowsRemaining == 0) goto atlasTooSmall;
						texelsRemainingInCurrentRow = _atlasSizeTargets[i];
					}
					texelsRemainingInCurrentRow -= width + AdditionalAtlasGlyphCellPadding;
				}
				
				return (heightTargets[j], scalingConstant, _atlasSizeTargets[i], ascent, descent);
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
			
			var (renderHeight, scalingConstant, atlasDimension, ascent, descent) = DetermineAppropriateFontRenderMetrics(fontHandle, in config);
			var atlasDimensionReciprocal = 1f / atlasDimension;
			var cellHeight = GetAtlasCellHeight(renderHeight);
			var runeMap = _runeMapPool.Rent();
			var kerningMap = _kerningMapPool.Rent();
			try {
				using var texelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(atlasDimension * atlasDimension);
				texelBuffer.Span.Clear();
				var nib = XYPair<int>.Zero;
				
				for (var r = 0; r < runes.Length; ++r) {
					if (_fontLoadRuneToGlyphMap[runes[r]] == 0) continue;

					FontGetGlyphAdvance(fontHandle, _fontLoadRuneToGlyphMap[runes[r]], out var rawAdvance).ThrowIfFailure();
					var advanceWidth = rawAdvance * scalingConstant * atlasDimensionReciprocal;

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
						
						if (potentialBufferPtr == null || bufferWidth == 0 || bufferHeight == 0) {
							runeMap[runes[r]] = new AtlasRuneData(XYPair<float>.Zero, XYPair<float>.Zero, XYPair<float>.Zero, advanceWidth);
							continue;
						}
						if (nib.X + bufferWidth > atlasDimension) {
							nib = (0, nib.Y + cellHeight);
							if (bufferWidth > atlasDimension || nib.Y + cellHeight > atlasDimension) {
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
							new XYPair<float>(xOffset, yOffset) * atlasDimensionReciprocal,
							advanceWidth
						);
						
						nib = nib with { X = nib.X + bufferWidth + AdditionalAtlasGlyphCellPadding };
					}
					finally {
						if (potentialBufferPtr != null) FontFreeSdfBuffer(potentialBufferPtr).ThrowIfFailure();
					}
				}

				FontGetKerningDataPresent(fontHandle, out var fontHasKerningData).ThrowIfFailure();
				if (fontHasKerningData) {
					for (var first = 0; first < runes.Length; ++first) {
						var firstGlyphIndex = _fontLoadRuneToGlyphMap[runes[first]];
						if (firstGlyphIndex == 0) continue;

						for (var second = 0; second < runes.Length; ++second) {
							var secondGlyphIndex = _fontLoadRuneToGlyphMap[runes[second]];
							if (secondGlyphIndex == 0) continue;

							FontGetGlyphPairKernAdvance(fontHandle, firstGlyphIndex, secondGlyphIndex, out var rawKerningAdvance).ThrowIfFailure();
							if (rawKerningAdvance == 0) continue;
							kerningMap[PackRunePair(runes[first], runes[second])] = rawKerningAdvance * scalingConstant * atlasDimensionReciprocal;
						}
					}
				}

				var atlas = _textureBuilder.CreateTexture(
					texelBuffer.Span,
					new TextureGenerationConfig {
						Dimensions = new(atlasDimension, atlasDimension)
					},
					new TextureCreationConfig {
						IsLinearColorspace = true,
						GenerateMipMaps = false,
						RenderingConfig = new(disableTextureRepeat: true, disableTexelBlending: false, Quality.Standard),
						Name = config.Name
					}
				);

				_prevHandleId++;
				var handle = new ResourceHandle<Font>(_prevHandleId);
				_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, config.Name, DefaultFontName);
				_activeFonts.Add(handle, new FontData(atlas, ascent * scalingConstant * atlasDimensionReciprocal, descent * scalingConstant * atlasDimensionReciprocal, runeMap, kerningMap, _penMapPool.Rent(), _stringMapPool.Rent(), _renderedTextCachePool.Rent()));
				return HandleToInstance(handle);
			}
			catch {
				_runeMapPool.Return(runeMap);
				_kerningMapPool.Return(kerningMap);
				throw;
			}
		}
		finally {
			FontDispose(fontHandle).ThrowIfFailure();
		}
	}

	public XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var textHash = String.GetHashCode(text, StringComparison.Ordinal);
		if (fontData.RenderedTextCache.TryGet(textHash, out var cachedTextData) && cachedTextData.Text.AsSpan.SequenceEqual(text)) {
			return cachedTextData.Size;
		}
		
		return RenderAndCacheText(handle, text, textHash).Size;
	}

	public FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text) {
		const string NameJoiningString = " text \"";
		const string NameEndingString = "\"";
		
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var textHash = String.GetHashCode(text, StringComparison.Ordinal);
		if (!fontData.RenderedTextCache.TryGet(textHash, out var textData) || !textData.Text.AsSpan.SequenceEqual(text)) {
			textData = RenderAndCacheText(handle, text, textHash);
		}
		
		var nameLength = SpanUtils.GetConcatenatedLength(
			_globals.GetResourceName(handle.Ident, DefaultFontName),
			NameJoiningString,
			text,
			NameEndingString
		);
		using var nameBuffer = _globals.HeapPool.CreateSpanLease<char>(nameLength);
		SpanUtils.Concatenate(
			nameBuffer.Span,
			_globals.GetResourceName(handle.Ident, DefaultFontName),
			NameJoiningString,
			text,
			NameEndingString
		);
		var mesh = _meshBuilder.CreateMesh(
			textData.Vertices.Span, 
			textData.Triangles.Span, 
			new MeshCreationConfig {
				Name = nameBuffer.Span
			}
		);
		++_prevHandleId;
		fontData.ActiveStrings[_prevHandleId] = new StringData(handle, mesh, textData.Size);
		return new FontString(HandleToInstance(handle), _prevHandleId);
	}

	RenderedTextData RenderAndCacheText(ResourceHandle<Font> fontHandle, ReadOnlySpan<char> text, int textHash) {
		const int UnicodeReplacementChar = 0xFFFD;
		var runeCount = 0;
		foreach (var _ in text.EnumerateRunes()) ++runeCount;
		
		var vertexBuffer = _globals.HeapPool.Borrow<MeshVertex>(runeCount * 4);
		var triangleBuffer = _globals.HeapPool.Borrow<VertexTriangle>(runeCount * 2);
		var fontData = _activeFonts[fontHandle];
		var runeMap = fontData.RuneMap;
		var kerningMap = fontData.KerningMap;
		if (!runeMap.TryGetValue(new Rune(UnicodeReplacementChar), out var replacementRuneData)) {
			replacementRuneData = new AtlasRuneData(XYPair<float>.Zero, XYPair<float>.Zero, XYPair<float>.Zero, 0f);
		}

		// TODO Render background quads
		var runeIndex = 0;
		var nibHorizontalLocation = 0f;
		var tangentRotation = MeshVertex.CalculateTangentRotation(Direction.Right, Direction.Up, Direction.Backward);
		var horizontalSize = 0f;
		Rune? previousRune = null;
		foreach (var rune in text.EnumerateRunes()) {
			if (!runeMap.TryGetValue(rune, out var runeData)) runeData = replacementRuneData;

			if (previousRune is { } prev && kerningMap.TryGetValue(PackRunePair(prev, rune), out var kerningAdvance)) {
				nibHorizontalLocation -= kerningAdvance;
			}

			var quadStartPoint = new XYPair<float>(
				nibHorizontalLocation - runeData.NibOffset.X,
				-(runeData.NibOffset.Y + runeData.AtlasUVSize.Y)
			);

			vertexBuffer.Span[runeIndex * 4 + 0] = new MeshVertex(
				location: new(quadStartPoint.X, quadStartPoint.Y, 0f),
				textureCoords: runeData.AtlasUVOffset,
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[runeIndex * 4 + 1] = new MeshVertex(
				location: new(quadStartPoint.X - runeData.AtlasUVSize.X, quadStartPoint.Y, 0f),
				textureCoords: runeData.AtlasUVOffset with { X = runeData.AtlasUVOffset.X + runeData.AtlasUVSize.X },
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[runeIndex * 4 + 2] = new MeshVertex(
				location: new(quadStartPoint.X - runeData.AtlasUVSize.X, quadStartPoint.Y + runeData.AtlasUVSize.Y, 0f),
				textureCoords: runeData.AtlasUVOffset + runeData.AtlasUVSize,
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[runeIndex * 4 + 3] = new MeshVertex(
				location: new(quadStartPoint.X, quadStartPoint.Y + runeData.AtlasUVSize.Y, 0f),
				textureCoords: runeData.AtlasUVOffset with { Y = runeData.AtlasUVOffset.Y + runeData.AtlasUVSize.Y },
				tangentRotation: tangentRotation
			);
			
			triangleBuffer.Span[runeIndex * 2 + 0] = new VertexTriangle(runeIndex * 4 + 0, runeIndex * 4 + 1, runeIndex * 4 + 2);
			triangleBuffer.Span[runeIndex * 2 + 1] = new VertexTriangle(runeIndex * 4 + 0, runeIndex * 4 + 2, runeIndex * 4 + 3);
			
			horizontalSize = -quadStartPoint.X + runeData.AtlasUVSize.X;
			++runeIndex;
			nibHorizontalLocation -= runeData.AdvanceWidth;
			previousRune = rune;
		}

		var result = new RenderedTextData(_globals.StringPool.RentAndCopy(text), vertexBuffer, triangleBuffer, new XYPair<float>(horizontalSize, fontData.Ascent - fontData.Descent));
		if (fontData.RenderedTextCache.AddOrSet(textHash, result, out var previouslyCachedData)) {
			TextCacheEvictionHandler(this, textHash, previouslyCachedData);
		}
		return result;
	}
	
	public Mesh GetStringMesh(ResourceHandle<Font> handle, nuint stringHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		ObjectDisposedException.ThrowIf(!fontData.ActiveStrings.TryGetValue(stringHandle, out var stringData), typeof(FontString));
		return stringData.Mesh;
	}

	public XYPair<float> GetStringSize(ResourceHandle<Font> handle, nuint stringHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		ObjectDisposedException.ThrowIf(!fontData.ActiveStrings.TryGetValue(stringHandle, out var stringData), typeof(FontString));
		return stringData.Size;
	}

	public FontPen CreatePen(ResourceHandle<Font> handle, ColorVect foregroundColor, ColorVect backgroundColor, ColorVect outlineColor, float outlineThicknessNormalized) {
		const string NameEndingString = " pen material";
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];

		if (!Single.IsFinite(outlineThicknessNormalized)) {
			throw new ArgumentOutOfRangeException(nameof(outlineThicknessNormalized), outlineThicknessNormalized, "Outline thickness multiplier must be a finite value.");
		}
		outlineThicknessNormalized = Single.Clamp(outlineThicknessNormalized, 0f, 1f);
		// With zero thickness the shader's outline coverage equals its text coverage; matching the colours makes
		// the mix chain collapse to the foreground colour alone, keeping the antialiased fringe untinted
		if (outlineThicknessNormalized == 0f) outlineColor = foregroundColor;

		var nameLength = SpanUtils.GetConcatenatedLength(
			_globals.GetResourceName(handle.Ident, DefaultFontName),
			NameEndingString
		);
		using var nameBuffer = _globals.HeapPool.CreateSpanLease<char>(nameLength);
		SpanUtils.Concatenate(
			nameBuffer.Span,
			_globals.GetResourceName(handle.Ident, DefaultFontName),
			NameEndingString
		);
		var material = _materialBuilder.AllocateTextMaterialInstance(fontData.Atlas, foregroundColor, outlineColor, outlineThicknessNormalized, nameBuffer.Span);
		++_prevHandleId;
		fontData.ActivePens[_prevHandleId] = new PenData(handle, material);
		return new FontPen(HandleToInstance(handle), _prevHandleId);
	}

	public Material GetPenMaterial(ResourceHandle<Font> handle, nuint penHandle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		ObjectDisposedException.ThrowIf(!fontData.ActivePens.TryGetValue(penHandle, out var penData), typeof(FontPen));
		return penData.Material;
	}

	/* Maintainer's note:
	 * To stop text instances 'jumping around' vertically as the string data changes, the vertical offsets are essentially hard-coded according to the font:
	 *	Vertical top: Ascent
	 *	Vertical bottom: Descent
	 *	Vertical centre: Baseline
	 */
	public Transform GetTextInstanceTransform(ResourceHandle<Font> handle, float? textInstanceWidth, float? textInstanceHeight, XYPair<float> stringSize, Location position, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var scaling = (stringWidth: textInstanceWidth, stringHeight: textInstanceHeight) switch {
			(null, not null) => new XYPair<float>(textInstanceHeight.Value) / stringSize.Y,
			(not null, null) => new XYPair<float>(textInstanceWidth.Value) / stringSize.X,
			(not null, not null) => new XYPair<float>(textInstanceWidth.Value / stringSize.X, textInstanceHeight.Value / stringSize.Y),
			_ => XYPair<float>.One,
		};
		
		var rotation = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, facingDirection, uprightDirection, enforceOrthogonality: false);
		var horizontalTranslation = (Direction.Left * rotation) * positionAnchor.GetHorizontalComponent() switch {
			HorizontalOrientation2D.Right => stringSize.X * scaling.X,
			HorizontalOrientation2D.Left => 0f,
			_ => stringSize.X * scaling.X * 0.5f
		};
		var verticalTranslation = (Direction.Down * rotation) * positionAnchor.GetVerticalComponent() switch {
			VerticalOrientation2D.Up => fontData.Ascent * scaling.Y,
			VerticalOrientation2D.Down => fontData.Descent * scaling.Y,
			_ => 0f
		};
		
		return new Transform(
			translation: position.AsVect() + horizontalTranslation + verticalTranslation,
			rotation: rotation,
			scaling: new Vect(scaling.X, scaling.Y, 1f)
		);
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_glyph_advance")]
	[SuppressGCTransition]
	static extern InteropResult FontGetGlyphAdvance(
		UIntPtr fontHandle,
		int glyphIndex,
		out int outAdvanceWidth
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_glyph_pair_kern_advance")]
	[SuppressGCTransition]
	static extern InteropResult FontGetGlyphPairKernAdvance(
		UIntPtr fontHandle,
		int glyphIndex1,
		int glyphIndex2,
		out int outKernAdvance
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "font_get_kerning_data_present")]
	[SuppressGCTransition]
	static extern InteropResult FontGetKerningDataPresent(
		UIntPtr fontHandle,
		out InteropBool outResult
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
	public void DisposePen(ResourceHandle<Font> handle, nuint penHandle) => DisposePen(handle, penHandle, true);
	public void DisposeString(ResourceHandle<Font> handle, nuint stringHandle) => DisposeString(handle, stringHandle, true);
	
	void Dispose(ResourceHandle<Font> handle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var data = _activeFonts[handle];
		
		foreach (var penHandle in data.ActivePens.Keys) {
			DisposePen(handle, penHandle, false);
		}
		foreach (var stringHandle in data.ActiveStrings.Keys) {
			DisposeString(handle, stringHandle, false);
		}
		
		data.Atlas.Dispose();
		data.RenderedTextCache.Clear(invokeCacheEvictionCallbackOnAllContainedValues: true);
		_renderedTextCachePool.Return(data.RenderedTextCache);
		_penMapPool.Return(data.ActivePens);
		_stringMapPool.Return(data.ActiveStrings);
		_runeMapPool.Return(data.RuneMap);
		_kerningMapPool.Return(data.KerningMap);

		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromMap) _activeFonts.Remove(handle);
	}
	void DisposePen(ResourceHandle<Font> handle, nuint penHandle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var fontData = _activeFonts[handle];
		if (!fontData.ActivePens.TryGetValue(penHandle, out var penData)) return;
		
		penData.Material.Dispose();
		if (removeFromMap) fontData.ActivePens.Remove(penHandle);
	}
	void DisposeString(ResourceHandle<Font> handle, nuint stringHandle, bool removeFromMap) {
		if (IsDisposed(handle)) return;
		var fontData = _activeFonts[handle];
		if (!fontData.ActiveStrings.TryGetValue(stringHandle, out var stringData)) return;
		
		stringData.Mesh.Dispose();
		if (removeFromMap) fontData.ActiveStrings.Remove(stringHandle);
	}

	public void Dispose() {
		if (_isDisposed) return;
		try {
			_fontLoadRuneToGlyphMap.Dispose();

			foreach (var kvp in _activeFonts) { 
				Dispose(kvp.Key, false);
			}
			
			_activeFonts.Dispose();
			_renderedTextCachePool.Dispose(invokeDisposeOnEachItemBeforeRelease: true);
			_penMapPool.Dispose();
			_stringMapPool.Dispose();
			_runeMapPool.Dispose();
			_kerningMapPool.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Font> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Font));
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}