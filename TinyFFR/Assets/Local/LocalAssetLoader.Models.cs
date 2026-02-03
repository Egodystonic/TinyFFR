// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

unsafe partial class LocalAssetLoader {
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
		readonly FixedByteBufferPool _owningPool;
		readonly FixedByteBufferPool.FixedByteBuffer _rentedBuffer;
		public readonly XYPair<int> Dimensions;
		
		public Span<TexelRgba32> TexelSpan => _rentedBuffer.AsSpan<TexelRgba32>(Dimensions.Area);

		public EmbeddedTextureData(FixedByteBufferPool owningPool, FixedByteBufferPool.FixedByteBuffer rentedBuffer, XYPair<int> dimensions) {
			_owningPool = owningPool;
			_rentedBuffer = rentedBuffer;
			Dimensions = dimensions;
		}

		public void Dispose() => _owningPool.Return(_rentedBuffer);
	}
	
	const string DefaultModelName = "Unnamed Model";
	readonly FixedByteBufferPool _embeddedAssetTextureBufferPool;
	readonly ArrayPoolBackedMap<ResourceHandle<Model>, ResourceGroup> _loadedModels = new();
	nuint _prevModelHandle = 0;
	
	public Model CreateModel(Mesh mesh, Material material, ReadOnlySpan<char> name) {
		ThrowIfThisIsDisposed();
		++_prevModelHandle;
		var handle = (ResourceHandle<Model>) _prevModelHandle;
		_globals.StoreResourceNameOrDefaultIfEmpty(handle.Ident, name, DefaultModelName);
		var resGroup = _globals.ResourceGroupProvider.CreateGroup(disposeContainedResourcesWhenDisposed: false, initialCapacity: 2, name: name);
		resGroup.Add(mesh);
		resGroup.Add(material);
		_loadedModels.Add(_prevModelHandle, resGroup);
		return HandleToInstance(handle);
	}
	
	EmbeddedTextureData LoadAssetTexture(UIntPtr assetHandle, int textureIndex, ref readonly byte assetRootDirStrRef) {
		GetLoadedAssetTextureSize(
			assetHandle, 
			textureIndex,
			in assetRootDirStrRef,
			out var outWidth,
			out var outHeight
		).ThrowIfFailure();
		
		if (outWidth < 0 || outHeight < 0) throw new InvalidOperationException($"Width or height for asset texture at index '{textureIndex}' was malformed.");
		
		var texelBuffer = _embeddedAssetTextureBufferPool.Rent<TexelRgba32>(checked(outWidth * outHeight));
		
		GetLoadedAssetTextureData(
			assetHandle,
			textureIndex,
			in assetRootDirStrRef,
			(void*) texelBuffer.StartPtr,
			texelBuffer.SizeBytes,
			out outWidth,
			out outHeight
		).ThrowIfFailure();
		
		return new(_embeddedAssetTextureBufferPool, texelBuffer, new XYPair<int>(outWidth, outHeight));
	}
	
	Span<TexelRgba32> AbstractTexelSpanFromParamPtr(AssetMaterialParam* paramPtr, UIntPtr assetHandle, ref readonly byte assetRootDirStrRef, ref TexelRgba32 stackTexelWithDefaultValue, out EmbeddedTextureData? outEmbeddedTex) {
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				stackTexelWithDefaultValue = paramPtr->ToTexel();
				outEmbeddedTex = null;
				return new Span<TexelRgba32>(ref stackTexelWithDefaultValue);
		
			case AssetMaterialParamDataFormat.TextureMap:
				outEmbeddedTex = LoadAssetTexture(
					assetHandle,
					paramPtr->TextureMapIndex,
					in assetRootDirStrRef
				);
				return outEmbeddedTex.Value.TexelSpan;
			
			default: 
				outEmbeddedTex = null;
				return new Span<TexelRgba32>(ref stackTexelWithDefaultValue);
		}
	}
	
	static bool ParamPtrsRepresentIdenticalTextures(AssetMaterialParam* paramPtrA, AssetMaterialParam* paramPtrB) {
		return paramPtrA->Format == AssetMaterialParamDataFormat.TextureMap
			&& paramPtrB->Format == AssetMaterialParamDataFormat.TextureMap
			&& paramPtrA->TextureMapIndex == paramPtrB->TextureMapIndex;
	}
	
	Texture CreateAssetColorMap(AssetMaterialParam* paramPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				return TextureBuilder.CreateColorMap(
					paramPtr->ToColorVect(),
					includeAlpha: true,
					config with {
						IsLinearColorspace = true // Numerical outputs are in linear space
					}
				);
			
			case AssetMaterialParamDataFormat.TextureMap:
				using (var embeddedTex = LoadAssetTexture(assetHandle, paramPtr->TextureMapIndex, in assetRootDirStrRef)) {
					return TextureBuilder.CreateTexture(
						embeddedTex.TexelSpan,
						new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
						config with { IsLinearColorspace = false }
					);
				}
				
			default: return TextureBuilder.CreateColorMap();
		}
	}
	
	Texture? CreateAssetAbsorptionTransmissionMap(AssetMaterialParam* absorptionParamPtr, AssetMaterialParam* transmissionParamPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		if (absorptionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && transmissionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return null;
		
		// All in one texture, just load the whole thing once and return it, no combination required
		if (ParamPtrsRepresentIdenticalTextures(absorptionParamPtr, transmissionParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				assetHandle,
				absorptionParamPtr->TextureMapIndex,
				in assetRootDirStrRef
			);
			return TextureBuilder.CreateTexture(
				embeddedTex.TexelSpan,
				new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
				config with { IsLinearColorspace = false }
			);
		}
		
		var defaultAbsorptionTexel = new TexelRgba32(ITextureBuilder.DefaultAbsorption);
		var defaultTransmissionTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultTransmission, Real.Zero, Real.Zero, Real.Zero);
		
		var absorptionTexels = AbstractTexelSpanFromParamPtr(
			absorptionParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultAbsorptionTexel,
			out var absorptionEmbeddedTex
		);
		var transmissionTexels = AbstractTexelSpanFromParamPtr(
			transmissionParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultTransmissionTexel,
			out var transmissionEmbeddedTex
		);

		try {
			var aDim = absorptionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = transmissionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgba32>(destDim.Area);
			TextureUtils.CombineTextures(
				absorptionTexels, aDim,	
				transmissionTexels, bDim,
				new TextureCombinationConfig(
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Buffer
			);
			return TextureBuilder.CreateTexture(
				destinationBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = destDim },
				config with { IsLinearColorspace = !absorptionEmbeddedTex.HasValue } // Numerical values are linear, textures assumed sRGB
			);
		}
		finally {
			absorptionEmbeddedTex?.Dispose();
			transmissionEmbeddedTex?.Dispose();
		}
	}
	
	Texture? CreateAssetNormalMap(AssetMaterialParam* paramPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		switch (paramPtr->Format) {
			case AssetMaterialParamDataFormat.Numerical:
				return TextureBuilder.CreateTexture(
					paramPtr->ToTexel().ToRgb24(),
					config with {
						IsLinearColorspace = true
					}
				);
			
			case AssetMaterialParamDataFormat.TextureMap:
				using (var embeddedTex = LoadAssetTexture(assetHandle, paramPtr->TextureMapIndex, in assetRootDirStrRef)) {
					using var rgbTexelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
					TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Buffer);
					return TextureBuilder.CreateTexture(
						rgbTexelBuffer.Buffer,
						new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
						config with { IsLinearColorspace = true }
					);
				}
				
			default: return null;
		}
	}
	
	Texture? CreateAssetOrmrMap(AssetMaterialParam* occlusionParamPtr, AssetMaterialParam* roughnessParamPtr, AssetMaterialParam* glossinessParamPtr, AssetMaterialParam* metallicParamPtr, AssetMaterialParam* iorParamPtr, bool reflectanceRequired, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		// Maintainer's note:
		// Reflectance can not be stored in a texture map, because it's actually exposed as IoR from assimp and there's no industry-normalized range mapping [0-1] to any known IoR range
		// So either we don't specify it at all or if there's a numerical value it's considered to be IoR and must be converted
		if (occlusionParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded 
			&& roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded 
			&& glossinessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded
			&& metallicParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded
			&& iorParamPtr->Format != AssetMaterialParamDataFormat.Numerical) {
			return null;
		}
		
		var reflectanceValue = iorParamPtr->Format == AssetMaterialParamDataFormat.Numerical
			? MathF.Pow((iorParamPtr->NumericalValueR - 1f) / (iorParamPtr->NumericalValueR + 1f), 2f) // Conversion from IoR to reflectance
			: (float?) null;
		if (reflectanceRequired) reflectanceValue ??= ITextureBuilder.DefaultReflectance;
		
		var occlusionAndRoughnessAreCombinedTextures = ParamPtrsRepresentIdenticalTextures(occlusionParamPtr, roughnessParamPtr);
		var roughnessAndMetallicAreCombinedTextures = ParamPtrsRepresentIdenticalTextures(roughnessParamPtr, metallicParamPtr);
		
		// No reflectance and the rest are a singular ORM map, just load it and be done
		if (reflectanceValue == null && occlusionAndRoughnessAreCombinedTextures && roughnessAndMetallicAreCombinedTextures) {
			using var embeddedTex = LoadAssetTexture(
				assetHandle,
				occlusionParamPtr->TextureMapIndex,
				in assetRootDirStrRef
			);
			return TextureBuilder.CreateTexture(
				embeddedTex.TexelSpan,
				new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
				config with { IsLinearColorspace = true }
			);
		}
		
		var occlusionAndMetallicAreCombinedTextures = ParamPtrsRepresentIdenticalTextures(occlusionParamPtr, metallicParamPtr);
		
		var defaultOcclusionTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion, ITextureBuilder.DefaultOcclusion);
		var defaultRoughnessTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness, ITextureBuilder.DefaultRoughness);
		var defaultMetallicTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic, ITextureBuilder.DefaultMetallic);
		
		var glossinessSpecifiedOverRoughness = roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && glossinessParamPtr->Format != AssetMaterialParamDataFormat.NotIncluded;
		
		var occlusionTexels = AbstractTexelSpanFromParamPtr(
			occlusionParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultOcclusionTexel,
			out var occlusionEmbeddedTex
		);
		var roughnessTexels = AbstractTexelSpanFromParamPtr(
			glossinessSpecifiedOverRoughness ? glossinessParamPtr : roughnessParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultRoughnessTexel,
			out var roughnessEmbeddedTex
		);
		var metallicTexels = AbstractTexelSpanFromParamPtr(
			metallicParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultMetallicTexel,
			out var metallicEmbeddedTex
		);
		
		try {
			var aDim = occlusionEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = roughnessEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var cDim = metallicEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim, cDim);
			var metallicChannel = (occlusionAndRoughnessAreCombinedTextures, roughnessAndMetallicAreCombinedTextures, occlusionAndMetallicAreCombinedTextures) switch {
				(_, false, false) => ColorChannel.R,
				(false, true, false) => ColorChannel.G,
				(false, false, true) => ColorChannel.G,
				_ => ColorChannel.B,	
			};
			if (glossinessSpecifiedOverRoughness) TextureUtils.NegateTexture(roughnessTexels, bDim);
			if (reflectanceValue.HasValue) {
				using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgba32>(destDim.Area);
				var reflectanceTexel = TexelRgba32.FromNormalizedFloats(reflectanceValue.Value, reflectanceValue.Value, reflectanceValue.Value, reflectanceValue.Value);
				TextureUtils.CombineTextures(
					occlusionTexels, aDim,	
					roughnessTexels, bDim,
					metallicTexels, cDim,
					new ReadOnlySpan<TexelRgba32>(in reflectanceTexel), XYPair<int>.One,
					new TextureCombinationConfig(
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, occlusionAndRoughnessAreCombinedTextures ? ColorChannel.G : ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, metallicChannel),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureD, ColorChannel.A)
					),
					destinationBuffer.Buffer
				);
				return TextureBuilder.CreateTexture(
					destinationBuffer.Buffer,
					new TextureGenerationConfig { Dimensions = destDim },
					config with { IsLinearColorspace = true }
				);
			}
			else {
				using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgb24>(destDim.Area);
				TextureUtils.CombineTextures(
					occlusionTexels, aDim,	
					roughnessTexels, bDim,
					metallicTexels, cDim,
					new TextureCombinationConfig(
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, occlusionAndRoughnessAreCombinedTextures ? ColorChannel.G : ColorChannel.R),
						new TextureCombinationSource(TextureCombinationSourceTexture.TextureC, metallicChannel)
					),
					destinationBuffer.Buffer
				);
				return TextureBuilder.CreateTexture(
					destinationBuffer.Buffer,
					new TextureGenerationConfig { Dimensions = destDim },
					config with { IsLinearColorspace = true }
				);
			}
		}
		finally {
			occlusionEmbeddedTex?.Dispose();
			roughnessEmbeddedTex?.Dispose();
			metallicEmbeddedTex?.Dispose();
		}
	}
	
	Texture? CreateAssetAnisotropyMap(AssetMaterialParam* angleParamPtr, AssetMaterialParam* strengthParamPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		if (angleParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && strengthParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return null;
		
		// All in one texture; the only well-defined texture format is in the glTF spec: https://github.com/KhronosGroup/glTF/blob/main/extensions/2.0/Khronos/KHR_materials_anisotropy/README.md
		// So we assume this format is the one being used, which matches what TinyFFR already expects thankfully
		if (ParamPtrsRepresentIdenticalTextures(angleParamPtr, strengthParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				assetHandle,
				angleParamPtr->TextureMapIndex,
				in assetRootDirStrRef
			);
			using var rgbTexelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
			TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Buffer);
			return TextureBuilder.CreateTexture(
				rgbTexelBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
				config with { IsLinearColorspace = true }
			);
		}
		
		var defaultAngleTexel = new TexelRgba32(0, 0, 0, 0);
		var defaultStrengthTexel = new TexelRgba32(255, 255, 255, 255);
		
		var angleTexels = AbstractTexelSpanFromParamPtr(
			angleParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultAngleTexel,
			out var angleEmbeddedTex
		);
		var strengthTexels = AbstractTexelSpanFromParamPtr(
			strengthParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultStrengthTexel,
			out var strengthEmbeddedTex
		);

		try {
			var aDim = angleEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = strengthEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgb24>(destDim.Area);
			TextureUtils.CombineTextures(
				angleTexels, aDim,	
				strengthTexels, bDim,
				new TextureCombinationConfig(
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Buffer
			);
			// After combining the disparate textures we need to convert them from angle/strength to tangent-space vector + strength
			IAssetLoader.ConvertRadialAngleToVectorFormatAnisotropy(destinationBuffer.Buffer, Orientation2D.Right, AnisotropyRadialAngleRange.ZeroTo360, true, ColorChannel.B);
			return TextureBuilder.CreateTexture(
				destinationBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = destDim },
				config with { IsLinearColorspace = true }
			);
		}
		finally {
			angleEmbeddedTex?.Dispose();
			strengthEmbeddedTex?.Dispose();
		}
	}
	
	Texture? CreateAssetEmissiveMap(AssetMaterialParam* colorParamPtr, AssetMaterialParam* intensityParamPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		if (colorParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && intensityParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return null;
		
		// All in one texture, just load the whole thing once and return it, no combination required
		if (ParamPtrsRepresentIdenticalTextures(colorParamPtr, intensityParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				assetHandle,
				colorParamPtr->TextureMapIndex,
				in assetRootDirStrRef
			);
			return TextureBuilder.CreateTexture(
				embeddedTex.TexelSpan,
				new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
				config with { IsLinearColorspace = false }
			);
		}
		
		var defaultColorTexel = new TexelRgba32(ITextureBuilder.DefaultEmissiveColor);
		var defaultIntensityTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity, ITextureBuilder.DefaultEmissiveIntensity);
		
		var colorTexels = AbstractTexelSpanFromParamPtr(
			colorParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultColorTexel,
			out var colorEmbeddedTex
		);
		var intensityTexels = AbstractTexelSpanFromParamPtr(
			intensityParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultIntensityTexel,
			out var intensityEmbeddedTex
		);

		try {
			var aDim = colorEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = intensityEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgba32>(destDim.Area);
			TextureUtils.CombineTextures(
				colorTexels, aDim,	
				intensityTexels, bDim,
				new TextureCombinationConfig(
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.G),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.B),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R)
				),
				destinationBuffer.Buffer
			);
			return TextureBuilder.CreateTexture(
				destinationBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = destDim },
				config with { IsLinearColorspace = !colorEmbeddedTex.HasValue } // Numerical values are linear, textures assumed sRGB
			);
		}
		finally {
			colorEmbeddedTex?.Dispose();
			intensityEmbeddedTex?.Dispose();
		}
	}
	
	Texture? CreateAssetClearCoatMap(AssetMaterialParam* strengthParamPtr, AssetMaterialParam* roughnessParamPtr, UIntPtr assetHandle, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
		if (strengthParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded && roughnessParamPtr->Format == AssetMaterialParamDataFormat.NotIncluded) return null;
		
		// All in one texture; the only well-defined texture format is in the glTF spec: https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_clearcoat
		// So we assume this format is the one being used, which matches what TinyFFR already expects thankfully
		if (ParamPtrsRepresentIdenticalTextures(strengthParamPtr, roughnessParamPtr)) {
			using var embeddedTex = LoadAssetTexture(
				assetHandle,
				strengthParamPtr->TextureMapIndex,
				in assetRootDirStrRef
			);
			using var rgbTexelBuffer = _globals.HeapPool.Borrow<TexelRgb24>(embeddedTex.Dimensions.Area);
			TextureUtils.Convert(embeddedTex.TexelSpan, rgbTexelBuffer.Buffer);
			return TextureBuilder.CreateTexture(
				rgbTexelBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = embeddedTex.Dimensions },
				config with { IsLinearColorspace = true }
			);
		}
		
		var defaultStrengthTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness, ITextureBuilder.DefaultClearCoatThickness);
		var defaultRoughnessTexel = TexelRgba32.FromNormalizedFloats(ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness, ITextureBuilder.DefaultClearCoatRoughness);
		
		var strengthTexels = AbstractTexelSpanFromParamPtr(
			strengthParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultStrengthTexel,
			out var strengthEmbeddedTex
		);
		var roughnessTexels = AbstractTexelSpanFromParamPtr(
			roughnessParamPtr,
			assetHandle,
			in assetRootDirStrRef,
			ref defaultRoughnessTexel,
			out var roughnessEmbeddedTex
		);

		try {
			var aDim = strengthEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var bDim = roughnessEmbeddedTex?.Dimensions ?? XYPair<int>.One;
			var destDim = TextureUtils.GetCombinedTextureDimensions(aDim, bDim);
			using var destinationBuffer = _globals.HeapPool.Borrow<TexelRgb24>(destDim.Area);
			TextureUtils.CombineTextures(
				strengthTexels, aDim,	
				roughnessTexels, bDim,
				new TextureCombinationConfig(
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureB, ColorChannel.R),
					new TextureCombinationSource(TextureCombinationSourceTexture.TextureA, ColorChannel.R)
				),
				destinationBuffer.Buffer
			);
			return TextureBuilder.CreateTexture(
				destinationBuffer.Buffer,
				new TextureGenerationConfig { Dimensions = destDim },
				config with { IsLinearColorspace = true }
			);
		}
		finally {
			strengthEmbeddedTex?.Dispose();
			roughnessEmbeddedTex?.Dispose();
		}
	}
	
	Material CreateAssetMaterial(UIntPtr assetHandle, int materialIndex, ResourceGroup assetResources, in TextureCreationConfig config, ref readonly byte assetRootDirStrRef) {
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
		
		var colorMap = CreateAssetColorMap(matParams.ColorParamsPtr, assetHandle, in config, in assetRootDirStrRef);
		var atMap = CreateAssetAbsorptionTransmissionMap(matParams.AbsorptionParamsPtr, matParams.TransmissionParamsPtr, assetHandle, in config, in assetRootDirStrRef);
		var normalMap = CreateAssetNormalMap(matParams.NormalParamsPtr, assetHandle, in config, in assetRootDirStrRef);
		var ormMap = CreateAssetOrmrMap(matParams.AmbientOcclusionParamsPtr, matParams.RoughnessParamsPtr, matParams.GlossinessParamsPtr, matParams.MetallicParamsPtr, matParams.IoRParamsPtr, atMap.HasValue, assetHandle, in config, in assetRootDirStrRef);
		var anisotropyMap = CreateAssetAnisotropyMap(matParams.AnisotropyAngleParamsPtr, matParams.AnisotropyStrengthParamsPtr, assetHandle, in config, in assetRootDirStrRef);
		var emissiveMap = CreateAssetEmissiveMap(matParams.EmissiveColorParamsPtr, matParams.EmissiveIntensityParamsPtr, assetHandle, in config, in assetRootDirStrRef);
		var clearCoatMap = atMap.HasValue ? null : CreateAssetClearCoatMap(matParams.ClearCoatStrengthParamsPtr, matParams.ClearCoatRoughnessParamsPtr, assetHandle, in config, in assetRootDirStrRef);

		assetResources.Add(colorMap);
		if (atMap != null) assetResources.Add(atMap.Value);
		if (normalMap != null) assetResources.Add(normalMap.Value);
		if (ormMap != null) assetResources.Add(ormMap.Value);
		if (anisotropyMap != null) assetResources.Add(anisotropyMap.Value);
		if (emissiveMap != null) assetResources.Add(emissiveMap.Value);
		if (clearCoatMap != null) assetResources.Add(clearCoatMap.Value);
	
		if (atMap.HasValue) {
			return MaterialBuilder.CreateTransmissiveMaterial(new TransmissiveMaterialCreationConfig {
				AlphaMode = alphaFormat switch { 2 => TransmissiveMaterialAlphaMode.FullBlending, _ => TransmissiveMaterialAlphaMode.MaskOnly },
				AbsorptionTransmissionMap = atMap.Value,
				AnisotropyMap = anisotropyMap,
				ColorMap = colorMap,
				EmissiveMap = emissiveMap,
				NormalMap = normalMap,
				OcclusionRoughnessMetallicReflectanceMap = ormMap,
				RefractionThickness = refractionThickness >= 0f ? refractionThickness : TransmissiveMaterialCreationConfig.DefaultRefractionThickness
			});
		}
		else {
			return MaterialBuilder.CreateStandardMaterial(new StandardMaterialCreationConfig {
				AlphaMode = alphaFormat switch { 2 => StandardMaterialAlphaMode.FullBlending, _ => StandardMaterialAlphaMode.MaskOnly },
				AnisotropyMap = anisotropyMap,
				ClearCoatMap = clearCoatMap,
				ColorMap = colorMap,
				EmissiveMap = emissiveMap,
				NormalMap = normalMap,
				OcclusionRoughnessMetallicMap = ormMap
			});
		}
	}
	
	public ResourceGroup LoadModels(ReadOnlySpan<char> filePath, in AssetCreationConfig config, in AssetReadConfig readConfig) {
		const int MaxIndicesOnStack = 1024;
		ThrowIfThisIsDisposed();
		config.ThrowIfInvalid();
		readConfig.ThrowIfInvalid();
		
		try {
			_assetFilePathBuffer.ConvertFromUtf16(filePath);
			
			LoadAssetFileInToMemory(
				in _assetFilePathBuffer.AsRef,
				readConfig.MeshConfig.FixCommonExportErrors,
				readConfig.MeshConfig.OptimizeForGpu,
				out var assetHandle
			).ThrowIfFailure();
			
			_assetFilePathBuffer.ConvertFromUtf16(Path.GetDirectoryName(filePath));

			try {
				GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure(); 
				GetLoadedAssetMaterialCount(assetHandle, out var materialCount).ThrowIfFailure(); 
				GetLoadedAssetTextureCount(assetHandle, out var textureCount).ThrowIfFailure();
				
				var result = _globals.ResourceGroupProvider.CreateGroup(
					disposeContainedResourcesWhenDisposed: true,
					name: config.Name,
					meshCount + materialCount + textureCount
				);
				
				var materialToGroupAddOrderMap = materialCount > MaxIndicesOnStack ? new int[materialCount] : stackalloc int[materialCount];
				
				for (var i = 0; i < meshCount; ++i) {
					GetLoadedAssetMeshVertexCount(assetHandle, i, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, i, out var tCount).ThrowIfFailure();
					
					var fixedVertexBuffer = _vertexTriangleBufferPool.Rent<MeshVertex>(vCount);
					var fixedTriangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(tCount);
					try {
						CopyLoadedAssetMeshVertices(assetHandle, i, fixedVertexBuffer.Size<MeshVertex>(), (MeshVertex*) fixedVertexBuffer.StartPtr).ThrowIfFailure();
						CopyLoadedAssetMeshTriangles(assetHandle, i, fixedTriangleBuffer.Size<VertexTriangle>(), (VertexTriangle*) fixedTriangleBuffer.StartPtr).ThrowIfFailure();
						
						var mesh = _meshBuilder.CreateMesh(
							fixedVertexBuffer.AsReadOnlySpan<MeshVertex>(vCount),
							fixedTriangleBuffer.AsReadOnlySpan<VertexTriangle>(tCount),
							config.MeshConfig
						);
						
						result.Add(mesh);
					
						GetLoadedAssetMeshMaterialIndex(
							assetHandle, 
							i, 
							out var matIndex
						).ThrowIfFailure();
						
						if (matIndex < 0 || matIndex >= materialCount) throw new InvalidOperationException($"Mesh at index '{i}' references material at index '{matIndex}' but asset only contains {materialCount} materials.");
						
						Material mat;
						if (materialToGroupAddOrderMap[matIndex] > 0) {
							mat = result.Materials[materialToGroupAddOrderMap[matIndex] - 1];
						}
						else {
							mat = CreateAssetMaterial(
								assetHandle,
								matIndex,
								result,
								config.TextureConfig,
								in _assetFilePathBuffer.AsRef
							);
							result.Add(mat);
							materialToGroupAddOrderMap[matIndex] = result.Materials.Count;
						}
						
						result.Add(CreateModel(mesh, mat, default));
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}
				}
				
				result.Seal();
				return result;
			}
			finally {
				UnloadAssetFileFromMemory(assetHandle).ThrowIfFailure();
			}
		}
		catch (Exception e) {
			if (!File.Exists(filePath.ToString())) throw new InvalidOperationException($"File '{filePath}' does not exist.", e);
			else throw;
		}
	}

	public Mesh GetMesh(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Meshes[0];
	}
	public Material GetMaterial(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _loadedModels[handle].Materials[0];
	}

	public string GetNameAsNewStringObject(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return new String(_globals.GetResourceName(handle.Ident, DefaultBackdropTextureName));
	}
	public int GetNameLength(ResourceHandle<Model> handle) {
		ThrowIfThisOrHandleIsDisposed(handle);
		return _globals.GetResourceName(handle.Ident, DefaultBackdropTextureName).Length;
	}
	public void CopyName(ResourceHandle<Model> handle, Span<char> destinationBuffer) {
		ThrowIfThisOrHandleIsDisposed(handle);
		_globals.CopyResourceName(handle.Ident, DefaultBackdropTextureName, destinationBuffer);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	Model HandleToInstance(ResourceHandle<Model> h) => new(h, this);

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_asset_file_in_to_memory")]
	static extern InteropResult LoadAssetFileInToMemory(
		ref readonly byte utf8FileNameBufferPtr,
		InteropBool fixCommonImporterErrors,
		InteropBool optimize,
		out UIntPtr outAssetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_material_count")]
	static extern InteropResult GetLoadedAssetMaterialCount(
		UIntPtr assetHandle,
		out int outMaterialCount
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_count")]
	static extern InteropResult GetLoadedAssetTextureCount(
		UIntPtr assetHandle,
		out int outTextureCount
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_material_index")]
	static extern InteropResult GetLoadedAssetMeshMaterialIndex(
		UIntPtr assetHandle,
		int meshIndex,
		out int outMaterialIndex
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_size")]
	static extern InteropResult GetLoadedAssetTextureSize(
		UIntPtr assetHandle,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		out int outWidth,
		out int outHeight
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_texture_data")]
	static extern InteropResult GetLoadedAssetTextureData(
		UIntPtr assetHandle,
		int textureIndex,
		ref readonly byte utf8AssetRootDirPathBufferPtr,
		void* dataBufferPtr,
		int bufferLengthBytes,
		out int outWidth,
		out int outHeight
	);
	
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_material_data")]
	static extern InteropResult GetLoadedAssetMaterialData(
		UIntPtr assetHandle,
		int materialIndex,
		AssetMaterialParamGroup* paramGroupPtr,
		out int outAlphaFormat,
		out float outRefractionThickness
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_asset_file_from_memory")]
	static extern InteropResult UnloadAssetFileFromMemory(
		UIntPtr assetHandle
	);
	#endregion
	
	#region Disposal
	public bool IsDisposed(ResourceHandle<Model> handle) => _isDisposed || !_loadedModels.ContainsKey(handle);

	public void Dispose(ResourceHandle<Model> handle) => Dispose(handle, removeFromCollection: true);
	void Dispose(ResourceHandle<Model> handle, bool removeFromCollection) {
		if (IsDisposed(handle)) return;
		_globals.DependencyTracker.ThrowForPrematureDisposalIfTargetHasDependents(HandleToInstance(handle));
		_loadedModels[handle].Dispose();
		_globals.DisposeResourceNameIfExists(handle.Ident);
		if (removeFromCollection) _loadedModels.Remove(handle);
	}
	
	void ThrowIfThisOrHandleIsDisposed(ResourceHandle<Model> handle) => ObjectDisposedException.ThrowIf(IsDisposed(handle), typeof(Model));
	#endregion
}