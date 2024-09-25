// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct ResourceGroup : IDisposable {
	public const int MaxResourcesPerGroup = 100;
	const int DisposalFlagSizeBytes = 1;

	static readonly int SingleResourceSerializedLength = IntPtr.Size + IHandleImplPairResource.SerializedLengthBytes;
	static readonly FixedByteBufferPool _assetBufferPool = new(SingleResourceSerializedLength * MaxResourcesPerGroup + DisposalFlagSizeBytes);
	static readonly ArrayPoolBackedMap<nuint, FixedByteBufferPool.FixedByteBuffer> _bufferMap = new();
	static nuint _previousGroupId = 0;

	readonly nuint _groupId;

	public int Count {
		get {
			var span = GetBufferForIdOrThrow(_groupId).AsReadOnlyByteSpan[DisposalFlagSizeBytes..];
			var result = 0;
			while (span.Length >= SingleResourceSerializedLength && BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
				span = span[SingleResourceSerializedLength..];
				++result;
			}
			return result;
		}
	}

	public int Capacity => (GetBufferForIdOrThrow(_groupId).SizeBytes - DisposalFlagSizeBytes) / SingleResourceSerializedLength;

	public ResourceGroup(int minCapacity, bool disposeAssetsWhenGroupIsDisposed) {
		if (minCapacity <= 0 || minCapacity > MaxResourcesPerGroup) {
			throw new ArgumentOutOfRangeException(nameof(minCapacity), minCapacity, $"Value must be greater than 0 and less than or equal to {nameof(MaxResourcesPerGroup)} ({MaxResourcesPerGroup}).");
		}
		
		var dataLengthRequired = DisposalFlagSizeBytes + SingleResourceSerializedLength * minCapacity;
		var buffer = _assetBufferPool.Rent(dataLengthRequired);
		buffer.AsByteSpan[0] = disposeAssetsWhenGroupIsDisposed ? Byte.MaxValue : Byte.MinValue;
		_groupId = ++_previousGroupId;
		_bufferMap.Add(_groupId, buffer);
	}

	public void AddResource<TResource>(TResource resource) where TResource : IHandleImplPairResource {
		var span = GetBufferForIdOrThrow(_groupId).AsByteSpan[DisposalFlagSizeBytes..];

		// TODO dependency

		while (BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
			span = span[SingleResourceSerializedLength..];
			if (span.Length < SingleResourceSerializedLength) {
				throw new InvalidOperationException($"Can not add resource '{resource}' to resource group '{Name}': Group is already full.");
			}
		}

		BinaryPrimitives.TryWriteIntPtrLittleEndian(span, typeof(TResource).TypeHandle.Value);
		resource.AllocateGcHandleAndSerializeResource(span[sizeof(IntPtr)..]);
	}

	public OneToManyEnumerator<(nuint, IntPtr), TResource> GetAllResourcesOfType<TResource>() where TResource : IHandleImplPairResource<TResource> {
		return new OneToManyEnumerator<(nuint, IntPtr), TResource>(
			(_groupId, typeof(TResource).TypeHandle.Value),
			&GetResourceCount,
			&GetResourceAtIndex<TResource>
		);
	}
	static int GetResourceCount((nuint GroupId, IntPtr TypeId) tuple) {
		var span = GetBufferForIdOrThrow(tuple.GroupId).AsByteSpan[DisposalFlagSizeBytes..];

		var result = 0;
		while (span.Length >= SingleResourceSerializedLength) {
			var typeId = BinaryPrimitives.ReadIntPtrLittleEndian(span);
			if (typeId == IntPtr.Zero) return result;
			else if (typeId == tuple.TypeId) ++result;

			span = span[SingleResourceSerializedLength..];
		}
		return result;
	}
	static TResource GetResourceAtIndex<TResource>((nuint GroupId, IntPtr TypeId) tuple, int index) where TResource : IHandleImplPairResource<TResource> {
		var span = GetBufferForIdOrThrow(tuple.GroupId).AsByteSpan[DisposalFlagSizeBytes..];

		var count = 0;
		while (span.Length >= SingleResourceSerializedLength) {
			var typeId = BinaryPrimitives.ReadIntPtrLittleEndian(span);
			if (typeId == IntPtr.Zero) break;
			
			if (typeId == tuple.TypeId) {
				if (count == index) {
					span = span[sizeof(IntPtr)..];
					return TResource.RecreateFromRawHandleAndImpl(
						IHandleImplPairResource.ReadHandleFromSerializedResource(span),
						(IHandleImplPairResource.ReadGcHandleFromSerializedResource(span).Target as IResourceImplProvider) ?? throw new InvalidOperationException("Unexpected null impl provider.")
					);
				}
				count++;
			}

			span = span[SingleResourceSerializedLength..];
		}

		throw new IndexOutOfRangeException($"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}

	public void Dispose() {
		if (!_bufferMap.ContainsKey(_groupId)) return;
		var buffer = GetBufferForIdOrThrow(_groupId);
		Dispose(_groupId, buffer, buffer.AsReadOnlyByteSpan[0] == Byte.MaxValue);
	}
	public void Dispose(bool disposeContainedResources) {
		if (!_bufferMap.ContainsKey(_groupId)) return;
		var buffer = GetBufferForIdOrThrow(_groupId);
		Dispose(_groupId, buffer, disposeContainedResources);
	}
	static void Dispose(nuint groupId, FixedByteBufferPool.FixedByteBuffer buffer, bool disposeContainedResources) {
		var span = buffer.AsReadOnlyByteSpan[DisposalFlagSizeBytes..];

		while (BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
			// TODO remove dependency
			var gcHandle = IHandleImplPairResource.ReadGcHandleFromSerializedResource(span[sizeof(IntPtr)..]);
			if (disposeContainedResources) {
				var impl = (gcHandle.Target as IResourceImplProvider) ?? throw new InvalidOperationException("Unexpected null impl provider.");
				impl.DisposeViaRawHandle(IHandleImplPairResource.ReadHandleFromSerializedResource(span[sizeof(IntPtr)..]));
			}
			gcHandle.Free();
			span = span[SingleResourceSerializedLength..];
		}

		_assetBufferPool.Return(buffer);
		_bufferMap.Remove(groupId);
	}

	static FixedByteBufferPool.FixedByteBuffer GetBufferForIdOrThrow(nuint groupId) {
		if (_bufferMap.TryGetValue(groupId, out var result)) return result;
		if (groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
		else throw new ObjectDisposedException(nameof(ResourceGroup));
	}
}