// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Text.RegularExpressions;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Resources;

public readonly unsafe struct ResourceGroup : IStringSpanNameEnabled, IDisposable {
	public const string DefaultGroupName = "Unnamed Group";
	const int DisposalFlagSizeBytes = 1;

	static readonly int SingleResourceSerializedLength = IntPtr.Size + IHandleImplPairResource.SerializedLengthBytes;
	static readonly ArrayPool<byte> _assetBufferPool = ArrayPool<byte>.Shared;
	static readonly ArrayPoolBackedMap<nuint, byte[]> _bufferMap = new();
	static readonly ArrayPoolBackedMap<nuint, Utf16StringBufferPool.Utf16PoolStringHandle> _nameMap = new();
	static nuint _previousGroupId = 0;

	readonly nuint _groupId;

	public int Count {
		get {
			var span = GetBufferForIdOrThrow(_groupId)[DisposalFlagSizeBytes..];
			var result = 0;
			while (span.Length >= SingleResourceSerializedLength && BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
				span = span[SingleResourceSerializedLength..];
				++result;
			}
			return result;
		}
	}

	public int Capacity => (GetBufferForIdOrThrow(_groupId).Length - DisposalFlagSizeBytes) / SingleResourceSerializedLength;

	public string Name {
		get {
			if (!_bufferMap.ContainsKey(_groupId)) {
				if (_groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
				else throw new ObjectDisposedException(nameof(ResourceGroup));
			}

			if (!_nameMap.TryGetValue(_groupId, out var nameHandle)) return DefaultGroupName;
			else return nameHandle.AsNewStringObject;
		}
	}

	public ResourceGroup(int minCapacity, bool disposeAssetsWhenGroupIsDisposed) {
		if (minCapacity <= 0) {
			throw new ArgumentOutOfRangeException(nameof(minCapacity), minCapacity, $"Value must be greater than 0.");
		}
		
		var dataLengthRequired = DisposalFlagSizeBytes + SingleResourceSerializedLength * minCapacity;
		var buffer = _assetBufferPool.Rent(dataLengthRequired);
		buffer[0] = disposeAssetsWhenGroupIsDisposed ? Byte.MaxValue : Byte.MinValue;
		_groupId = ++_previousGroupId;
		_bufferMap.Add(_groupId, buffer);
	}

	public void AddResource<TResource>(TResource resource) where TResource : IHandleImplPairResource {
		var span = GetBufferForIdOrThrow(_groupId)[DisposalFlagSizeBytes..];

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
		var span = GetBufferForIdOrThrow(tuple.GroupId)[DisposalFlagSizeBytes..];

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
		var span = GetBufferForIdOrThrow(tuple.GroupId)[DisposalFlagSizeBytes..];

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
		Dispose(_groupId, buffer, buffer[0] == Byte.MaxValue);
	}
	public void Dispose(bool disposeContainedResources) {
		if (!_bufferMap.ContainsKey(_groupId)) return;
		var buffer = GetBufferForIdOrThrow(_groupId);
		Dispose(_groupId, buffer, disposeContainedResources);
	}
	static void Dispose(nuint groupId, ReadOnlySpan<byte> buffer, bool disposeContainedResources) {
		var span = buffer[DisposalFlagSizeBytes..];

		while (BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
			// TODO remove dependency
			var gcHandle = IHandleImplPairResource.ReadGcHandleFromSerializedResource(span[sizeof(IntPtr)..]);
			if (disposeContainedResources) {
				var impl = (gcHandle.Target as IResourceImplProvider) ?? throw new InvalidOperationException("Unexpected null impl provider.");
				impl.RawHandleDispose(IHandleImplPairResource.ReadHandleFromSerializedResource(span[sizeof(IntPtr)..]));
			}
			gcHandle.Free();
			span = span[SingleResourceSerializedLength..];
		}

		_assetBufferPool.Return(_bufferMap[groupId]);
		_bufferMap.Remove(groupId);
	}

	public int GetNameUsingSpan(Span<char> dest) {
		if (!_bufferMap.ContainsKey(_groupId)) {
			if (_groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
			else throw new ObjectDisposedException(nameof(ResourceGroup));
		}

		if (!_nameMap.TryGetValue(_groupId, out var nameHandle)) {
			DefaultGroupName.CopyTo(dest);
			return DefaultGroupName.Length;
		}
		else {
			nameHandle.AsSpan.CopyTo(dest);
			return nameHandle.Length;
		}
	}

	public int GetNameSpanLength() {
		if (!_bufferMap.ContainsKey(_groupId)) {
			if (_groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
			else throw new ObjectDisposedException(nameof(ResourceGroup));
		}

		if (!_nameMap.TryGetValue(_groupId, out var nameHandle)) return DefaultGroupName.Length;
		else return nameHandle.Length;
	}

	static Span<byte> GetBufferForIdOrThrow(nuint groupId) {
		if (_bufferMap.TryGetValue(groupId, out var result)) return result;
		if (groupId == 0UL) throw InvalidObjectException.InvalidDefault<ResourceGroup>();
		else throw new ObjectDisposedException(nameof(ResourceGroup));
	}
}