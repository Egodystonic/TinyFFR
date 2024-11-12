// Created on 2024-09-27 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Xml.Linq;
using static Egodystonic.TinyFFR.Resources.ICombinedResourceGroupImplProvider;

namespace Egodystonic.TinyFFR.Factory.Local;

sealed unsafe class LocalCombinedResourceGroupImplProvider : ICombinedResourceGroupImplProvider {
	const string DefaultGroupName = "Unnamed Resource Group";
	const int DisposalFlagSizeBytes = 1;
	const byte DisposalFlagEnabledValue = Byte.MaxValue;
	const byte DisposalFlagDisabledValue = Byte.MinValue;
	static readonly int SingleResourceSerializedLength = IntPtr.Size + IResource.SerializedLengthBytes;

	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
	readonly ArrayPoolBackedMap<CombinedResourceGroupHandle, byte[]> _bufferMap = new();
	readonly ArrayPoolBackedMap<CombinedResourceGroupHandle, ManagedStringPool.RentedStringHandle> _nameMap = new();
	nuint _previousGroupId = 0;

	public LocalCombinedResourceGroupImplProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);

		_globals = globals;
	}

	public CombinedResourceGroup CreateGroup(int minCapacity, bool disposeContainedResourcesWhenDisposed) {
		if (minCapacity < 0) {
			throw new ArgumentOutOfRangeException(nameof(minCapacity), minCapacity, $"Value must be non-negative.");
		}

		var dataLengthRequired = DisposalFlagSizeBytes + SingleResourceSerializedLength * minCapacity;
		var buffer = _bufferPool.Rent(dataLengthRequired);
		buffer[0] = disposeContainedResourcesWhenDisposed ? DisposalFlagEnabledValue : DisposalFlagDisabledValue;
		var handle = new CombinedResourceGroupHandle(++_previousGroupId);
		_bufferMap.Add(handle, buffer);
		
		return new CombinedResourceGroup(handle, this);
	}

	public CombinedResourceGroup CreateGroup(int minCapacity, bool disposeContainedResourcesWhenDisposed, ReadOnlySpan<char> name) {
		var result = CreateGroup(minCapacity, disposeContainedResourcesWhenDisposed);
		_nameMap.Add(result.Handle, _globals.StringPool.RentAndCopy(name));
		return result;
	}

	public int GetResourceCount(CombinedResourceGroupHandle handle) {
		var span = GetSpanForHandleOrThrow(handle)[DisposalFlagSizeBytes..];

		var result = 0;
		while (span.Length >= SingleResourceSerializedLength && BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
			span = span[SingleResourceSerializedLength..];
			++result;
		}
		return result;
	}

	public int GetResourceCapacity(CombinedResourceGroupHandle handle) => (GetSpanForHandleOrThrow(handle).Length - DisposalFlagSizeBytes) / SingleResourceSerializedLength;

	public void AddResource<TResource>(CombinedResourceGroupHandle handle, TResource resource) where TResource : IResource {
		var span = GetSpanForHandleOrThrow(handle)[DisposalFlagSizeBytes..];

		_globals.DependencyTracker.RegisterDependency(new CombinedResourceGroup(handle, this), resource);

		while (BinaryPrimitives.ReadIntPtrLittleEndian(span) != IntPtr.Zero) {
			span = span[SingleResourceSerializedLength..];
			if (span.Length < SingleResourceSerializedLength) {
				throw new InvalidOperationException($"Can not add resource '{resource}' to resource group '{GetName(handle)}': Group is already full.");
			}
		}

		BinaryPrimitives.TryWriteIntPtrLittleEndian(span, typeof(TResource).TypeHandle.Value);
		resource.AllocateGcHandleAndSerializeResource(span[sizeof(IntPtr)..]);
	}

	public OneToManyEnumerator<EnumerationArg, TResource> GetAllResourcesOfType<TResource>(CombinedResourceGroupHandle handle) where TResource : IResource<TResource> {
		ThrowIfHandleIsDisposed(handle);

		return new OneToManyEnumerator<EnumerationArg, TResource>(
			new(this, handle, typeof(TResource).TypeHandle.Value),
			&GetEnumeratorResourceCount,
			&GetEnumeratorResourceAtIndex<TResource>
		);
	}
	static int GetEnumeratorResourceCount(EnumerationArg arg) {
		var implProvider = (arg.Impl as LocalCombinedResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalCombinedResourceGroupImplProvider)}.");
		var span = implProvider.GetSpanForHandleOrThrow(arg.Handle)[DisposalFlagSizeBytes..];

		var result = 0;
		while (span.Length >= SingleResourceSerializedLength) {
			var typeId = BinaryPrimitives.ReadIntPtrLittleEndian(span);
			if (typeId == IntPtr.Zero) return result;
			else if (typeId == arg.ResourceTypeHandle) ++result;

			span = span[SingleResourceSerializedLength..];
		}
		return result;
	}
	static TResource GetEnumeratorResourceAtIndex<TResource>(EnumerationArg arg, int index) where TResource : IResource<TResource> {
		var implProvider = (arg.Impl as LocalCombinedResourceGroupImplProvider) ?? throw new InvalidOperationException($"Expected impl provider to be of type {nameof(LocalCombinedResourceGroupImplProvider)}.");
		var span = implProvider.GetSpanForHandleOrThrow(arg.Handle)[DisposalFlagSizeBytes..];

		var count = 0;
		while (span.Length >= SingleResourceSerializedLength) {
			var typeId = BinaryPrimitives.ReadIntPtrLittleEndian(span);
			if (typeId == IntPtr.Zero) break;

			if (typeId == arg.ResourceTypeHandle) {
				if (count == index) {
					span = span[sizeof(IntPtr)..];
					return TResource.RecreateFromRawHandleAndImpl(
						IResource.ReadHandleFromSerializedResource(span),
						(IResource.ReadGcHandleFromSerializedResource(span).Target as IResourceImplProvider) ?? throw new InvalidOperationException("Unexpected null impl provider.")
					);
				}
				count++;
			}

			span = span[SingleResourceSerializedLength..];
		}

		throw new IndexOutOfRangeException($"Index '{index}' is out of range for resources of type '{typeof(TResource).Name}' in this resource group (actual count = {count}).");
	}

	public ReadOnlySpan<char> GetName(CombinedResourceGroupHandle handle) {
		ThrowIfHandleIsDisposed(handle);

		if (!_nameMap.TryGetValue(handle, out var nameHandle)) return DefaultGroupName;
		else return nameHandle.AsSpan;
	}

	public bool IsDisposed(CombinedResourceGroupHandle handle) => !_bufferMap.ContainsKey(handle.AsInteger);
	public void Dispose(CombinedResourceGroupHandle handle) {
		if (!_bufferMap.TryGetValue(handle, out var buffer)) return;
		Dispose(handle, buffer, buffer[0] == DisposalFlagEnabledValue);
	}
	public void Dispose(CombinedResourceGroupHandle handle, bool disposeContainedResources) {
		if (!_bufferMap.TryGetValue(handle, out var buffer)) return;
		Dispose(handle, buffer, disposeContainedResources);
	}
	void Dispose(CombinedResourceGroupHandle handle, byte[] buffer, bool disposeContainedResources) {
		var span = buffer.AsSpan()[DisposalFlagSizeBytes..];

		nint typeHandle;
		while ((typeHandle = BinaryPrimitives.ReadIntPtrLittleEndian(span)) != IntPtr.Zero) {
			var gcHandle = IResource.ReadGcHandleFromSerializedResource(span[sizeof(IntPtr)..]);
			var rawHandle = IResource.ReadHandleFromSerializedResource(span[sizeof(IntPtr)..]);
			var impl = (gcHandle.Target as IResourceImplProvider) ?? throw new InvalidOperationException("Unexpected null impl provider.");
			var stub = new ResourceStub(new(typeHandle, rawHandle), impl);

			_globals.DependencyTracker.DeregisterDependency(new CombinedResourceGroup(handle, this), stub);
			if (disposeContainedResources) stub.Dispose();
			gcHandle.Free();

			span = span[SingleResourceSerializedLength..];
		}

		_bufferMap.Remove(handle);
		_nameMap.Remove(handle);
		_bufferPool.Return(buffer);
	}

	void ThrowIfHandleIsDisposed(CombinedResourceGroupHandle handle) {
		if (_bufferMap.ContainsKey(handle)) return;
		
		if (handle == default) throw InvalidObjectException.InvalidDefault<CombinedResourceGroup>();
		else throw new ObjectDisposedException(nameof(CombinedResourceGroup));
	}

	Span<byte> GetSpanForHandleOrThrow(CombinedResourceGroupHandle handle) {
		if (_bufferMap.TryGetValue(handle, out var result)) return result;

		if (handle == 0UL) throw InvalidObjectException.InvalidDefault<CombinedResourceGroup>();
		else throw new ObjectDisposedException(nameof(CombinedResourceGroup));
	}
}