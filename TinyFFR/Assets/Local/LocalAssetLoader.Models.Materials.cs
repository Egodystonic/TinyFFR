// Created on 2026-08-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
	const string MatNameSuffix = " material ";
	const string TexNameSuffix = " texture map_";
	const int TexNameTypeSpaceMax = 20;
	const int MaxExternalAssetFilePathLength = 2048;
	
	enum AssetMaterialParamDataFormat : int { NotIncluded = 0, Numerical = 1, TextureMap = 2 }
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 24)] 
	readonly record struct AssetMaterialParam(AssetMaterialParamDataFormat Format, int TextureMapIndex, float NumericalValueR, float NumericalValueG, float NumericalValueB, float NumericalValueA) {
		public TexelRgba32 ToTexel() => TexelRgba32.FromNormalizedFloats(NumericalValueR, NumericalValueG, NumericalValueB, NumericalValueA);
		public ColorVect ToColorVect() => new(NumericalValueR, NumericalValueG, NumericalValueB, NumericalValueA);
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8 * 15)] 
	readonly struct AssetMaterialParamGroup {
		public readonly AssetMaterialParam* ColorParamsPtr;
		public readonly AssetMaterialParam* NormalParamsPtr;
		public readonly AssetMaterialParam* AmbientOcclusionParamsPtr;
		public readonly AssetMaterialParam* RoughnessParamsPtr;
		public readonly AssetMaterialParam* GlossinessParamsPtr;
		public readonly AssetMaterialParam* MetallicParamsPtr;
		public readonly AssetMaterialParam* IoRParamsPtr;
		public readonly AssetMaterialParam* AbsorptionParamsPtr;
		public readonly AssetMaterialParam* TransmissionParamsPtr;
		public readonly AssetMaterialParam* EmissiveColorParamsPtr;
		public readonly AssetMaterialParam* EmissiveIntensityParamsPtr;
		public readonly AssetMaterialParam* AnisotropyAngleParamsPtr;
		public readonly AssetMaterialParam* AnisotropyStrengthParamsPtr;
		public readonly AssetMaterialParam* ClearCoatStrengthParamsPtr;
		public readonly AssetMaterialParam* ClearCoatRoughnessParamsPtr;

		public AssetMaterialParamGroup(AssetMaterialParam* colorParamsPtr, AssetMaterialParam* normalParamsPtr, AssetMaterialParam* ambientOcclusionParamsPtr, AssetMaterialParam* roughnessParamsPtr, AssetMaterialParam* glossinessParamsPtr, AssetMaterialParam* metallicParamsPtr, AssetMaterialParam* ioRParamsPtr, AssetMaterialParam* absorptionParamsPtr, AssetMaterialParam* transmissionParamsPtr, AssetMaterialParam* emissiveColorParamsPtr, AssetMaterialParam* emissiveIntensityParamsPtr, AssetMaterialParam* anisotropyAngleParamsPtr, AssetMaterialParam* anisotropyStrengthParamsPtr, AssetMaterialParam* clearCoatStrengthParamsPtr, AssetMaterialParam* clearCoatRoughnessParamsPtr) {
			ColorParamsPtr = colorParamsPtr;
			NormalParamsPtr = normalParamsPtr;
			AmbientOcclusionParamsPtr = ambientOcclusionParamsPtr;
			RoughnessParamsPtr = roughnessParamsPtr;
			GlossinessParamsPtr = glossinessParamsPtr;
			MetallicParamsPtr = metallicParamsPtr;
			IoRParamsPtr = ioRParamsPtr;
			AbsorptionParamsPtr = absorptionParamsPtr;
			TransmissionParamsPtr = transmissionParamsPtr;
			EmissiveColorParamsPtr = emissiveColorParamsPtr;
			EmissiveIntensityParamsPtr = emissiveIntensityParamsPtr;
			AnisotropyAngleParamsPtr = anisotropyAngleParamsPtr;
			AnisotropyStrengthParamsPtr = anisotropyStrengthParamsPtr;
			ClearCoatStrengthParamsPtr = clearCoatStrengthParamsPtr;
			ClearCoatRoughnessParamsPtr = clearCoatRoughnessParamsPtr;
		}
	};
	readonly struct EmbeddedTextureData : IDisposable {
		readonly PooledHeapMemory<TexelRgba32> _texelBuffer;
		public readonly XYPair<int> Dimensions;
		
		public Span<TexelRgba32> TexelSpan => _texelBuffer.Span[..Dimensions.Area];

		public EmbeddedTextureData(PooledHeapMemory<TexelRgba32> texelBuffer, XYPair<int> dimensions) {
			_texelBuffer = texelBuffer;
			Dimensions = dimensions;
		}

		public void Dispose() => _texelBuffer.Dispose();
	}
	readonly ref struct AssetMaterialCreationParameters {
		readonly Span<char> _subResourceNameBuffer;
		readonly int _subResourceNameTexTypeStartIndex;
		public readonly UIntPtr AssetHandle;
		public readonly int MaterialIndex;
		public readonly TextureCreationConfig Config;
		public readonly ref readonly byte AssetRootDirStrRef;
		public readonly bool UriUnescapeEmbeddedResourceStrings;
		public readonly float GltfEmissiveStrengthScalar;
		public readonly float EmissiveStrengthCap;
		public readonly TextureCombinationScalingStrategy TextureCombinationStrategy;
		public readonly ModelLoadMaterialTextureRegistry TextureRegistry;
		public readonly ThreadSafeHeapPoolWrapper HeapPool;

		public AssetMaterialCreationParameters(UIntPtr assetHandle, int materialIndex, Span<char> subResourceNameBuffer, int matNameLength, TextureCreationConfig config, ref readonly byte assetRootDirStrRef, bool uriUnescapeEmbeddedResourceStrings, float gltfEmissiveStrengthScalar, float emissiveStrengthCap, TextureCombinationScalingStrategy textureCombinationStrategy, ModelLoadMaterialTextureRegistry textureRegistry, ThreadSafeHeapPoolWrapper heapPool) {
			AssetHandle = assetHandle;
			MaterialIndex = materialIndex;
			TextureRegistry = textureRegistry;
			HeapPool = heapPool;
			_subResourceNameBuffer = subResourceNameBuffer;
			TexNameSuffix.CopyTo(_subResourceNameBuffer[matNameLength..]);
			_subResourceNameTexTypeStartIndex = matNameLength + TexNameSuffix.Length;
			Config = config;
			AssetRootDirStrRef = ref assetRootDirStrRef;
			UriUnescapeEmbeddedResourceStrings = uriUnescapeEmbeddedResourceStrings;
			GltfEmissiveStrengthScalar = gltfEmissiveStrengthScalar;
			EmissiveStrengthCap = emissiveStrengthCap;
			TextureCombinationStrategy = textureCombinationStrategy;
		}
		
		public ReadOnlySpan<char> CreateTextureName(ReadOnlySpan<char> textureTypeName) {
			textureTypeName.CopyTo(_subResourceNameBuffer[_subResourceNameTexTypeStartIndex..]);
			return _subResourceNameBuffer[..(_subResourceNameTexTypeStartIndex + textureTypeName.Length)];
		}
	}
	
	readonly record struct GatheredMaterialData(
		int ColorMapSlot,
		int AbsorptionTransmissionMapSlot,
		int NormalMapSlot,
		int OrmMapSlot,
		int AnisotropyMapSlot,
		int EmissiveMapSlot,
		int ClearCoatMapSlot,
		int AlphaFormat,
		float RefractionThickness,
		NameSlice Name
	);

	sealed class ModelLoadMaterialTextureRegistry : IDisposable {
		readonly ArrayPoolBackedVector<TextureCreationMetadata> _entries = new();
		readonly ArrayPoolBackedVector<NameSlice> _names = new();
		readonly ArrayPoolBackedVector<char> _nameChars = new();
		int _count = 0;

		public NameSlice AppendName(ReadOnlySpan<char> name) {
			var startIndex = _nameChars.Count;
			for (var i = 0; i < name.Length; ++i) _nameChars.Add(name[i]);
			return new NameSlice(startIndex, name.Length);
		}

		public ReadOnlySpan<char> GetName(NameSlice slice) => _nameChars.AsSpan.Slice(slice.StartIndex, slice.Length);

		public int Add<TTexel>(TTexel singleTexel, in TextureCreationConfig config, ThreadSafeHeapPoolWrapper heapPool) where TTexel : unmanaged, ITexel<TTexel> {
			return Add(new ReadOnlySpan<TTexel>(in singleTexel), XYPair<int>.One, in config, heapPool);
		}

		public int Add<TTexel>(ReadOnlySpan<TTexel> texels, XYPair<int> dimensions, in TextureCreationConfig config, ThreadSafeHeapPoolWrapper heapPool) where TTexel : unmanaged, ITexel<TTexel> {
			if (_count == _entries.Count) _entries.Add(new TextureCreationMetadata());
			var metadata = _entries[_count];

			var buffer = heapPool.Borrow<byte>(dimensions.Area * sizeof(TTexel));
			metadata.OwnedTexelData = buffer;
			var destination = MemoryMarshal.Cast<byte, TTexel>(buffer.Span);
			texels[..dimensions.Area].CopyTo(destination);

			var generationConfig = new TextureGenerationConfig { Dimensions = dimensions };
			LocalTextureBuilder.CheckConfigValidityAndProcessTexture(destination, in generationConfig, in config);

			metadata.Dimensions = dimensions;
			metadata.IsRgba = TTexel.BlitType == TexelType.Rgba32;
			metadata.GenerateMipMaps = config.GenerateMipMaps;
			metadata.DataType = config.DataType;
			metadata.AllowsDynamicWrites = config.AllowsDynamicWrites;
			metadata.RenderingConfig = config.RenderingConfig;
			_names.Add(AppendName(config.Name));
			return _count++;
		}

		public Texture Materialize(LocalAssetLoader self, int slot) => CompleteTextureLoad(self, _entries[slot], GetName(_names[slot]));

		public void DisposeBuffersAndReset() {
			for (var i = 0; i < _count; ++i) _entries[i].TearDown();
			_count = 0;
			_names.ClearWithoutZeroingMemory();
			_nameChars.ClearWithoutZeroingMemory();
		}

		public void Dispose() {
			DisposeBuffersAndReset();
			_entries.Dispose();
			_names.Dispose();
			_nameChars.Dispose();
		}
	}

	EmbeddedTextureData LoadAssetTexture(UIntPtr assetHandle, int materialIndex, int textureIndex, ref readonly byte assetRootDirStrRef, bool uriUnescapeEmbeddedResourceStrings, ThreadSafeHeapPoolWrapper heapPool) {
		if (textureIndex < 0) {
			GetLoadedAssetTextureExternalPathLength(
				assetHandle,
				materialIndex,
				textureIndex,
				in assetRootDirStrRef,
				out var strLenLessNullTerminator
			).ThrowIfFailure();
			
			if (strLenLessNullTerminator <= 0 || strLenLessNullTerminator >= MaxExternalAssetFilePathLength) {
				throw new InvalidOperationException($"Can not load embedded texture at index '{textureIndex}' as its external path length is '{strLenLessNullTerminator}' bytes.");
			}
			
			var strBuffer = stackalloc byte[strLenLessNullTerminator + 1];
			
			GetLoadedAssetTextureExternalPath(
				assetHandle,
				materialIndex,
				textureIndex,
				in assetRootDirStrRef,
				strBuffer,
				strLenLessNullTerminator + 1
			).ThrowIfFailure();
			
			if (uriUnescapeEmbeddedResourceStrings) {
				var strBufferSpan = new Span<byte>(strBuffer, strLenLessNullTerminator + 1);
				var escapedStr = Encoding.UTF8.GetString(strBufferSpan[..^1]);
				var unescapedStr = Uri.UnescapeDataString(escapedStr);
				var byteCount = Encoding.UTF8.GetByteCount(unescapedStr);
				if (byteCount > strLenLessNullTerminator) throw new InvalidOperationException($"Escaped resource string '{escapedStr}' was longer than unescaped string '{unescapedStr}'!");
				strBufferSpan.Clear(); // This makes sure the unwritten portion of the buffer will be null terminator(s)
				Encoding.UTF8.GetBytes(unescapedStr, strBufferSpan[..^1]);
			}

			var loadResult = LoadTextureFileInToMemory(
				in Unsafe.AsRef<byte>(strBuffer),
				true,
				out var width,
				out var height,
				out var texBuf
			);
			if (!loadResult) {
				try {
					loadResult.ThrowIfFailure();
				}
				catch (Exception e) {
					if (uriUnescapeEmbeddedResourceStrings) throw;
					else {
						throw new InvalidOperationException($"Failure to load embedded model texture; " +
							$"if this resource name is an escaped URI string consider setting {nameof(ModelReadConfig.HandleUriEscapedStrings)} to true.", e);
					}
				}
			}

			try {
				if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");
				var texelCount = width * height;

				ThrowIfAssetBufferSizeExceedsMaximum((long) texelCount * sizeof(TexelRgba32), $"embedded asset texture ({width}x{height})");
				var resultBuffer = heapPool.Borrow<TexelRgba32>(checked(width * height));
				new ReadOnlySpan<TexelRgba32>(texBuf, texelCount).CopyTo(resultBuffer.Span);
				return new(resultBuffer, new XYPair<int>(width, height));
			}
			finally {
				UnloadTextureFileFromMemory(texBuf).ThrowIfFailure();
			}
		}
		
		GetLoadedAssetTextureSize(
			assetHandle, 
			textureIndex,
			in assetRootDirStrRef,
			out var outWidth,
			out var outHeight
		).ThrowIfFailure();
		
		if (outWidth < 0 || outHeight < 0) throw new InvalidOperationException($"Width or height for asset texture at index '{textureIndex}' was malformed.");
		
		ThrowIfAssetBufferSizeExceedsMaximum((long) outWidth * outHeight * sizeof(TexelRgba32), $"embedded asset texture ({outWidth}x{outHeight})");
		var texelBuffer = heapPool.Borrow<TexelRgba32>(checked(outWidth * outHeight));
		
		fixed (TexelRgba32* texelBufferPtr = texelBuffer.Span) {
			GetLoadedAssetTextureData(
				assetHandle,
				textureIndex,
				in assetRootDirStrRef,
				(void*) texelBufferPtr,
				checked(outWidth * outHeight * sizeof(TexelRgba32)),
				out outWidth,
				out outHeight
			).ThrowIfFailure();
		}
		
		return new(texelBuffer, new XYPair<int>(outWidth, outHeight));
	}
	
	Span<TexelRgba32> AbstractTexelSpanFromParamPtr(AssetMaterialParam* paramPtr, UIntPtr assetHandle, int materialIndex, bool uriUnescapeEmbeddedResourceStrings, ref readonly byte assetRootDirStrRef, ref TexelRgba32 stackTexelWithDefaultValue, ThreadSafeHeapPoolWrapper heapPool, out EmbeddedTextureData? outEmbeddedTex) {
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				stackTexelWithDefaultValue = paramPtr->ToTexel();
				outEmbeddedTex = null;
				return new Span<TexelRgba32>(ref stackTexelWithDefaultValue);
		
			case AssetMaterialParamDataFormat.TextureMap:
				outEmbeddedTex = LoadAssetTexture(
					assetHandle,
					materialIndex,
					paramPtr->TextureMapIndex,
					in assetRootDirStrRef,
					uriUnescapeEmbeddedResourceStrings,
					heapPool
				);
				// ReSharper disable CompareOfFloatsByEqualityOperator -- expected that the native side will explicitly set these to exactly -1f/0f/1f/etc
				if (paramPtr->NumericalValueR == -1f && paramPtr->NumericalValueG == -1f && paramPtr->NumericalValueB == -1f && paramPtr->NumericalValueA != 1f) {
					if (paramPtr->NumericalValueA == 0f) {
						// Shortcut out for a 0 value
						outEmbeddedTex.Value.TexelSpan.Clear();
					}
					else {
						for (var i = 0; i < outEmbeddedTex.Value.TexelSpan.Length; ++i) {
							var beforeMult = outEmbeddedTex.Value.TexelSpan[i];
							outEmbeddedTex.Value.TexelSpan[i] = new TexelRgba32(
								(byte) (beforeMult.R * paramPtr->NumericalValueA),
								(byte) (beforeMult.G * paramPtr->NumericalValueA),
								(byte) (beforeMult.B * paramPtr->NumericalValueA),
								(byte) (beforeMult.A * paramPtr->NumericalValueA)
							);
						}	
					}
				}
				// ReSharper restore CompareOfFloatsByEqualityOperator
				return outEmbeddedTex.Value.TexelSpan;
			
			default: 
				outEmbeddedTex = null;
				return new Span<TexelRgba32>(ref stackTexelWithDefaultValue);
		}
	}
	
	static bool ParamPtrsRepresentIdenticalTextures(UIntPtr assetHandle, int materialIndex, ref readonly byte assetRootDirStrRef, AssetMaterialParam* paramPtrA, AssetMaterialParam* paramPtrB) {
		if (paramPtrA->Format != AssetMaterialParamDataFormat.TextureMap || paramPtrB->Format != AssetMaterialParamDataFormat.TextureMap) return false;
		if (paramPtrA->TextureMapIndex == paramPtrB->TextureMapIndex) return true;
		if (paramPtrA->TextureMapIndex >= 0 || paramPtrB->TextureMapIndex >= 0) return false;
		
		GetLoadedAssetTextureExternalPathLength(
			assetHandle,
			materialIndex,
			paramPtrA->TextureMapIndex,
			in assetRootDirStrRef,
			out var aStrLenLessNullTerminator
		).ThrowIfFailure();
		
		GetLoadedAssetTextureExternalPathLength(
			assetHandle,
			materialIndex,
			paramPtrB->TextureMapIndex,
			in assetRootDirStrRef,
			out var bStrLenLessNullTerminator
		).ThrowIfFailure();
		
		var aInvalid = aStrLenLessNullTerminator is <= 0 or >= MaxExternalAssetFilePathLength;
		var bInvalid = bStrLenLessNullTerminator is <= 0 or >= MaxExternalAssetFilePathLength;
		
		if (aInvalid || bInvalid || aStrLenLessNullTerminator != bStrLenLessNullTerminator) return false;
		
		var aStrBuffer = stackalloc byte[aStrLenLessNullTerminator + 1];
		var bStrBuffer = stackalloc byte[bStrLenLessNullTerminator + 1];
		
		GetLoadedAssetTextureExternalPath(
			assetHandle,
			materialIndex,
			paramPtrA->TextureMapIndex,
			in assetRootDirStrRef,
			aStrBuffer,
			aStrLenLessNullTerminator + 1
		).ThrowIfFailure();
		
		GetLoadedAssetTextureExternalPath(
			assetHandle,
			materialIndex,
			paramPtrB->TextureMapIndex,
			in assetRootDirStrRef,
			bStrBuffer,
			bStrLenLessNullTerminator + 1
		).ThrowIfFailure();
		
		var aSpan = new ReadOnlySpan<byte>(aStrBuffer, aStrLenLessNullTerminator);
		var bSpan = new ReadOnlySpan<byte>(bStrBuffer, bStrLenLessNullTerminator);
		return aSpan.SequenceEqual(bSpan);
	}
	
	int GatherAssetColorMap(AssetMaterialParam* paramPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "color";
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				return creationParams.TextureRegistry.Add(
					new TexelRgba32(paramPtr->ToColorVect()),
					creationParams.Config with {
						Name = creationParams.CreateTextureName(TextureTypeName),
						DataType = TextureDataType.LinearData // Numerical outputs are in linear space
					},
					creationParams.HeapPool
				);
			
			case AssetMaterialParamDataFormat.TextureMap:
				using (var embeddedTex = LoadAssetTexture(creationParams.AssetHandle, creationParams.MaterialIndex, paramPtr->TextureMapIndex, in creationParams.AssetRootDirStrRef, creationParams.UriUnescapeEmbeddedResourceStrings, creationParams.HeapPool)) {
					return creationParams.TextureRegistry.Add(
						embeddedTex.TexelSpan,
						embeddedTex.Dimensions,
						creationParams.Config with {
							Name = creationParams.CreateTextureName(TextureTypeName),
							DataType = TextureDataType.ColorSrgb
						},
						creationParams.HeapPool
					);
				}
				
			default:
				return creationParams.TextureRegistry.Add(
					new TexelRgb24(ITextureBuilder.DefaultColor),
					ITextureBuilder.GetColorMapCreationConfig(XYPair<int>.One, includeAlpha: false, creationParams.CreateTextureName(TextureTypeName)),
					creationParams.HeapPool
				);
		}
	}
	
	int GatherAssetAbsorptionTransmissionMap(AssetMaterialParam* absorptionParamPtr, AssetMaterialParam* transmissionParamPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "at";
		if (absorptionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && transmissionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return -1;
		
		// All in one texture, just load the whole thing once and return it, no combination required
		if (ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, absorptionParamPtr, transmissionParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				creationParams.AssetHandle,
				creationParams.MaterialIndex,
				absorptionParamPtr->TextureMapIndex,
				in creationParams.AssetRootDirStrRef,
				creationParams.UriUnescapeEmbeddedResourceStrings,
				creationParams.HeapPool
			);
			return creationParams.TextureRegistry.Add(
				embeddedTex.TexelSpan,
				embeddedTex.Dimensions,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.ColorSrgb },
				creationParams.HeapPool
			);
		}
		
		var defaultAbsorptionTexel = new TexelRgba32(ITextureBuilder.DefaultAbsorption);
		var defaultTransmissionTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultTransmission, Real.Zero, Real.Zero, Real.Zero);
		
		var absorptionTexels = AbstractTexelSpanFromParamPtr(
			absorptionParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultAbsorptionTexel,
			creationParams.HeapPool,
			out var absorptionEmbeddedTex
		);
		var transmissionTexels = AbstractTexelSpanFromParamPtr(
			transmissionParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultTransmissionTexel,
			creationParams.HeapPool,
			out var transmissionEmbeddedTex
		);

		try {
			var aDim = absorptionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = transmissionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgba32>(destDim.Area);
			TextureUtils.CombineTextures(
				absorptionTexels, aDim,	
				transmissionTexels, bDim,
				new TextureCombinationConfig(
					creationParams.TextureCombinationStrategy,
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Span
			);
			return creationParams.TextureRegistry.Add(
				destinationBuffer.Span,
				destDim,
				creationParams.Config with {
					Name = creationParams.CreateTextureName(TextureTypeName),
					DataType = absorptionEmbeddedTex.HasValue ? TextureDataType.ColorSrgb : TextureDataType.LinearData // Numerical values are linear, textures assumed sRGB
				},
				creationParams.HeapPool
			);
		}
		finally {
			absorptionEmbeddedTex?.Dispose();
			transmissionEmbeddedTex?.Dispose();
		}
	}
	
	int GatherAssetNormalMap(AssetMaterialParam* paramPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "norm";
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				return creationParams.TextureRegistry.Add(
					paramPtr->ToTexel().ToRgb24(),
					creationParams.Config with {
						Name = creationParams.CreateTextureName(TextureTypeName),
						DataType = TextureDataType.LinearDataUnitVector
					},
					creationParams.HeapPool
				);
			
			case AssetMaterialParamDataFormat.TextureMap:
				using (var embeddedTex = LoadAssetTexture(creationParams.AssetHandle, creationParams.MaterialIndex, paramPtr->TextureMapIndex, in creationParams.AssetRootDirStrRef, creationParams.UriUnescapeEmbeddedResourceStrings, creationParams.HeapPool)) {
					using var rgbTexelBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
					TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Span);
					return creationParams.TextureRegistry.Add(
						rgbTexelBuffer.Span,
						embeddedTex.Dimensions,
						creationParams.Config with {
							Name = creationParams.CreateTextureName(TextureTypeName), 
							DataType = TextureDataType.LinearDataUnitVector
						},
						creationParams.HeapPool
					);
				}
				
			default: return -1;
		}
	}
	
	int GatherAssetOrmrMap(AssetMaterialParam* occlusionParamPtr, AssetMaterialParam* roughnessParamPtr, AssetMaterialParam* glossinessParamPtr, AssetMaterialParam* metallicParamPtr, AssetMaterialParam* iorParamPtr, bool reflectanceRequired, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "orm";
		
		// Maintainer's note:
		// Reflectance can not be stored in a texture map, because it's actually exposed as IoR from assimp and there's no industry-normalized range mapping [0-1] to any known IoR range
		// So either we don't specify it at all or if there's a numerical value it's considered to be IoR and must be converted
		if (occlusionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded 
			&& roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded 
			&& glossinessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded
			&& metallicParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded
			&& iorParamPtr->Format != AssetMaterialParamDataFormat.Numerical) {
			return -1;
		}
		
		var reflectanceValue = iorParamPtr->Format == AssetMaterialParamDataFormat.Numerical
			? MathF.Pow((iorParamPtr->NumericalValueR - 1f) / (iorParamPtr->NumericalValueR + 1f), 2f) // Conversion from IoR to reflectance
			: (float?) null;
		if (reflectanceRequired) reflectanceValue ??= ITextureBuilder.DefaultReflectance;
		
		var occlusionRoughnessAreSameTexture = ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, occlusionParamPtr, roughnessParamPtr);
		var roughnessMetallicAreSameTexture = ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, roughnessParamPtr, metallicParamPtr);
		// No reflectance and the rest are a singular ORM map, just load it and be done
		if (reflectanceValue == null && occlusionRoughnessAreSameTexture && roughnessMetallicAreSameTexture) {
			using var embeddedTex = LoadAssetTexture(
				creationParams.AssetHandle,
				creationParams.MaterialIndex,
				occlusionParamPtr->TextureMapIndex,
				in creationParams.AssetRootDirStrRef,
				creationParams.UriUnescapeEmbeddedResourceStrings,
				creationParams.HeapPool
			);
			return creationParams.TextureRegistry.Add(
				embeddedTex.TexelSpan,
				embeddedTex.Dimensions,
				creationParams.Config with {
					Name = creationParams.CreateTextureName(TextureTypeName),
					DataType = TextureDataType.LinearData
				},
				creationParams.HeapPool
			);
		}
		
		var defaultOcclusionTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion);
		var defaultRoughnessTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness);
		var defaultMetallicTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic);
		
		var glossinessSpecifiedOverRoughness = roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && glossinessParamPtr->Format != AssetMaterialParamDataFormat.NotIncluded;
		
		var occlusionTexels = AbstractTexelSpanFromParamPtr(
			occlusionParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultOcclusionTexel,
			creationParams.HeapPool,
			out var occlusionEmbeddedTex
		);
		var roughnessTexels = AbstractTexelSpanFromParamPtr(
			glossinessSpecifiedOverRoughness ? glossinessParamPtr : roughnessParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultRoughnessTexel,
			creationParams.HeapPool,
			out var roughnessEmbeddedTex
		);
		var metallicTexels = AbstractTexelSpanFromParamPtr(
			metallicParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultMetallicTexel,
			creationParams.HeapPool,
			out var metallicEmbeddedTex
		);
		
		try {
			var aDim = occlusionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = roughnessEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var cDim = metallicEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim, cDim);
			if (glossinessSpecifiedOverRoughness) TextureUtils.NegateTexture(roughnessTexels, bDim);
			
			/*	Maintainer's note:
			 *	I originally had logic here to select the roughness/metallic channel for combination below depending on which textures were detected as "combined".
			 *	The thinking was that if e.g. the metallic texture was a separate texture file, we should select its R channel rather than the canonical B channel,
			 *	as it could in theory have data only in Red. However, this proved more problematic than useful, and more often than not was just plain wrong.
			 *	In fact, if we have a separate metallic texture I think we can assume it's monochromatic but with all RGB channels set to the same value. Ditto roughness.
			 */

			if (reflectanceValue.HasValue) {
				using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgba32>(destDim.Area);
				var reflectanceTexel = TexelRgba32.FromNormalizedFloats(reflectanceValue.Value, reflectanceValue.Value, reflectanceValue.Value, reflectanceValue.Value);
				TextureUtils.CombineTextures(
					occlusionTexels, aDim,	
					roughnessTexels, bDim,
					metallicTexels, cDim,
					new ReadOnlySpan<TexelRgba32>(in reflectanceTexel), XYPair<int>.One,
					new TextureCombinationConfig(
						creationParams.TextureCombinationStrategy,
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.G),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, ColorChannel.B),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureD, ColorChannel.A)
					),
					destinationBuffer.Span
				);
				return creationParams.TextureRegistry.Add(
					destinationBuffer.Span,
					destDim,
					creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName + "r"), DataType = TextureDataType.LinearData },
					creationParams.HeapPool
				);
			}
			else {
				using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(destDim.Area);
				TextureUtils.CombineTextures(
					occlusionTexels, aDim,	
					roughnessTexels, bDim,
					metallicTexels, cDim,
					new TextureCombinationConfig(
						creationParams.TextureCombinationStrategy,
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.G),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, ColorChannel.B)
					),
					destinationBuffer.Span
				);
				return creationParams.TextureRegistry.Add(
					destinationBuffer.Span,
					destDim,
					creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.LinearData },
					creationParams.HeapPool
				);
			}
		}
		finally {
			occlusionEmbeddedTex?.Dispose();
			roughnessEmbeddedTex?.Dispose();
			metallicEmbeddedTex?.Dispose();
		}
	}
	
	int GatherAssetAnisotropyMap(AssetMaterialParam* angleParamPtr, AssetMaterialParam* strengthParamPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "aniso";
		if (angleParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && strengthParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return -1;
		
		// All in one texture; the only well-defined texture format is in the glTF spec: https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_anisotropy/README.md
		// So we assume this format is the one being used, which matches what TinyFFR already expects thankfully
		if (ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, angleParamPtr, strengthParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				creationParams.AssetHandle,
				creationParams.MaterialIndex,
				angleParamPtr->TextureMapIndex,
				in creationParams.AssetRootDirStrRef,
				creationParams.UriUnescapeEmbeddedResourceStrings,
				creationParams.HeapPool
			);
			using var rgbTexelBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
			TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Span);
			return creationParams.TextureRegistry.Add(
				rgbTexelBuffer.Span,
				embeddedTex.Dimensions,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.LinearData },
				creationParams.HeapPool
			);
		}
		
		var defaultAngleTexel = new TexelRgba32(0, 0, 0, 0);
		var defaultStrengthTexel = new TexelRgba32(255, 255, 255, 255);
		
		var angleTexels = AbstractTexelSpanFromParamPtr(
			angleParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultAngleTexel,
			creationParams.HeapPool,
			out var angleEmbeddedTex
		);
		var strengthTexels = AbstractTexelSpanFromParamPtr(
			strengthParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultStrengthTexel,
			creationParams.HeapPool,
			out var strengthEmbeddedTex
		);

		try {
			var aDim = angleEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = strengthEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(destDim.Area);
			TextureUtils.CombineTextures(
				angleTexels, aDim,	
				strengthTexels, bDim,
				new TextureCombinationConfig(
					creationParams.TextureCombinationStrategy,
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Span
			);
			// After combining the disparate textures we need to convert them from angle/strength to tangent-space vector + strength
			IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(destinationBuffer.Span, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, ColorChannel.B);
			return creationParams.TextureRegistry.Add(
				destinationBuffer.Span,
				destDim,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.LinearData },
				creationParams.HeapPool
			);
		}
		finally {
			angleEmbeddedTex?.Dispose();
			strengthEmbeddedTex?.Dispose();
		}
	}
	
	int GatherAssetEmissiveMap(AssetMaterialParam* colorParamPtr, AssetMaterialParam* intensityParamPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "emissive";
		if (colorParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && intensityParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return -1;
		
		static void ScaleAndCapEmissiveIntensity(Span<TexelRgba32> texels, bool targetAlphaChannel, float scalar, byte cap) {
			if (targetAlphaChannel) {
				for (var i = 0; i < texels.Length; ++i) {
					var scaled = (byte) MathF.Round(Single.Clamp(texels[i].A * scalar, 0f, cap));
					texels[i] = texels[i] with { A = scaled };
				}
			}
			else {
				for (var i = 0; i < texels.Length; ++i) {
					var scaled = (byte) MathF.Round(Single.Clamp(texels[i].R * scalar, 0f, cap));
					texels[i] = texels[i] with { R = scaled };
				}
			}
		}
		
		// All in one texture, just load the whole thing once and return it, no combination required
		if (ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, colorParamPtr, intensityParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				creationParams.AssetHandle,
				creationParams.MaterialIndex,
				colorParamPtr->TextureMapIndex,
				in creationParams.AssetRootDirStrRef,
				creationParams.UriUnescapeEmbeddedResourceStrings,
				creationParams.HeapPool
			);
			ScaleAndCapEmissiveIntensity(embeddedTex.TexelSpan, true, creationParams.GltfEmissiveStrengthScalar, (byte) (creationParams.EmissiveStrengthCap * Byte.MaxValue));
			return creationParams.TextureRegistry.Add(
				embeddedTex.TexelSpan,
				embeddedTex.Dimensions,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.ColorSrgb },
				creationParams.HeapPool
			);
		}
		
		var defaultColorTexel = new TexelRgba32(ITextureBuilder.DefaultEmissiveColor);
		var defaultIntensityTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity);
		
		var colorTexels = AbstractTexelSpanFromParamPtr(
			colorParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultColorTexel,
			creationParams.HeapPool,
			out var colorEmbeddedTex
		);
		var modifiedIntensityParam = (*intensityParamPtr) with {
			NumericalValueR = Single.Clamp(intensityParamPtr->NumericalValueR * creationParams.GltfEmissiveStrengthScalar, 0f, creationParams.EmissiveStrengthCap),
			NumericalValueG = Single.Clamp(intensityParamPtr->NumericalValueG * creationParams.GltfEmissiveStrengthScalar, 0f, creationParams.EmissiveStrengthCap),
			NumericalValueB = Single.Clamp(intensityParamPtr->NumericalValueB * creationParams.GltfEmissiveStrengthScalar, 0f, creationParams.EmissiveStrengthCap),
			NumericalValueA = Single.Clamp(intensityParamPtr->NumericalValueA * creationParams.GltfEmissiveStrengthScalar, 0f, creationParams.EmissiveStrengthCap)
		};
		var intensityTexels = AbstractTexelSpanFromParamPtr(
			&modifiedIntensityParam,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultIntensityTexel,
			creationParams.HeapPool,
			out var intensityEmbeddedTex
		);
		if (intensityParamPtr->Format == AssetMaterialParamDataFormat.TextureMap) {
			ScaleAndCapEmissiveIntensity(intensityTexels, false, creationParams.GltfEmissiveStrengthScalar, (byte) (creationParams.EmissiveStrengthCap * Byte.MaxValue));
		}

		try {
			var aDim = colorEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = intensityEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgba32>(destDim.Area);
			TextureUtils.CombineTextures(
				colorTexels, aDim,	
				intensityTexels, bDim,
				new TextureCombinationConfig(
					creationParams.TextureCombinationStrategy,
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Span
			);
			return creationParams.TextureRegistry.Add(
				destinationBuffer.Span,
				destDim,
				creationParams.Config with {
					Name = creationParams.CreateTextureName(TextureTypeName), 
					DataType = colorEmbeddedTex.HasValue ? TextureDataType.ColorSrgb : TextureDataType.LinearData // Numerical values are linear, textures assumed sRGB
				},
				creationParams.HeapPool
			);
		}
		finally {
			colorEmbeddedTex?.Dispose();
			intensityEmbeddedTex?.Dispose();
		}
	}
	
	int GatherAssetClearCoatMap(AssetMaterialParam* strengthParamPtr, AssetMaterialParam* roughnessParamPtr, in AssetMaterialCreationParameters creationParams) {
		const string TextureTypeName = "clearcoat";
		if (strengthParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return -1;
		
		// All in one texture; the only well-defined texture format is in the glTF spec: https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_clearcoat
		// So we assume this format is the one being used, which matches what TinyFFR already expects thankfully
		if (ParamPtrsRepresentIdenticalTextures(creationParams.AssetHandle, creationParams.MaterialIndex, in creationParams.AssetRootDirStrRef, strengthParamPtr, roughnessParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				creationParams.AssetHandle,
				creationParams.MaterialIndex,
				strengthParamPtr->TextureMapIndex,
				in creationParams.AssetRootDirStrRef,
				creationParams.UriUnescapeEmbeddedResourceStrings,
				creationParams.HeapPool
			);
			using var rgbTexelBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
			TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Span);
			return creationParams.TextureRegistry.Add(
				rgbTexelBuffer.Span,
				embeddedTex.Dimensions,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.LinearDataTwoChannelMax },
				creationParams.HeapPool
			);
		}
		
		var defaultStrengthTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness);
		var defaultRoughnessTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness);
		
		var strengthTexels = AbstractTexelSpanFromParamPtr(
			strengthParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultStrengthTexel,
			creationParams.HeapPool,
			out var strengthEmbeddedTex
		);
		var roughnessTexels = AbstractTexelSpanFromParamPtr(
			roughnessParamPtr,
			creationParams.AssetHandle,
			creationParams.MaterialIndex,
			creationParams.UriUnescapeEmbeddedResourceStrings,
			in creationParams.AssetRootDirStrRef,
			ref defaultRoughnessTexel,
			creationParams.HeapPool,
			out var roughnessEmbeddedTex
		);

		try {
			var aDim = strengthEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = roughnessEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = creationParams.HeapPool.Borrow<TexelRgb24>(destDim.Area);
			TextureUtils.CombineTextures(
				strengthTexels, aDim,	
				roughnessTexels, bDim,
				new TextureCombinationConfig(
					creationParams.TextureCombinationStrategy,
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R)
				),
				destinationBuffer.Span
			);
			return creationParams.TextureRegistry.Add(
				destinationBuffer.Span,
				destDim,
				creationParams.Config with { Name = creationParams.CreateTextureName(TextureTypeName), DataType = TextureDataType.LinearDataTwoChannelMax },
				creationParams.HeapPool
			);
		}
		finally {
			strengthEmbeddedTex?.Dispose();
			roughnessEmbeddedTex?.Dispose();
		}
	}
	
	GatheredMaterialData GatherAssetMaterial(UIntPtr assetHandle, int materialIndex, ReadOnlySpan<char> assetName, in TextureCreationConfig config, in ModelReadConfig readConfig, ref readonly byte assetRootDirStrRef, ModelLoadMaterialTextureRegistry textureRegistry, ThreadSafeHeapPoolWrapper heapPool) {
		var matParamsBuffer = stackalloc AssetMaterialParam[15];
		var matParams = new AssetMaterialParamGroup(
			matParamsBuffer + 0,
			matParamsBuffer + 1,
			matParamsBuffer + 2,
			matParamsBuffer + 3,
			matParamsBuffer + 4,
			matParamsBuffer + 5,
			matParamsBuffer + 6,
			matParamsBuffer + 7,
			matParamsBuffer + 8,
			matParamsBuffer + 9,
			matParamsBuffer + 10,
			matParamsBuffer + 11,
			matParamsBuffer + 12,
			matParamsBuffer + 13,
			matParamsBuffer + 14
		);

		GetLoadedAssetMaterialData(
			assetHandle,
			materialIndex,
			&matParams,
			out var alphaFormat,
			out var refractionThickness
		).ThrowIfFailure();

		var matNamePlusSuffixConcatLength = SpanUtils.GetConcatenatedLength(assetName, MatNameSuffix);
		Span<char> subResourceNameBuffer = stackalloc char[matNamePlusSuffixConcatLength + ResourceNameIndexSpaceMax + TexNameSuffix.Length + TexNameTypeSpaceMax];
		SpanUtils.Concatenate(subResourceNameBuffer, assetName, MatNameSuffix);
		_ = materialIndex.TryFormat(subResourceNameBuffer[matNamePlusSuffixConcatLength..], out var indexCharsCount, provider: CultureInfo.InvariantCulture);
		var matName = subResourceNameBuffer[..(matNamePlusSuffixConcatLength + indexCharsCount)];
		var matNameSlice = textureRegistry.AppendName(matName);

		var assetMaterialCreationParams = new AssetMaterialCreationParameters(
			assetHandle,
			materialIndex,
			subResourceNameBuffer,
			matName.Length,
			config,
			in assetRootDirStrRef,
			readConfig.HandleUriEscapedStrings,
			readConfig.GltfEmissiveStrengthScalar,
			readConfig.EmissiveStrengthCap,
			readConfig.EmbeddedTextureMapScalingStrategy,
			textureRegistry,
			heapPool
		);
		var colorMapSlot = GatherAssetColorMap(matParams.ColorParamsPtr, assetMaterialCreationParams);
		var atMapSlot = GatherAssetAbsorptionTransmissionMap(matParams.AbsorptionParamsPtr, matParams.TransmissionParamsPtr, assetMaterialCreationParams);
		var normalMapSlot = GatherAssetNormalMap(matParams.NormalParamsPtr, assetMaterialCreationParams);
		var ormMapSlot = GatherAssetOrmrMap(matParams.AmbientOcclusionParamsPtr, matParams.RoughnessParamsPtr, matParams.GlossinessParamsPtr, matParams.MetallicParamsPtr, matParams.IoRParamsPtr, atMapSlot >= 0, assetMaterialCreationParams);
		var anisotropyMapSlot = GatherAssetAnisotropyMap(matParams.AnisotropyAngleParamsPtr, matParams.AnisotropyStrengthParamsPtr, assetMaterialCreationParams);
		var emissiveMapSlot = GatherAssetEmissiveMap(matParams.EmissiveColorParamsPtr, matParams.EmissiveIntensityParamsPtr, assetMaterialCreationParams);
		var clearCoatMapSlot = atMapSlot >= 0 ? -1 : GatherAssetClearCoatMap(matParams.ClearCoatStrengthParamsPtr, matParams.ClearCoatRoughnessParamsPtr, assetMaterialCreationParams);

		return new GatheredMaterialData(
			colorMapSlot,
			atMapSlot,
			normalMapSlot,
			ormMapSlot,
			anisotropyMapSlot,
			emissiveMapSlot,
			clearCoatMapSlot,
			alphaFormat,
			refractionThickness,
			matNameSlice
		);
	}

	Material MaterializeAssetMaterial(in GatheredMaterialData gathered, ModelLoadMaterialTextureRegistry pending, ResourceGroup assetResources) {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();

		var colorMap = pending.Materialize(this, gathered.ColorMapSlot);
		var atMap = gathered.AbsorptionTransmissionMapSlot >= 0 ? pending.Materialize(this, gathered.AbsorptionTransmissionMapSlot) : (Texture?) null;
		var normalMap = gathered.NormalMapSlot >= 0 ? pending.Materialize(this, gathered.NormalMapSlot) : (Texture?) null;
		var ormMap = gathered.OrmMapSlot >= 0 ? pending.Materialize(this, gathered.OrmMapSlot) : (Texture?) null;
		var anisotropyMap = gathered.AnisotropyMapSlot >= 0 ? pending.Materialize(this, gathered.AnisotropyMapSlot) : (Texture?) null;
		var emissiveMap = gathered.EmissiveMapSlot >= 0 ? pending.Materialize(this, gathered.EmissiveMapSlot) : (Texture?) null;
		var clearCoatMap = gathered.ClearCoatMapSlot >= 0 ? pending.Materialize(this, gathered.ClearCoatMapSlot) : (Texture?) null;

		assetResources.Add(colorMap);
		if (atMap != null) assetResources.Add(atMap.Value);
		if (normalMap != null) assetResources.Add(normalMap.Value);
		if (ormMap != null) assetResources.Add(ormMap.Value);
		if (anisotropyMap != null) assetResources.Add(anisotropyMap.Value);
		if (emissiveMap != null) assetResources.Add(emissiveMap.Value);
		if (clearCoatMap != null) assetResources.Add(clearCoatMap.Value);

		var matName = pending.GetName(gathered.Name);

		if (atMap.HasValue) {
			return MaterialBuilder.CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
				AlphaMode = gathered.AlphaFormat switch { 2 => TransmissiveMaterialAlphaMode.FullBlending, _ => TransmissiveMaterialAlphaMode.MaskOnly },
				AbsorptionTransmissionMap = atMap.Value,
				AnisotropyMap = anisotropyMap,
				ColorMap = colorMap,
				EmissiveMap = emissiveMap,
				NormalMap = normalMap,
				OcclusionRoughnessMetallicReflectanceMap = ormMap,
				RefractionThickness = gathered.RefractionThickness.IsPositiveAndFinite() ? gathered.RefractionThickness : TransmissiveMaterialCreationConfig.DefaultRefractionThickness,
				Name = matName
			});
		}
		else {
			return MaterialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
				AlphaMode = gathered.AlphaFormat switch { 2 => StandardMaterialAlphaMode.FullBlending, _ => StandardMaterialAlphaMode.MaskOnly },
				AnisotropyMap = anisotropyMap,
				ClearCoatMap = clearCoatMap,
				ColorMap = colorMap,
				EmissiveMap = emissiveMap,
				NormalMap = normalMap,
				OcclusionRoughnessMetallicMap = ormMap,
				Name = matName
			});
		}
	}
}
