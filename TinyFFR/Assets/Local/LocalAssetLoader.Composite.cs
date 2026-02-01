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
	readonly record struct AssetMaterialParam(AssetMaterialParamDataFormat Format, int TextureMapIndex, float NumericalValueR, float NumericalValueG, float NumericalValueB, float NumericalValueA);
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8 * 14)] 
	readonly struct AssetMaterialParamGroup {
		public readonly AssetMaterialParam* ColorParamsPtr;
		public readonly AssetMaterialParam* NormalParamsPtr;
		public readonly AssetMaterialParam* AmbientOcclusionParamsPtr;
		public readonly AssetMaterialParam* RoughnessParamsPtr;
		public readonly AssetMaterialParam* GlossinessParamsPtr;
		public readonly AssetMaterialParam* MetallicParamsPtr;
		public readonly AssetMaterialParam* ReflectanceParamsPtr;
		public readonly AssetMaterialParam* IoRParamsPtr;
		public readonly AssetMaterialParam* AbsorptionParamsPtr;
		public readonly AssetMaterialParam* TransmissionParamsPtr;
		public readonly AssetMaterialParam* EmissiveParamsPtr;
		public readonly AssetMaterialParam* EmissiveIntensityParamsPtr;
		public readonly AssetMaterialParam* AnisotropyAngleParamsPtr;
		public readonly AssetMaterialParam* AnisotropyStrengthParamsPtr;

		public AssetMaterialParamGroup(AssetMaterialParam* colorParamsPtr, AssetMaterialParam* normalParamsPtr, AssetMaterialParam* ambientOcclusionParamsPtr, AssetMaterialParam* roughnessParamsPtr, AssetMaterialParam* glossinessParamsPtr, AssetMaterialParam* metallicParamsPtr, AssetMaterialParam* reflectanceParamsPtr, AssetMaterialParam* ioRParamsPtr, AssetMaterialParam* absorptionParamsPtr, AssetMaterialParam* transmissionParamsPtr, AssetMaterialParam* emissiveParamsPtr, AssetMaterialParam* emissiveIntensityParamsPtr, AssetMaterialParam* anisotropyAngleParamsPtr, AssetMaterialParam* anisotropyStrengthParamsPtr) {
			ColorParamsPtr = colorParamsPtr;
			NormalParamsPtr = normalParamsPtr;
			AmbientOcclusionParamsPtr = ambientOcclusionParamsPtr;
			RoughnessParamsPtr = roughnessParamsPtr;
			GlossinessParamsPtr = glossinessParamsPtr;
			MetallicParamsPtr = metallicParamsPtr;
			ReflectanceParamsPtr = reflectanceParamsPtr;
			IoRParamsPtr = ioRParamsPtr;
			AbsorptionParamsPtr = absorptionParamsPtr;
			TransmissionParamsPtr = transmissionParamsPtr;
			EmissiveParamsPtr = emissiveParamsPtr;
			EmissiveIntensityParamsPtr = emissiveIntensityParamsPtr;
			AnisotropyAngleParamsPtr = anisotropyAngleParamsPtr;
			AnisotropyStrengthParamsPtr = anisotropyStrengthParamsPtr;
		}
	};
	
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
	
	public ResourceGroup Load(ReadOnlySpan<char> filePath, in AssetCreationConfig config, in AssetReadConfig readConfig) {
		static (FixedByteBufferPool.FixedByteBuffer TexelBuffer, XYPair<int> Dimensions) LoadAssetTexture(FixedByteBufferPool texelBufferPool, UIntPtr assetHandle, int textureIndex, ref readonly byte assetRootDirStrRef) {
			GetLoadedAssetTextureSize(
				assetHandle, 
				textureIndex,
				in assetRootDirStrRef,
				out var outWidth,
				out var outHeight
			).ThrowIfFailure();
			
			if (outWidth < 0 || outHeight < 0) throw new InvalidOperationException($"Width or height for asset texture at index '{textureIndex}' was malformed.");
			
			var texelBuffer = texelBufferPool.Rent<TexelRgba32>(checked(outWidth * outHeight));
			
			GetLoadedAssetTextureData(
				assetHandle,
				textureIndex,
				in assetRootDirStrRef,
				(void*) texelBuffer.StartPtr,
				texelBuffer.SizeBytes,
				out outWidth,
				out outHeight
			).ThrowIfFailure();
			
			return (texelBuffer, new XYPair<int>(outWidth, outHeight));
		}
		
		static Material CreateAssetMaterial(FixedByteBufferPool texelBufferPool, ITextureBuilder textureBuilder, IMaterialBuilder materialBuilder, UIntPtr assetHandle, int materialIndex, Span<int> materialToGroupAddOrderMap, ref readonly byte assetRootDirStrRef, in TextureCreationConfig config, ResourceGroup result) {
			var matParamsBuffer = stackalloc AssetMaterialParam[14];
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
				matParamsBuffer + 13
			);
			(FixedByteBufferPool.FixedByteBuffer TexelBuffer, XYPair<int> Dimensions) loadedTex;
			
			GetLoadedAssetMaterialData(
				assetHandle,
				materialIndex,
				&matParams
			).ThrowIfFailure();
			
			Texture colorMap;
			Texture? atMap = null;
			Texture? normalMap = null;
			Texture? ormMap = null;
			Texture? anisotropyMap = null;
			Texture? emissiveMap = null;
			Texture? clearCoatMap = null;

			#region ColorMap
			switch (matParams.ColorParamsPtr->Format) {
				case AssetMaterialParamDataFormat.Numerical: {
					colorMap = textureBuilder.CreateColorMap(
						new ColorVect(matParams.ColorParamsPtr->NumericalValueR, matParams.ColorParamsPtr->NumericalValueG, matParams.ColorParamsPtr->NumericalValueB, matParams.ColorParamsPtr->NumericalValueA), 
						includeAlpha: true, 
						config with { IsLinearColorspace = false }
					);
					break;	
				}
				case AssetMaterialParamDataFormat.TextureMap: {

					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.ColorParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					colorMap = textureBuilder.CreateTexture(
						loadedTex.TexelBuffer.AsReadOnlySpan<TexelRgba32>(loadedTex.Dimensions.Area),
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
				default: {
					colorMap = textureBuilder.CreateColorMap();
					break;
				}
			}
			result.Add(colorMap);
			#endregion

			#region AtMap
			switch ((matParams.AbsorptionParamsPtr->Format, matParams.TransmissionParamsPtr->Format)) {
				case (AssetMaterialParamDataFormat.NotIncluded, AssetMaterialParamDataFormat.Numerical): {
					atMap = textureBuilder.CreateAbsorptionTransmissionMap(
						transmission: matParams.TransmissionParamsPtr->NumericalValueR
					);
					break;
				}
				case (AssetMaterialParamDataFormat.NotIncluded, AssetMaterialParamDataFormat.TextureMap): {
					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.TransmissionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					// We assume the transmission data is in the R channel, so we move it to A, and leave absorption as black
					var texels = loadedTex.TexelBuffer.AsSpan<TexelRgba32>(loadedTex.Dimensions.Area);
					for (var i = 0; i < texels.Length; ++i) texels[i] = new TexelRgba32(0, 0, 0, texels[i].R);
					atMap = textureBuilder.CreateTexture(
						texels,
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
				case (AssetMaterialParamDataFormat.TextureMap, AssetMaterialParamDataFormat.NotIncluded): {
					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.AbsorptionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					// We assume the transmission data is in the R channel, so we move it to A, and leave absorption as black
					var texels = loadedTex.TexelBuffer.AsSpan<TexelRgba32>(loadedTex.Dimensions.Area);
					for (var i = 0; i < texels.Length; ++i) texels[i] = texels[i] with { A = (byte) (ITextureBuilder.DefaultTransmission * Byte.MaxValue) };
					atMap = textureBuilder.CreateTexture(
						texels,
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
				case (AssetMaterialParamDataFormat.TextureMap, AssetMaterialParamDataFormat.Numerical): {
					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.AbsorptionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					var texels = loadedTex.TexelBuffer.AsSpan<TexelRgba32>(loadedTex.Dimensions.Area);
					for (var i = 0; i < texels.Length; ++i) texels[i] = texels[i] with { A = (byte) (matParams.TransmissionParamsPtr->NumericalValueR * Byte.MaxValue) };
					atMap = textureBuilder.CreateTexture(
						texels,
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
				case (AssetMaterialParamDataFormat.TextureMap, AssetMaterialParamDataFormat.TextureMap) when matParams.TransmissionParamsPtr->TextureMapIndex == matParams.AbsorptionParamsPtr->TextureMapIndex: {
					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.TransmissionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					atMap = textureBuilder.CreateTexture(
						loadedTex.TexelBuffer.AsReadOnlySpan<TexelRgba32>(loadedTex.Dimensions.Area),
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
				case (AssetMaterialParamDataFormat.TextureMap, AssetMaterialParamDataFormat.TextureMap): {
					var absorptionTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.AbsorptionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					var transmissionTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.TransmissionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					var texels = absorptionTex.TexelBuffer.AsSpan<TexelRgba32>(absorptionTex.Dimensions.Area);
					// TODO better rescaling
					for (var i = 0; i < texels.Length; ++i) texels[i] = texels[i] with { A = transmissionTex.TexelBuffer.AsSpan<TexelRgba32>(transmissionTex.Dimensions.Area)[i % transmissionTex.Dimensions.Area].R };
					atMap = textureBuilder.CreateTexture(
						texels,
						new TextureGenerationConfig { Dimensions = absorptionTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(absorptionTex.TexelBuffer);
					texelBufferPool.Return(transmissionTex.TexelBuffer);
					break;
				}
				case (AssetMaterialParamDataFormat.Numerical, AssetMaterialParamDataFormat.NotIncluded): {
					atMap = textureBuilder.CreateAbsorptionTransmissionMap(
						absorption: new ColorVect(
							matParams.AbsorptionParamsPtr->NumericalValueR,
							matParams.AbsorptionParamsPtr->NumericalValueG,
							matParams.AbsorptionParamsPtr->NumericalValueB
						)
					);
					break;
				}
				case (AssetMaterialParamDataFormat.Numerical, AssetMaterialParamDataFormat.Numerical): {
					atMap = textureBuilder.CreateAbsorptionTransmissionMap(
						absorption: new ColorVect(
							matParams.AbsorptionParamsPtr->NumericalValueR,
							matParams.AbsorptionParamsPtr->NumericalValueG,
							matParams.AbsorptionParamsPtr->NumericalValueB
						),
						transmission: matParams.TransmissionParamsPtr->NumericalValueR
					);
					break;
				}
				case (AssetMaterialParamDataFormat.Numerical, AssetMaterialParamDataFormat.TextureMap): {
					loadedTex = LoadAssetTexture(
						texelBufferPool,
						assetHandle,
						matParams.TransmissionParamsPtr->TextureMapIndex,
						in assetRootDirStrRef
					);
					var texels = loadedTex.TexelBuffer.AsSpan<TexelRgba32>(loadedTex.Dimensions.Area);
					for (var i = 0; i < texels.Length; ++i) texels[i] = new TexelRgba32(new ColorVect(matParams.AbsorptionParamsPtr->NumericalValueR, matParams.AbsorptionParamsPtr->NumericalValueG, matParams.AbsorptionParamsPtr->NumericalValueB)) with { A = texels[i].R };
					atMap = textureBuilder.CreateTexture(
						texels,
						new TextureGenerationConfig { Dimensions = loadedTex.Dimensions },
						config with { IsLinearColorspace = false } 
					);
					texelBufferPool.Return(loadedTex.TexelBuffer);
					break;
				}
			}
			if (atMap != null) result.Add(atMap.Value);
			#endregion
			
			result.Add(mat);
			materialToGroupAddOrderMap[materialIndex] = result.Materials.Count;
			
			return mat;
		}
		
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
								_embeddedAssetTextureBufferPool,
								_textureBuilder,
								_materialBuilder,
								assetHandle,
								matIndex,
								materialToGroupAddOrderMap, 
								in _assetFilePathBuffer.AsRef,
								config.TextureConfig,
								result
							);
						}
						
						result.Add(CreateModel(mesh, mat, default));
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}
				}
				
				// TODO if skip is false, add the un-added mats/textures here
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				
				var meshMaterialMap = meshCount > 1024 ? new int[meshCount] : stackalloc int[meshCount];
				for (var m = 0; m < meshCount; ++m) {
					GetLoadedAssetMeshMaterialIndex(
						assetHandle, 
						m, 
						out var matIndex
					).ThrowIfFailure();
					meshMaterialMap[m] = matIndex; 
				}
				var materialIndexToResultIndexMap = materialCount > 1024 ? new int[materialCount] : stackalloc int[materialCount];
				
				
				
				for (var t = 0; t < textureCount; ++t) {
					GetLoadedAssetTextureSize(
						assetHandle, 
						t,
						in _assetFilePathBuffer.AsRef,
						out var outWidth,
						out var outHeight
					).ThrowIfFailure();
					var texelBuffer = _embeddedAssetTextureBufferPool.Rent<TexelRgba32>(checked(outWidth * outHeight));
					try {
						GetLoadedAssetTextureData(
							assetHandle,
							t,
							in _assetFilePathBuffer.AsRef,
							(void*) texelBuffer.StartPtr,
							texelBuffer.SizeBytes,
							out outWidth,
							out outHeight
						).ThrowIfFailure();
						var actualLengthTexels = checked(outWidth * outHeight);
						result.Add(
							_textureBuilder.CreateTexture(
								texelBuffer.AsSpan<TexelRgba32>(actualLengthTexels),
								new TextureGenerationConfig { Dimensions = (outWidth, outHeight) },
								config.TextureConfig // TODO we need to determine whether it should be linear colorspace or not, that's gonna require us setting up the mapping beforehand
							)
						);
					}
					finally {
						_embeddedAssetTextureBufferPool.Return(texelBuffer);
					}
				}
				
				for (var m = 0; m < materialCount; ++m) {
					if (readConfig.SkipUnusedMaterials && meshCount > 0) {
						var skip = true;
						for (var i = 0; i < meshMaterialMap.Length; ++i) {
							if (meshMaterialMap[i] == m) {
								skip = false;
								break;
							}
						}
						if (skip) continue;
					}
					
					GetLoadedAssetMaterialData(
						assetHandle,
						m,
						out var colorData,
						out var normalsData,
						out var ormData
					).ThrowIfFailure();
					
					var colorMap = colorData.Format switch {
						AssetMaterialParamDataFormat.Numerical => ((ITextureBuilder) _textureBuilder).CreateColorMap(new ColorVect(colorData.NumericalValueR, colorData.NumericalValueG, colorData.NumericalValueB, colorData.NumericalValueA), includeAlpha: true, config.TextureConfig with { IsLinearColorspace = false }),
						AssetMaterialParamDataFormat.TextureMap => result.Textures[colorData.TextureMapIndex],
						_ => ((ITextureBuilder) _textureBuilder).CreateColorMap(),
					};

					var mat = _materialBuilder.CreateStandardMaterial(
						new StandardMaterialCreationConfig {
							ColorMap = colorMap
						}
					);
					result.Add(mat);
					materialIndexToResultIndexMap[m] = result.Materials.Count - 1;
				}
				
				for (var m = 0; m < meshCount; ++m) {
					GetLoadedAssetMeshVertexCount(assetHandle, m, out var vCount).ThrowIfFailure();
					GetLoadedAssetMeshTriangleCount(assetHandle, m, out var tCount).ThrowIfFailure();
					
					var fixedVertexBuffer = _vertexTriangleBufferPool.Rent<MeshVertex>(vCount);
					var fixedTriangleBuffer = _vertexTriangleBufferPool.Rent<VertexTriangle>(tCount);
					try {
						CopyLoadedAssetMeshVertices(assetHandle, m, fixedVertexBuffer.Size<MeshVertex>(), (MeshVertex*) fixedVertexBuffer.StartPtr).ThrowIfFailure();
						CopyLoadedAssetMeshTriangles(assetHandle, m, fixedTriangleBuffer.Size<VertexTriangle>(), (VertexTriangle*) fixedTriangleBuffer.StartPtr).ThrowIfFailure();
					}
					finally {
						_vertexTriangleBufferPool.Return(fixedVertexBuffer);
						_vertexTriangleBufferPool.Return(fixedTriangleBuffer);
					}

					var mesh = _meshBuilder.CreateMesh(
						fixedVertexBuffer.AsReadOnlySpan<MeshVertex>(vCount),
						fixedTriangleBuffer.AsReadOnlySpan<VertexTriangle>(tCount),
						config.MeshConfig
					);
					
					result.Add(mesh);
					
					var mat = result.Materials[materialIndexToResultIndexMap[meshMaterialMap[m]]];
					var model = CreateModel(mesh, mat, default);
					result.Add(model);
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
		AssetMaterialParamGroup* paramGroupPtr
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