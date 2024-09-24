// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers.Binary;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct ResourceGroup : IDisposable {
	public const int MaxAssetsPerGroup = 100;

	const int DisposeAssetsFlagIndex = 0;
	const int MeshCountIndex = DisposeAssetsFlagIndex + sizeof(int);
	const int TextureCountIndex = MeshCountIndex + sizeof(int);
	const int MaterialCountIndex = TextureCountIndex + sizeof(int);
	const int DataBlockStartIndex = MaterialCountIndex + sizeof(int);

	static int GetMeshObjectStartIndex(int meshIndex) => DataBlockStartIndex + IHandleImplPairResource.SerializedLengthBytes * meshIndex;
	static int GetTextureObjectStartIndex(int meshCount, int textureIndex) => GetMeshObjectStartIndex(meshCount) + IHandleImplPairResource.SerializedLengthBytes * textureIndex;
	static int GetMaterialObjectStartIndex(int meshCount, int textureCount, int materialIndex) => GetTextureObjectStartIndex(meshCount, textureCount) + IHandleImplPairResource.SerializedLengthBytes * materialIndex;

	static readonly FixedByteBufferPool _assetBufferPool = new((LargestHandleSizeBytes + sizeof(GCHandle)) * MaxAssetsPerGroup);
	static readonly ArrayPoolBackedMap<ulong, FixedByteBufferPool.FixedByteBuffer> _bufferMap = new();
	static ulong _previousGroupId = 0UL;

	readonly ulong _groupId;

	public OneToManyEnumerator<ulong, Mesh> Meshes => new(_groupId, &GetGroupMeshCount, &GetGroupMeshAtIndex);
	public OneToManyEnumerator<ulong, Texture> Textures => new(_groupId, &GetGroupTextureCount, &GetGroupTextureAtIndex);
	public OneToManyEnumerator<ulong, Material> Materials => new(_groupId, &GetGroupMaterialCount, &GetGroupMaterialAtIndex);

	public ResourceGroup(
		bool disposeAssetsWhenGroupIsDisposed,
		ReadOnlySpan<Mesh> meshes, 
		ReadOnlySpan<Texture> textures, 
		ReadOnlySpan<Material> materials) {
		var dataLengthRequired = DataBlockStartIndex
								 + MeshObjectSizeBytes * meshes.Length
								 + TextureObjectSizeBytes * textures.Length
								 + MaterialObjectSizeBytes * materials.Length;
		
		_groupId = ++_previousGroupId;
		var buffer = _assetBufferPool.Rent(dataLengthRequired);
		var span = buffer.AsByteSpan;

		BinaryPrimitives.WriteInt32LittleEndian(span[MeshCountIndex..], meshes.Length);
		BinaryPrimitives.WriteInt32LittleEndian(span[TextureCountIndex..], textures.Length);
		BinaryPrimitives.WriteInt32LittleEndian(span[MaterialCountIndex..], materials.Length);
		span = span[DataBlockStartIndex..];

		for (var i = 0; i < meshes.Length; ++i) {
			var gcHandle = GCHandle.Alloc(meshes[i].Implementation, GCHandleType.Normal);
			var assetHandle = meshes[i].Handle;
			MemoryMarshal.Write(span, gcHandle);
			span = span[sizeof(GCHandle)..];
			MemoryMarshal.Write(span, assetHandle);
			span = span[sizeof(MeshHandle)..];
		}

		for (var i = 0; i < textures.Length; ++i) {
			var gcHandle = GCHandle.Alloc(textures[i].Implementation, GCHandleType.Normal);
			var assetHandle = textures[i].Handle;
			MemoryMarshal.Write(span, gcHandle);
			span = span[sizeof(GCHandle)..];
			MemoryMarshal.Write(span, assetHandle);
			span = span[sizeof(TextureHandle)..];
		}

		for (var i = 0; i < materials.Length; ++i) {
			var gcHandle = GCHandle.Alloc(materials[i].Implementation, GCHandleType.Normal);
			var assetHandle = materials[i].Handle;
			MemoryMarshal.Write(span, gcHandle);
			span = span[sizeof(GCHandle)..];
			MemoryMarshal.Write(span, assetHandle);
			span = span[sizeof(MaterialHandle)..];
		}

		_bufferMap.Add(_groupId, buffer);
	}

	public void Dispose() {
		 // TODO
	}
	public void Dispose(bool disposeContainedAssets) { // TODO
		if (!_bufferMap.ContainsKey(_groupId)) return;
		var buffer = GetBufferForIdOrThrow(_groupId);

		var meshCount = GetCount(buffer, MeshCountIndex);
		var textureCount = GetCount(buffer, TextureCountIndex);
		var materialCount = GetCount(buffer, MaterialCountIndex);

		for (var i = 0; i < meshCount; ++i) {
			var tuple = GetItemTuple<MeshHandle>(buffer, GetMeshObjectStartIndex(i));
			tuple.ImplHandle.Free();
		}
		for (var i = 0; i < textureCount; ++i) {
			var tuple = GetItemTuple<TextureHandle>(buffer, GetTextureObjectStartIndex(meshCount, i));
			tuple.ImplHandle.Free();
		}
		for (var i = 0; i < materialCount; ++i) {
			var tuple = GetItemTuple<MaterialHandle>(buffer, GetMaterialObjectStartIndex(meshCount, textureCount, i));
			tuple.ImplHandle.Free();
		}

		_assetBufferPool.Return(buffer);
		_bufferMap.Remove(_groupId);
	}

	static int GetGroupMeshCount(ulong groupId) => GetCount(groupId, MeshCountIndex);
	static Mesh GetGroupMeshAtIndex(ulong groupId, int index) {
		var itemTuple = GetItemTuple<MeshHandle, IMeshAssetImplProvider>(groupId, GetMeshObjectStartIndex(index));
		return new(itemTuple.Handle, itemTuple.Impl);
	}

	static int GetGroupTextureCount(ulong groupId) => GetCount(groupId, TextureCountIndex);
	static Texture GetGroupTextureAtIndex(ulong groupId, int index) {
		var itemTuple = GetItemTuple<TextureHandle, ITextureAssetImplProvider>(groupId, GetTextureObjectStartIndex(GetGroupMeshCount(groupId), index));
		return new(itemTuple.Handle, itemTuple.Impl);
	}

	static int GetGroupMaterialCount(ulong groupId) => GetCount(groupId, MaterialCountIndex);
	static Material GetGroupMaterialAtIndex(ulong groupId, int index) {
		var itemTuple = GetItemTuple<MaterialHandle, IMaterialAssetImplProvider>(groupId, GetMaterialObjectStartIndex(GetGroupMeshCount(groupId), GetGroupTextureCount(groupId), index));
		return new(itemTuple.Handle, itemTuple.Impl);
	}

	static FixedByteBufferPool.FixedByteBuffer GetBufferForIdOrThrow(ulong groupId) {
		if (_bufferMap.TryGetValue(groupId, out var result)) return result;
		if (groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
		else throw new ObjectDisposedException(nameof(ResourceGroup));
	}

	static int GetCount(ulong groupId, int countIndex) => GetCount(GetBufferForIdOrThrow(groupId), countIndex);
	static int GetCount(FixedByteBufferPool.FixedByteBuffer buffer, int countIndex) {
		return BinaryPrimitives.ReadInt32LittleEndian(buffer.AsReadOnlyByteSpan[countIndex..(countIndex + sizeof(int))]);
	}

	static (THandle Handle, GCHandle ImplHandle) GetItemTuple<THandle>(ulong groupId, int startByte) where THandle : unmanaged => GetItemTuple<THandle>(GetBufferForIdOrThrow(groupId), startByte);
	static (THandle Handle, GCHandle ImplHandle) GetItemTuple<THandle>(FixedByteBufferPool.FixedByteBuffer buffer, int startByte) where THandle : unmanaged {
		var span = buffer.AsReadOnlyByteSpan;
		var handle = MemoryMarshal.Read<THandle>(span[startByte..]);
		var implHandle = MemoryMarshal.Read<GCHandle>(span[(startByte + sizeof(THandle))..]);
		return (handle, implHandle);
	}
	static (THandle Handle, TImpl Impl) GetItemTuple<THandle, TImpl>(ulong groupId, int startByte) where THandle : unmanaged where TImpl : class => GetItemTuple<THandle, TImpl>(GetBufferForIdOrThrow(groupId), startByte);
	static (THandle Handle, TImpl Impl) GetItemTuple<THandle, TImpl>(FixedByteBufferPool.FixedByteBuffer buffer, int startByte) where THandle : unmanaged where TImpl : class {
		var nonCastTuple = GetItemTuple<THandle>(buffer, startByte);
		return (nonCastTuple.Handle, (nonCastTuple.ImplHandle as TImpl) ?? throw new InvalidOperationException($"Unexpected null implementation (this may indicate use of an {nameof(ResourceGroup)} after disposal)."));
	}
}