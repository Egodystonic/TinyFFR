// Created on 2024-08-19 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System.IO;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Materials.Local;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Meshes.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Local;

sealed unsafe class LocalAssetLoader : IAssetLoader, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly LocalMeshBuilder _meshBuilder;
	readonly LocalMaterialBuilder _materialBuilder;
	readonly InteropStringBuffer _assetFilePathBuffer;
	bool _isDisposed = false;

	public IMeshBuilder MeshBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _meshBuilder;
	public IMaterialBuilder MaterialBuilder => _isDisposed ? throw new ObjectDisposedException(nameof(IAssetLoader)) : _materialBuilder;

	public LocalAssetLoader(LocalFactoryGlobalObjectGroup globals, LocalAssetLoaderConfig config) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(config);

		_globals = globals;
		_meshBuilder = new LocalMeshBuilder(globals);
		_materialBuilder = new LocalMaterialBuilder(globals, config);
		_assetFilePathBuffer = new InteropStringBuffer(config.MaxAssetFilePathLengthChars, addOneForNullTerminator: true);
	}

	public Mesh LoadMesh(in MeshLoadConfig config) {
		config.ThrowIfInvalid();

		_assetFilePathBuffer.ConvertFromUtf16(config.FilePath);
		LoadAssetFileInToMemory(
			in _assetFilePathBuffer.BufferRef,
			config.FixCommonExportErrors,
			config.OptimizeForGpu,
			out var assetHandle
		).ThrowIfFailure();

		try {
			GetLoadedAssetMeshCount(assetHandle, out var meshCount).ThrowIfFailure();
			GetLoadedAssetMaterialCount(assetHandle, out var matCount).ThrowIfFailure();
			GetLoadedAssetTextureCount(assetHandle, out var texCount).ThrowIfFailure();
			Console.WriteLine(meshCount);
			Console.WriteLine(matCount);
			Console.WriteLine(texCount);
			throw new NotImplementedException();
		}
		finally {
			UnloadAssetFileFromMemory(assetHandle).ThrowIfFailure();
		}
	}

	public Texture LoadTexture(in TextureLoadConfig config) {
		config.ThrowIfInvalid();
		
		_assetFilePathBuffer.ConvertFromUtf16(config.FilePath);
		LoadTextureFileInToMemory(
			in _assetFilePathBuffer.BufferRef,
			config.IncludeWAlphaChannel,
			out var width,
			out var height,
			out var texelBuffer
		).ThrowIfFailure();

		try {
			if (width < 0 || height < 0) throw new InvalidOperationException($"Loaded texture had width/height of {width}/{height}.");
			var texelCount = width * height;

			if (config.IncludeWAlphaChannel) {
				return _materialBuilder.CreateTexture(
					new ReadOnlySpan<TexelRgba32>(texelBuffer, texelCount),
					TextureCreationConfig.FromLoadConfig(config, width, height)
				);
			}
			else {
				return _materialBuilder.CreateTexture(
					new ReadOnlySpan<TexelRgb24>(texelBuffer, texelCount),
					TextureCreationConfig.FromLoadConfig(config, width, height)
				);
			}
		}
		finally {
			UnloadTextureFileFromMemory(texelBuffer).ThrowIfFailure();
		}
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_asset_file_in_to_memory")]
	static extern InteropResult LoadAssetFileInToMemory(
		ref readonly byte utf8FileNameBufferPtr,
		InteropBool fixCommonImporterErrors,
		InteropBool optimize,
		out UIntPtr outAssetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "get_loaded_asset_mesh_count")]
	static extern InteropResult GetLoadedAssetMeshCount(
		UIntPtr assetHandle,
		out int outMeshCount
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

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_asset_file_from_memory")]
	static extern InteropResult UnloadAssetFileFromMemory(
		UIntPtr assetHandle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "load_texture_file_in_to_memory")]
	static extern InteropResult LoadTextureFileInToMemory(
		ref readonly byte utf8FileNameBufferPtr,
		InteropBool includeWAlphaChannel,
		out int outWidth,
		out int outHeight,
		out void* outTexelBufferPtr
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "unload_texture_file_from_memory")]
	static extern InteropResult UnloadTextureFileFromMemory(
		void* texelBufferPtr
	);
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Asset Loader [Disposed]" : "TinyFFR Local Asset Loader";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			_assetFilePathBuffer.Dispose();
			_meshBuilder.Dispose();
			_materialBuilder.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() {
		ObjectDisposedException.ThrowIf(_isDisposed, typeof(IAssetLoader));
	}
	#endregion
}