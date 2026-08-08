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
	readonly record struct RenderedTextData(ManagedStringPool.RentedStringHandle Text, TextJustification Justification, PooledHeapMemory<MeshVertex> Vertices, PooledHeapMemory<VertexTriangle> Triangles, XYPair<float> Size);
	readonly record struct AtlasRuneData(XYPair<float> AtlasUVOffset, XYPair<float> AtlasUVSize, XYPair<float> NibOffset, float AdvanceWidth);
	readonly record struct TextLineRecord(int StartVertexIndex, float Width, float MinX, float MaxX);
	readonly record struct FontData(Texture Atlas, float Ascent, float Descent, float LineAdvance, Rune LineBreakRune, ArrayPoolBackedMap<Rune, AtlasRuneData> RuneMap, ArrayPoolBackedMap<ulong, float> KerningMap, ArrayPoolBackedMap<nuint, PenData> ActivePens, ArrayPoolBackedMap<nuint, StringData> ActiveStrings, ArrayPoolBackedLruCache<int, RenderedTextData> RenderedTextCache);
	readonly record struct PenData(ResourceHandle<Font> OwningFont, Material Material);
	readonly record struct StringData(ResourceHandle<Font> OwningFont, Mesh Mesh, XYPair<float> Size);

	const string DefaultFontName = "Unnamed Font";
	const int SdfRenderPadding = 5; // Changes to this require changing the text shader | Defines the max outline width (but because we do single-pass rendering for now too high values overlap glyphs)
	const int AdditionalAtlasGlyphCellPadding = 2; // Any lower risks interference from bilinear filtering
	const int ReservedBackgroundBlockDimension = 3;
	const byte SdfOnEdgeValue = 180; // Changes to this require changing the text shader | Defines the crossover point (on the 0 to 255 scale) where anything lower becomes 'outline' -- higher is better to give more resolution
	const float SdfPixelDistScale = SdfOnEdgeValue / (float) SdfRenderPadding;
	const float DefaultFontHeight = 0.1f;
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
	
	static ArrayPoolBackedLruCache<int, RenderedTextData> CreateNewTextCache(LocalFontLoader @this) => new(Int32.Max(@this._config.MaxCachedTextMeshesPerFont, 1), &TextCacheEvictionHandler, @this);
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
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		switch (font) {
			case BuiltInFont.Serif: {
				var fontDataRef = EmbeddedResourceResolver.GetResource("Assets.Text.builtin_font_dejavu_serif.zip");	
				return config.Name.IsEmpty
					? LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, config with { Name = "Built-In Font 'Serif'" })
					: LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, in config);
			}
			case BuiltInFont.Monospace: {
				var fontDataRef = EmbeddedResourceResolver.GetResource("Assets.Text.builtin_font_dejavu_mono.zip");	
				return config.Name.IsEmpty
					? LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, config with { Name = "Built-In Font 'Monospace'" })
					: LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, in config);
			}
			case BuiltInFont.SansSerif:
			default: {
				var fontDataRef = EmbeddedResourceResolver.GetResource("Assets.Text.builtin_font_dejavu_sans.zip");	
				return config.Name.IsEmpty
					? LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, config with { Name = "Built-In Font 'Sans-Serif'" })
					: LoadFont((byte*) fontDataRef.DataPtr, fontDataRef.DataLenBytes, in config);
			}
		}
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
	(int RenderHeight, float ScalingConstant, int AtlasDimension, float Ascent, float Descent, float LineGap) DetermineAppropriateFontRenderMetrics(UIntPtr fontHandle, in FontCreationConfig config) {
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
					out var lineGap
				).ThrowIfFailure();
				
				var cellHeight = GetAtlasCellHeight(heightTargets[j]);
				
				var numRowsRemaining = _atlasSizeTargets[i] / cellHeight;
				var texelsRemainingInCurrentRow = _atlasSizeTargets[i] - (ReservedBackgroundBlockDimension + AdditionalAtlasGlyphCellPadding);
				
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
				
				return (heightTargets[j], scalingConstant, _atlasSizeTargets[i], ascent, descent, lineGap);
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
			
			var (renderHeight, scalingConstant, atlasDimension, ascent, descent, lineGap) = DetermineAppropriateFontRenderMetrics(fontHandle, in config);
			var atlasDimensionReciprocal = 1f / atlasDimension;
			var cellHeight = GetAtlasCellHeight(renderHeight);
			var runeMap = _runeMapPool.Rent();
			var kerningMap = _kerningMapPool.Rent();
			try {
				using var texelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(atlasDimension * atlasDimension);
				texelBuffer.Span.Clear();
				for (var row = 0; row < ReservedBackgroundBlockDimension; ++row) {
					for (var column = 0; column < ReservedBackgroundBlockDimension; ++column) {
						texelBuffer.Span[row * atlasDimension + column] = new TexelRgb24(0, 0, 255);
					}
				}
				var nib = new XYPair<int>(ReservedBackgroundBlockDimension + AdditionalAtlasGlyphCellPadding, 0);
				
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
								texelBuffer.Span[spanStartIndex + column] = new TexelRgb24(sdfValue, sdfValue, 0);
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
				_activeFonts.Add(
					handle, 
					new FontData(
						atlas,
						ascent * scalingConstant * atlasDimensionReciprocal,
						descent * scalingConstant * atlasDimensionReciprocal,
						(ascent - descent + lineGap) * scalingConstant * atlasDimensionReciprocal * config.LineSpacingMultiplier,
						config.LineBreakRune,
						runeMap,
						kerningMap, 
						_penMapPool.Rent(), 
						_stringMapPool.Rent(), 
						_renderedTextCachePool.Rent()
					)
				);
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

	static int GetTextCacheKey(ReadOnlySpan<char> text, TextJustification justification) {
		return HashCode.Combine(String.GetHashCode(text, StringComparison.Ordinal), justification);
	}

	public XYPair<float> MeasureString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var cacheKey = GetTextCacheKey(text, multiLineJustification);
		if (fontData.RenderedTextCache.TryGet(cacheKey, out var cachedTextData) && cachedTextData.Justification == multiLineJustification && cachedTextData.Text.AsSpan.SequenceEqual(text)) {
			return cachedTextData.Size;
		}

		return RenderAndCacheText(handle, text, multiLineJustification, cacheKey).Size;
	}

	public FontString CreateString(ResourceHandle<Font> handle, ReadOnlySpan<char> text, TextJustification multiLineJustification) {
		const string NameJoiningString = " string \"";
		const string NameEndingString = "\"";

		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var cacheKey = GetTextCacheKey(text, multiLineJustification);
		if (!fontData.RenderedTextCache.TryGet(cacheKey, out var textData) || textData.Justification != multiLineJustification || !textData.Text.AsSpan.SequenceEqual(text)) {
			textData = RenderAndCacheText(handle, text, multiLineJustification, cacheKey);
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
				Name = nameBuffer.Span,
				OriginTranslation = -GetTextMeshOriginOffset(fontData, textData.Size)
			}
		);
		++_prevHandleId;
		fontData.ActiveStrings[_prevHandleId] = new StringData(handle, mesh, textData.Size);
		return new FontString(HandleToInstance(handle), _prevHandleId);
	}

	RenderedTextData RenderAndCacheText(ResourceHandle<Font> fontHandle, ReadOnlySpan<char> text, TextJustification justification, int cacheKey) {
		const int UnicodeReplacementChar = 0xFFFD;
		var fontData = _activeFonts[fontHandle];
		var runeCount = 0;
		var lineCount = 1;
		foreach (var rune in text.EnumerateRunes()) {
			++runeCount;
			if (rune == fontData.LineBreakRune) ++lineCount;
		}

		var vertexBuffer = _globals.HeapPool.Borrow<MeshVertex>(runeCount * 4 + 4);
		var triangleBuffer = _globals.HeapPool.Borrow<VertexTriangle>(runeCount * 2 + 2);
		using var lineRecordBuffer = _globals.HeapPool.Borrow<TextLineRecord>(lineCount);
		var runeMap = fontData.RuneMap;
		var kerningMap = fontData.KerningMap;
		if (!runeMap.TryGetValue(new Rune(UnicodeReplacementChar), out var replacementRuneData)) {
			if (!runeMap.TryGetValue(new Rune('?'), out replacementRuneData)) {
				replacementRuneData = new AtlasRuneData(XYPair<float>.Zero, XYPair<float>.Zero, XYPair<float>.Zero, 0f);
			}
		}

		var runeIndex = 0;
		var lineIndex = 0;
		var lineStartVertexIndex = 4;
		var lineWidth = 0f;
		var lineMinX = Single.PositiveInfinity;
		var lineMaxX = Single.NegativeInfinity;
		var nibHorizontalLocation = 0f;
		var nibVerticalLocation = 0f;
		var tangentRotation = MeshVertex.CalculateTangentRotation(Direction.Right, Direction.Up, Direction.Backward);
		var backgroundBounds = (MinX: Single.PositiveInfinity, MaxX: Single.NegativeInfinity, MinY: Single.PositiveInfinity, MaxY: Single.NegativeInfinity);
		Rune? previousRune = null;
		foreach (var rune in text.EnumerateRunes()) {
			var vertexStartIndex = runeIndex * 4 + 4;

			if (rune == fontData.LineBreakRune) {
				// A degenerate (zero-size) quad keeps the vertex/triangle indexing exact without rendering anything
				var breakVertex = new MeshVertex(
					location: new(nibHorizontalLocation, nibVerticalLocation, 0f),
					textureCoords: XYPair<float>.Zero,
					tangentRotation: tangentRotation
				);
				vertexBuffer.Span[vertexStartIndex + 0] = breakVertex;
				vertexBuffer.Span[vertexStartIndex + 1] = breakVertex;
				vertexBuffer.Span[vertexStartIndex + 2] = breakVertex;
				vertexBuffer.Span[vertexStartIndex + 3] = breakVertex;
				triangleBuffer.Span[runeIndex * 2 + 2] = new VertexTriangle(vertexStartIndex + 0, vertexStartIndex + 1, vertexStartIndex + 2);
				triangleBuffer.Span[runeIndex * 2 + 3] = new VertexTriangle(vertexStartIndex + 0, vertexStartIndex + 2, vertexStartIndex + 3);

				lineRecordBuffer.Span[lineIndex] = new TextLineRecord(lineStartVertexIndex, lineWidth, lineMinX, lineMaxX);
				++lineIndex;
				lineStartVertexIndex = vertexStartIndex + 4;
				lineWidth = 0f;
				lineMinX = Single.PositiveInfinity;
				lineMaxX = Single.NegativeInfinity;
				nibHorizontalLocation = 0f;
				nibVerticalLocation -= fontData.LineAdvance;
				previousRune = null;
				++runeIndex;
				continue;
			}

			if (!runeMap.TryGetValue(rune, out var runeData)) runeData = replacementRuneData;

			if (previousRune is { } prev && kerningMap.TryGetValue(PackRunePair(prev, rune), out var kerningAdvance)) {
				nibHorizontalLocation -= kerningAdvance;
			}

			var quadStartPoint = new XYPair<float>(
				nibHorizontalLocation - runeData.NibOffset.X,
				nibVerticalLocation - (runeData.NibOffset.Y + runeData.AtlasUVSize.Y)
			);

			if (runeData.AtlasUVSize != XYPair<float>.Zero) {
				lineMinX = Single.Min(lineMinX, quadStartPoint.X - runeData.AtlasUVSize.X);
				lineMaxX = Single.Max(lineMaxX, quadStartPoint.X);
				backgroundBounds.MinY = Single.Min(backgroundBounds.MinY, quadStartPoint.Y);
				backgroundBounds.MaxY = Single.Max(backgroundBounds.MaxY, quadStartPoint.Y + runeData.AtlasUVSize.Y);
			}

			vertexBuffer.Span[vertexStartIndex + 0] = new MeshVertex(
				location: new(quadStartPoint.X, quadStartPoint.Y, 0f),
				textureCoords: runeData.AtlasUVOffset,
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[vertexStartIndex + 1] = new MeshVertex(
				location: new(quadStartPoint.X - runeData.AtlasUVSize.X, quadStartPoint.Y, 0f),
				textureCoords: runeData.AtlasUVOffset with { X = runeData.AtlasUVOffset.X + runeData.AtlasUVSize.X },
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[vertexStartIndex + 2] = new MeshVertex(
				location: new(quadStartPoint.X - runeData.AtlasUVSize.X, quadStartPoint.Y + runeData.AtlasUVSize.Y, 0f),
				textureCoords: runeData.AtlasUVOffset + runeData.AtlasUVSize,
				tangentRotation: tangentRotation
			);
			vertexBuffer.Span[vertexStartIndex + 3] = new MeshVertex(
				location: new(quadStartPoint.X, quadStartPoint.Y + runeData.AtlasUVSize.Y, 0f),
				textureCoords: runeData.AtlasUVOffset with { Y = runeData.AtlasUVOffset.Y + runeData.AtlasUVSize.Y },
				tangentRotation: tangentRotation
			);

			triangleBuffer.Span[runeIndex * 2 + 2] = new VertexTriangle(vertexStartIndex + 0, vertexStartIndex + 1, vertexStartIndex + 2);
			triangleBuffer.Span[runeIndex * 2 + 3] = new VertexTriangle(vertexStartIndex + 0, vertexStartIndex + 2, vertexStartIndex + 3);

			lineWidth = -quadStartPoint.X + runeData.AtlasUVSize.X;
			++runeIndex;
			nibHorizontalLocation -= runeData.AdvanceWidth;
			previousRune = rune;
		}
		lineRecordBuffer.Span[lineIndex] = new TextLineRecord(lineStartVertexIndex, lineWidth, lineMinX, lineMaxX);

		var maxLineWidth = 0f;
		for (var l = 0; l < lineCount; ++l) {
			maxLineWidth = Single.Max(maxLineWidth, lineRecordBuffer.Span[l].Width);
		}

		// Justification pass: lines are laid out flush at X = 0, then shifted towards negative X
		// (the visual-right direction in mesh space) according to the block's widest line
		for (var l = 0; l < lineCount; ++l) {
			var lineRecord = lineRecordBuffer.Span[l];
			var horizontalShift = justification switch {
				TextJustification.Left => 0f,
				TextJustification.Right => -(maxLineWidth - lineRecord.Width),
				_ => (maxLineWidth - lineRecord.Width) * -0.5f
			};
			if (horizontalShift != 0f) {
				var lineEndVertexIndex = l + 1 < lineCount ? lineRecordBuffer.Span[l + 1].StartVertexIndex : runeCount * 4 + 4;
				for (var v = lineRecord.StartVertexIndex; v < lineEndVertexIndex; ++v) {
					var vertex = vertexBuffer.Span[v];
					vertexBuffer.Span[v] = vertex with { Location = vertex.Location with { X = vertex.Location.X + horizontalShift } };
				}
			}
			if (lineRecord.MinX <= lineRecord.MaxX) {
				backgroundBounds.MinX = Single.Min(backgroundBounds.MinX, lineRecord.MinX + horizontalShift);
				backgroundBounds.MaxX = Single.Max(backgroundBounds.MaxX, lineRecord.MaxX + horizontalShift);
			}
		}

		// The background quad occupies the first vertex/triangle slots so every glyph quad rasterizes after
		// (and therefore composites over) it; its constant UV points at the reserved B=255 atlas block
		if (backgroundBounds.MinX > backgroundBounds.MaxX) backgroundBounds = (0f, 0f, 0f, 0f);
		vertexBuffer.Span[0] = new MeshVertex(
			location: new(backgroundBounds.MaxX, backgroundBounds.MinY, 0f),
			textureCoords: XYPair<float>.Zero,
			tangentRotation: tangentRotation
		);
		vertexBuffer.Span[1] = new MeshVertex(
			location: new(backgroundBounds.MinX, backgroundBounds.MinY, 0f),
			textureCoords: XYPair<float>.Zero,
			tangentRotation: tangentRotation
		);
		vertexBuffer.Span[2] = new MeshVertex(
			location: new(backgroundBounds.MinX, backgroundBounds.MaxY, 0f),
			textureCoords: XYPair<float>.Zero,
			tangentRotation: tangentRotation
		);
		vertexBuffer.Span[3] = new MeshVertex(
			location: new(backgroundBounds.MaxX, backgroundBounds.MaxY, 0f),
			textureCoords: XYPair<float>.Zero,
			tangentRotation: tangentRotation
		);
		triangleBuffer.Span[0] = new VertexTriangle(0, 1, 2);
		triangleBuffer.Span[1] = new VertexTriangle(0, 2, 3);

		var result = new RenderedTextData(
			_globals.StringPool.RentAndCopy(text),
			justification,
			vertexBuffer,
			triangleBuffer,
			new XYPair<float>(maxLineWidth, (fontData.Ascent - fontData.Descent) + (lineCount - 1) * fontData.LineAdvance)
		);
		if (fontData.RenderedTextCache.AddOrSet(cacheKey, result, out var previouslyCachedData)) {
			TextCacheEvictionHandler(this, cacheKey, previouslyCachedData);
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
		var material = _materialBuilder.AllocateTextMaterialInstance(fontData.Atlas, foregroundColor, backgroundColor, outlineColor, outlineThicknessNormalized, nameBuffer.Span);
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
	 * Text meshes are built with their origin at the block's left edge and first baseline, then shifted on to their centre origin
	 * (horizontal centre, mean baseline) via OriginTranslation when the mesh is created in CreateString. GetTextMeshOriginOffset below
	 * is the single source of truth for that shift: the mesh build subtracts it and the anchor query adds it back, so the two must never
	 * be allowed to drift apart or camera-locked text accumulates positional error every frame.
	 *
	 * To stop text instances 'jumping around' vertically as the string data changes, the vertical offsets are essentially hard-coded according to the font:
	 *	Vertical top: First line's ascent
	 *	Vertical bottom: Last line's descent (== Descent for single-line strings, derived via Ascent - blockHeight otherwise)
	 *	Vertical centre: Mean baseline of all lines (== first baseline for single-line strings)
	 */
	// stringSize.Y is (Ascent - Descent) + (lineCount - 1) * LineAdvance, so the Y term is exactly half the total line advance without needing to recover the line count
	static Vect GetTextMeshOriginOffset(in FontData fontData, XYPair<float> stringSize) => new(
		stringSize.X * 0.5f,
		(stringSize.Y - (fontData.Ascent - fontData.Descent)) * 0.5f,
		0f
	);

	public XYPair<float> GetTextInstanceScaling(ResourceHandle<Font> handle, XYPair<float> stringSize, float? textInstanceWidth, float? textInstanceHeight, bool disableAutomaticLineCountBasedHeightScaling) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		// stringSize.Y == (Ascent - Descent) + (lineCount - 1) * LineAdvance, so this inverts it (rounding kills FP noise)
		var heightMultiplier = !disableAutomaticLineCountBasedHeightScaling && fontData.LineAdvance > 0f
			? Single.Round((stringSize.Y - (fontData.Ascent - fontData.Descent)) / fontData.LineAdvance) + 1f
			: 1f;
		return (stringWidth: textInstanceWidth, stringHeight: textInstanceHeight) switch {
			(null, not null) => new XYPair<float>(textInstanceHeight.Value * heightMultiplier) / stringSize.Y,
			(not null, null) => new XYPair<float>(textInstanceWidth.Value) / stringSize.X,
			(not null, not null) => new XYPair<float>(textInstanceWidth.Value / stringSize.X, textInstanceHeight.Value * heightMultiplier / stringSize.Y),
			_ => new XYPair<float>(DefaultFontHeight * heightMultiplier) / stringSize.Y,
		};
	}

	public Transform GetTextInstanceTransform(ResourceHandle<Font> handle, float? textInstanceWidth, float? textInstanceHeight, XYPair<float> stringSize, Location position, Direction facingDirection, Direction uprightDirection, Orientation2D positionAnchor, bool disableAutomaticLineCountBasedHeightScaling) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var scaling = GetTextInstanceScaling(handle, stringSize, textInstanceWidth, textInstanceHeight, disableAutomaticLineCountBasedHeightScaling);
		var rotation = Rotation.FromStartAndEndOrientation(Direction.Backward, Direction.Up, facingDirection, uprightDirection, enforceOrthogonality: false);

		return new Transform(
			translation: position.AsVect() + GetTextInstanceAnchorOffset(handle, stringSize, scaling, positionAnchor) * rotation,
			rotation: rotation,
			scaling: new Vect(scaling.X, scaling.Y, 1f)
		);
	}

	public Vect GetTextInstanceAnchorOffset(ResourceHandle<Font> handle, XYPair<float> stringSize, XYPair<float> scaling, Orientation2D positionAnchor) {
		ThrowIfThisOrHandleIsDisposed(handle);
		var fontData = _activeFonts[handle];
		var originOffset = GetTextMeshOriginOffset(fontData, stringSize);
		var horizontalOffset = Direction.Left * ((positionAnchor.GetHorizontalComponent() switch {
			HorizontalOrientation2D.Right => stringSize.X * scaling.X,
			HorizontalOrientation2D.Left => 0f,
			_ => stringSize.X * scaling.X * 0.5f
		}) - originOffset.X * scaling.X);
		var verticalOffset = Direction.Down * ((positionAnchor.GetVerticalComponent() switch {
			VerticalOrientation2D.Up => fontData.Ascent * scaling.Y,
			VerticalOrientation2D.Down => (fontData.Ascent - stringSize.Y) * scaling.Y,
			_ => (fontData.Ascent - fontData.Descent - stringSize.Y) * 0.5f * scaling.Y
		}) + originOffset.Y * scaling.Y);
		return horizontalOffset + verticalOffset;
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

		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		foreach (var penHandle in data.ActivePens.Keys) {
			_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(data.ActivePens[penHandle].Material);
		}
		foreach (var stringHandle in data.ActiveStrings.Keys) {
			_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(data.ActiveStrings[stringHandle].Mesh);
		}

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