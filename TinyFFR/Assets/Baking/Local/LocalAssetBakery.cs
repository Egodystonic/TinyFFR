// Created on 2026-08-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Assets.Text;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;
using Egodystonic.TinyFFR.Threading;

namespace Egodystonic.TinyFFR.Assets.Baking;

static class BakedValueTypeConverter<T> where T : unmanaged {
	public static readonly Type Converted = typeof(T).IsEnum ? typeof(T).GetEnumUnderlyingType() : typeof(T);
}

sealed unsafe class LocalAssetBakery : IAssetBakery, IDisposable {
	public const string ResourceNameSectionName = "resource_name";
	const int InitialResourceMemoryAllocationSize = 1024;
	
	static ReadOnlySpan<byte> BakedAssetStreamStartString => "_TinyFFR"u8; // 8 bytes long for alignment
	const int FileHeaderLengthBytes = 20; // BakedAssetStreamStartString.Length + two version numbers + type ID
	static ReadOnlySpan<byte> SectionStartString => "SectionStart"u8;
	
	// Maintainer's note: Changing the order of these is a schema major version change
	// Adding new types on to the end is a minor change
	static readonly Type[] _typeIdToTypeMap = new[] {
		typeof(byte[]),
		typeof(byte),
		typeof(ushort),
		typeof(uint),
		typeof(ulong),
		typeof(sbyte),
		typeof(short),
		typeof(int),
		typeof(long),
		typeof(float),
		typeof(double),
		typeof(bool),
		typeof(char),
		typeof(TimeSpan),
		typeof(string),
		typeof(BackdropTexture),
		typeof(Font),
		typeof(Material),
		typeof(Mesh),
		typeof(MeshAnimation),
		typeof(MeshNode),
		typeof(Model),
		typeof(Texture),
		typeof(Matrix4x4),
		typeof(PositionedCuboid)
	};
#pragma warning disable CA1859 // "Don't use readonlydict for increased performance" -- I don't need perf, I need the intentionality of read-only; that's why I chose that interface :/
	static readonly IReadOnlyDictionary<Type, int> _typeToTypeIdMap = _typeIdToTypeMap.Select((t, i) => (Type: t, Id: i)).ToDictionary(tuple => tuple.Type, tuple => tuple.Id);
#pragma warning restore CA1859
	
	readonly record struct BakedData(PooledHeapMemory<byte> Buffer, int CursorPos);
	readonly record struct PendingFinalizer(object Invoker, UIntPtr Func);
	public readonly struct AssetLoadConfig : IConfigStruct<AssetLoadConfig> {
		public void* Callback { get; init; }
		
		public static int GetHeapStorageFormattedLength(in AssetLoadConfig src) => UIntPtr.Size;
		public static void AllocateAndConvertToHeapStorage(Span<byte> dest, in AssetLoadConfig src) { BinaryPrimitives.WriteUIntPtrLittleEndian(dest, (UIntPtr) src.Callback); }
		public static AssetLoadConfig ConvertFromAllocatedHeapStorage(ReadOnlySpan<byte> src) => new() { Callback = (void*) BinaryPrimitives.ReadUIntPtrLittleEndian(src) };
		public static void DisposeAllocatedHeapStorage(ReadOnlySpan<byte> src) { }
	}
	
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly AssetBakeryConfig _config;
	readonly ArrayPoolBackedMap<ResourceStub, BakedData> _inProgressResources = new();
	readonly ArrayPoolBackedMap<ResourceStub, PendingFinalizer> _pendingFinalizers = new();
	readonly ArrayPoolBackedLruCache<ResourceStub, BakedData> _resourcesReadyForBaking;
	readonly ArrayPoolBackedObjectPool<LoadedBakedAsset, LocalAssetBakery> _loadedAssetPool;
	readonly WorkerJobSyncHelper<LocalAssetBakery, AssetLoadContext, AssetLoadConfig> _loadSyncHelper;
	readonly Lock _loadedAssetPoolLock = new();
	bool _isDisposed = false;

	public bool Enabled {
		get; 
		set {
			if (field == value) return;
			if (value) ThrowIfThisIsDisposed();
			field = value;
			if (!value) {
				ClearBakeryMemory();
			}
		}
	}
	
	public sealed class AssetLoadContext : WorkerJobSyncHelper<LocalAssetBakery, AssetLoadContext, AssetLoadConfig>.WorkerJobSyncHelperContext {
		public object This { get; set; } = null!;
		public LoadedBakedAsset AssetData { get; set; } = null!;
		public PooledHeapMemory<char>? AssetFilePath { get; set; } = null;
		
		public TThis Invoker<TThis>() where TThis : class => (TThis) This;
		public ReadOnlySpan<char> StoredOrOverridingName {
			get {
				if (!Name.IsEmpty) return Name;
				return AssetData.ExtractString(ResourceNameSectionName, Name);
			}
		}
		
		public override void TearDown() {
			AssetData?.Dispose();
			AssetData = null!;
			AssetFilePath?.Dispose();
			AssetFilePath = null;
			This = null!;
		}
		
		public override void Dispose() {
			/* no-op */
		}
	}

	public LocalAssetBakery(LocalFactoryGlobalObjectGroup globals, AssetBakeryConfig config) {
		_globals = globals;
		_config = config;
		_resourcesReadyForBaking = new(_config.MaxResourcesInBakeryMemory, &CacheEvictionCallback, this);
		lock (_loadedAssetPoolLock) {
			_loadedAssetPool = new(&CreateNewLoadedAssetObject, this);
		}
		_loadSyncHelper = new(this, _globals.HeapPool.ThreadSafeWrapper, _globals.PrimaryThreadDispatcher, _globals.SynchronousWorkScheduler, _globals.ThreadPoolWorkScheduler);
		Enabled = config.Enabled;
	}

	#region Bake
	public void StartResourceBake<TResource>(TResource resource) where TResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		var buffer = _globals.HeapPool.Borrow(InitialResourceMemoryAllocationSize);
		var cursor = 0;
		
		BakedAssetStreamStartString.CopyTo(buffer.Span);
		cursor += BakedAssetStreamStartString.Length;
		
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Span[cursor..], BakedResourceSchemata.VersionMajor);
		cursor += sizeof(int);
		
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Span[cursor..], BakedResourceSchemata.VersionMinor);
		cursor += sizeof(int);
		
		if (!_typeToTypeIdMap.TryGetValue(typeof(TResource), out var typeId)) {
			throw new InvalidOperationException($"Could not bake resource {resource} as it is of unsupported type {typeof(TResource).Name} (this is a bug in TinyFFR).");
		}
		BinaryPrimitives.WriteInt32LittleEndian(buffer.Span[cursor..], typeId);
		cursor += sizeof(int);
		
		Debug.Assert(cursor == FileHeaderLengthBytes);
		_inProgressResources.Add(resource.AsStub, new(buffer, cursor));
	}
	
	public void AddResourceBakeValue<TResource, T>(TResource resource, ReadOnlySpan<char> sectionName, T value) where TResource : IResource where T : unmanaged {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		
		var dataLengthBytes = Unsafe.SizeOf<T>();
		var tuple = WriteSectionHeaderAndEnsureSpaceForData(resource.AsStub, sectionName, BakedValueTypeConverter<T>.Converted, dataLengthBytes);
		
		fixed (byte* bufPtr = tuple.Buffer.Span) {
			Unsafe.WriteUnaligned(bufPtr + tuple.CursorPos, value);
		}
		_inProgressResources[resource.AsStub] = tuple with { CursorPos = tuple.CursorPos + dataLengthBytes };
	}
	
	public void AddResourceBakeSubResource<TResource, TSubResource>(TResource resource, ReadOnlySpan<char> sectionName, TSubResource value) where TResource : IResource where TSubResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		
		if (!_resourcesReadyForBaking.TryGet(value.AsStub, out var bakeData)) {
			throw new AssetBakeException(
				$"Can not prepare {resource} for bake because {nameof(AssetBakeryConfig.MaxResourcesInBakeryMemory)} " +
				$"in the given {nameof(AssetBakeryConfig)} is too low to accomodate all sub-resources. Increase this value to accomodate baking " +
				$"larger resources (or resources with many sub-assets)."
			);
		}
		
		var dataLengthBytes = bakeData.CursorPos - FileHeaderLengthBytes;
		var tuple = WriteSectionHeaderAndEnsureSpaceForData(resource.AsStub, sectionName, typeof(TSubResource), dataLengthBytes);
		
		bakeData.Buffer.Span[FileHeaderLengthBytes..bakeData.CursorPos].CopyTo(tuple.Buffer.Span[tuple.CursorPos..]);
		_inProgressResources[resource.AsStub] = tuple with { CursorPos = tuple.CursorPos + dataLengthBytes };
	}
	
	public void AddResourceBakeValue<TResource>(TResource resource, ReadOnlySpan<char> sectionName, ReadOnlySpan<byte> data) where TResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		
		var tuple = WriteSectionHeaderAndEnsureSpaceForData(resource.AsStub, sectionName, typeof(byte[]), data.Length);
		
		data.CopyTo(tuple.Buffer.Span[tuple.CursorPos..]);
		_inProgressResources[resource.AsStub] = tuple with { CursorPos = tuple.CursorPos + data.Length };
	}
	
	public void AddResourceBakeValue<TResource>(TResource resource, ReadOnlySpan<char> sectionName, ReadOnlySpan<char> str) where TResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		
		var asBytes = MemoryMarshal.AsBytes(str);
		var tuple = WriteSectionHeaderAndEnsureSpaceForData(resource.AsStub, sectionName, typeof(string), asBytes.Length);
		
		asBytes.CopyTo(tuple.Buffer.Span[tuple.CursorPos..]);
		_inProgressResources[resource.AsStub] = tuple with { CursorPos = tuple.CursorPos + asBytes.Length };
	}
	
	BakedData WriteSectionHeaderAndEnsureSpaceForData(ResourceStub stub, ReadOnlySpan<char> sectionName, Type type, int dataLengthBytes) {
		if (sectionName.IsEmpty) {
			throw new ArgumentException("Section name may not be empty (this is a bug in TinyFFR).", nameof(sectionName));
		}
		if (!_typeToTypeIdMap.TryGetValue(type, out var typeId)) {
			throw new InvalidOperationException($"Could not bake resource {stub.GetNameAsNewStringObject()} as sub-resource or value '{sectionName}' is of unsupported type {type.Name} (this is a bug in TinyFFR).");
		}
		var tuple = _inProgressResources[stub];
		var headerLengthBytes = SectionStartString.Length + sizeof(int) + Encoding.UTF8.GetByteCount(sectionName) + sizeof(int) * 2;
		var buffer = EnsureSpace(stub, tuple, headerLengthBytes + dataLengthBytes);
		var headerSpan = buffer.Span.Slice(tuple.CursorPos, headerLengthBytes);
		SectionStartString.CopyTo(headerSpan);
		var sectionNameByteCount = Encoding.UTF8.GetBytes(sectionName, headerSpan[(SectionStartString.Length + sizeof(int))..]);
		BinaryPrimitives.WriteInt32LittleEndian(headerSpan[(SectionStartString.Length)..], sectionNameByteCount);
		BinaryPrimitives.WriteInt32LittleEndian(headerSpan[(SectionStartString.Length + sizeof(int) + sectionNameByteCount)..], typeId);
		BinaryPrimitives.WriteInt32LittleEndian(headerSpan[(SectionStartString.Length + sizeof(int) + sectionNameByteCount + sizeof(int))..], dataLengthBytes);
		
		return _inProgressResources[stub] = new(buffer, tuple.CursorPos + headerLengthBytes);
	}
	
	PooledHeapMemory<byte> EnsureSpace(ResourceStub stub, BakedData tuple, int requiredLength) {
		if (tuple.Buffer.Span.Length - tuple.CursorPos >= requiredLength) return tuple.Buffer;
		var newBuffer = _globals.HeapPool.Borrow(Int32.Max(tuple.Buffer.Span.Length * 2, tuple.CursorPos + requiredLength));
		_inProgressResources[stub] = new(newBuffer, tuple.CursorPos);
		tuple.Buffer.Span.CopyTo(newBuffer.Span);
		tuple.Buffer.Dispose();
		return newBuffer;
	}
	
	public void CompleteResourceBake<TResource>(TResource resource) where TResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		_inProgressResources.Remove(resource.AsStub, out var tuple);
		_resourcesReadyForBaking.AddOrSet(resource.AsStub, tuple);
	}

	public void CompleteResourceBakePendingFinalization<TResource>(TResource resource, object invoker, delegate* managed<object, LocalAssetBakery, TResource, void> finalizer) where TResource : IResource {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		CompleteResourceBake(resource);
		_pendingFinalizers[resource.AsStub] = new PendingFinalizer(invoker, (UIntPtr) finalizer);
	}

	public void DiscardBakeryDataIfPresent<TResource>(TResource resource) where TResource : IResource {
		if (!Enabled) return;
		if (_resourcesReadyForBaking.Remove(resource.AsStub, out var bakeData)) bakeData.Buffer.Dispose();
		_pendingFinalizers.Remove(resource.AsStub);
	}

	public void Bake(BackdropTexture resource, ReadOnlySpan<char> filePath) => Bake<BackdropTexture>(resource, filePath);
	public void Bake(Font resource, ReadOnlySpan<char> filePath) => Bake<Font>(resource, filePath);
	public void Bake(Material resource, ReadOnlySpan<char> filePath) => Bake<Material>(resource, filePath);
	public void Bake(Mesh resource, ReadOnlySpan<char> filePath) => Bake<Mesh>(resource, filePath);
	public void Bake(Model resource, ReadOnlySpan<char> filePath) => Bake<Model>(resource, filePath);
	public void Bake(Texture resource, ReadOnlySpan<char> filePath) => Bake<Texture>(resource, filePath);

	void Bake<TResource>(TResource resource, ReadOnlySpan<char> filePath) where TResource : IResource<TResource> {
		ThreadSafetyTracker.AssertCurrentThreadIsPrimary();
		ThrowIfThisIsDisposedOrDisabled();
		if (!_resourcesReadyForBaking.TryGet(resource.AsStub, out var bakeData)) {
			throw new AssetBakeException(
				$"Resource was not created when this asset bakery was {nameof(Enabled)} OR " +
				$"was created too long ago (in which case try increasing " +
				$"{nameof(AssetBakeryConfig)}.{nameof(AssetBakeryConfig.MaxResourcesInBakeryMemory)})."
			);
		}

		if (!_pendingFinalizers.TryGetValue(resource.AsStub, out var finalizer)) {
			File.WriteAllBytes(filePath.ToString(), bakeData.Buffer.Span[..bakeData.CursorPos]);
			return;
		}

		var workingBuffer = _globals.HeapPool.Borrow(bakeData.CursorPos);
		bakeData.Buffer.Span[..bakeData.CursorPos].CopyTo(workingBuffer.Span);
		_inProgressResources.Add(resource.AsStub, new BakedData(workingBuffer, bakeData.CursorPos));
		try {
			((delegate* managed<object, LocalAssetBakery, TResource, void>) finalizer.Func.ToPointer())(finalizer.Invoker, this, resource);
			var finalizedData = _inProgressResources[resource.AsStub];
			File.WriteAllBytes(filePath.ToString(), finalizedData.Buffer.Span[..finalizedData.CursorPos]);
		}
		finally {
			if (_inProgressResources.Remove(resource.AsStub, out var finalData)) finalData.Buffer.Dispose();
		}
	}
	#endregion
	
	#region Load
	static LoadedBakedAsset CreateNewLoadedAssetObject(LocalAssetBakery @this) => new(@this, &ReturnLoadedAssetObject);
	static void ReturnLoadedAssetObject(LocalAssetBakery @this, LoadedBakedAsset asset) {
		if (!asset.IsRootAsset) return;
		var stream = asset.Stream;
		lock (@this._loadedAssetPoolLock) {
			ReturnAssetTreeToPool(@this, asset);
		}
		stream.Dispose();
	}
	static void ReturnAssetTreeToPool(LocalAssetBakery @this, LoadedBakedAsset asset) {
		Debug.Assert(@this._loadedAssetPoolLock.IsHeldByCurrentThread);
		foreach (var sectionValue in asset.Sections.Values) {
			if (sectionValue.SubAsset is { } subAsset) ReturnAssetTreeToPool(@this, subAsset);
		}
		asset.Sections.Clear();
		asset.Type = null!;
		asset.Stream = default;
		asset.IsRootAsset = false;
		@this._loadedAssetPool.Return(asset);
	}
	
	LoadedBakedAsset RentLoadedBakedAsset() {
		lock (_loadedAssetPoolLock) {
			return _loadedAssetPool.Rent();
		}
	}
	
	LoadedBakedAsset Load<TResource>(ReadOnlySpan<char> filePath) where TResource : IResource {
		if (!_typeToTypeIdMap.TryGetValue(typeof(TResource), out var typeId)) {
			throw new InvalidOperationException($"Could not load {typeof(TResource).Name} resource at '{filePath}' because of unsupported type ID (this is a bug in TinyFFR).");
		}
		
		var stream = LocalFileSystemUtils.ReadFileIntoPooledMemory(_globals.HeapPool.ThreadSafeWrapper, filePath.ToString(), "Baked resource");
		try {
			var span = stream.Span;
			if (span.Length < FileHeaderLengthBytes) throw new AssetBakeException("Stream length shorter than expected file header length.");
			if (!span[..BakedAssetStreamStartString.Length].SequenceEqual(BakedAssetStreamStartString)) throw new AssetBakeException("File header corrupt or not a TinyFFR baked asset.");
			if (BinaryPrimitives.ReadInt32LittleEndian(span[BakedAssetStreamStartString.Length..]) != BakedResourceSchemata.VersionMajor) throw new AssetBakeException("Baked asset was made with an incompatible TinyFFR library version.");
			if (_config.RequireStrictAssetBakeSchemaMatch && BinaryPrimitives.ReadInt32LittleEndian(span[(BakedAssetStreamStartString.Length + sizeof(int))..]) != BakedResourceSchemata.VersionMinor) {
				throw new AssetBakeException($"Baked asset was made with an incompatible TinyFFR library version (failed due to {nameof(AssetBakeryConfig.RequireStrictAssetBakeSchemaMatch)} being true).");
			}
			var declaredTypeId = BinaryPrimitives.ReadInt32LittleEndian(span[(BakedAssetStreamStartString.Length + sizeof(int) * 2)..]);
			if (declaredTypeId != typeId) {
				var declaredTypeName = declaredTypeId >= 0 && declaredTypeId < _typeIdToTypeMap.Length ? _typeIdToTypeMap[declaredTypeId].Name : $"<unknown type ID {declaredTypeId}>";
				throw new AssetBakeException($"Baked asset declares itself as being of type '{declaredTypeName}' but was being loaded as a '{typeof(TResource).Name}'.");
			}

			return ReadAssetFromStream(typeof(TResource), stream, FileHeaderLengthBytes, stream.Span.Length, isRootAsset: true);
		}
		catch (Exception e) {
			stream.Dispose();
			throw new AssetBakeException($"Could not load baked {typeof(TResource).Name} at '{filePath.ToString()}'.", e);
		}
	}
	
	LoadedBakedAsset ReadAssetFromStream(Type type, PooledHeapMemory<byte> stream, int cursor, int assetEndPoint, bool isRootAsset) {
		var span = stream.Span[cursor..assetEndPoint];

		var result = RentLoadedBakedAsset();
		result.Stream = stream;
		result.Type = type;
		result.IsRootAsset = isRootAsset;

		try {
			while (cursor < assetEndPoint) {
				// Section header
				if (span.Length < SectionStartString.Length || !span[..SectionStartString.Length].SequenceEqual(SectionStartString)) {
					throw new AssetBakeException($"Corrupt file (incomplete section header at byte {cursor}).");
				}
				cursor += SectionStartString.Length;
				span = stream.Span[cursor..assetEndPoint];
			
				// Section name length
				if (span.Length < sizeof(int)) {
					throw new AssetBakeException($"Corrupt file (incomplete section header at byte {cursor}).");
				}
				var expectedSectionNameLength = BinaryPrimitives.ReadInt32LittleEndian(span);
				cursor += sizeof(int);
				span = stream.Span[cursor..assetEndPoint];
			
				// Section name
				if (expectedSectionNameLength <= 0 || expectedSectionNameLength > span.Length) {
					throw new AssetBakeException($"Corrupt file (section header declared section name length of {expectedSectionNameLength} at byte {cursor - sizeof(int)}).");
				}
				var utf16Length = Encoding.UTF8.GetCharCount(span[..expectedSectionNameLength]);
				using var utf16Buffer = _globals.HeapPool.ThreadSafeWrapper.Borrow<char>(utf16Length);
				Encoding.UTF8.GetChars(span[..expectedSectionNameLength], utf16Buffer.Span);
				cursor += expectedSectionNameLength;
				span = stream.Span[cursor..assetEndPoint];
			
				// Type ID
				if (span.Length < sizeof(int)) {
					throw new AssetBakeException($"Corrupt file (section header ended before declaring type ID at byte {cursor}).");
				}
				var typeId = BinaryPrimitives.ReadInt32LittleEndian(span);
				cursor += sizeof(int);
				span = stream.Span[cursor..assetEndPoint];
				
				// Data
				if (span.Length < sizeof(int)) {
					throw new AssetBakeException($"Corrupt file (section header ended before declaring data length at byte {cursor}).");
				}
				var dataLength = BinaryPrimitives.ReadInt32LittleEndian(span);
				cursor += sizeof(int);
				span = stream.Span[cursor..assetEndPoint];
				
				if (dataLength < 0 || dataLength > span.Length) {
					throw new AssetBakeException($"Corrupt file (section header indicated data length ({dataLength}) longer than the remainder of the stream ({span.Length}) at byte {cursor}).");
				}
				
				if (typeId < 0 || typeId >= _typeIdToTypeMap.Length) {
					if (_config.RequireStrictAssetBakeSchemaMatch) {
						throw new AssetBakeException(
							$"Baked asset declares embedded data value of type '{typeId}' at byte {cursor - sizeof(int) * 2}) which is unknown to this version of TinyFFR " +
							$"(failed due to {nameof(AssetBakeryConfig.RequireStrictAssetBakeSchemaMatch)} being true)."
						);
					}
				}
				else {
					var sectionType = _typeIdToTypeMap[typeId];
					LoadedBakedAsset? subAsset = null; 
					if (sectionType.IsAssignableTo(typeof(IResource))) {
						subAsset = ReadAssetFromStream(sectionType, stream, cursor, cursor + dataLength, isRootAsset: false);
					}
					result.Sections.Add(utf16Buffer.Span, new BakedAssetStreamSection(sectionType, cursor, dataLength, subAsset));
				}
				
				cursor += dataLength;
				span = stream.Span[cursor..assetEndPoint];
			}
		}
		catch {
			lock (_loadedAssetPoolLock) {
				ReturnAssetTreeToPool(this, result);
			}
			throw;
		}
		
		return result;
	}
	
	public TResult Load<TAsset, TResult, TThis>(TThis @this, ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> nameOverride, delegate* managed<AssetLoadContext, TResult> callback) where TAsset : IResource where TResult : struct, IResource<TResult> where TThis : class {
		static TResult Execute(AssetLoadContext ctx, in AssetLoadConfig cfg) {
			ctx.AssetData = ctx.Self.Load<TAsset>(ctx.AssetFilePath!.Value.Span);
			return ((delegate* managed<AssetLoadContext, TResult>) cfg.Callback)(ctx);
		}
		
		var ctxWrapper = _loadSyncHelper.CreateContextWrapper();
		ctxWrapper.Context.SetName(nameOverride);
		ctxWrapper.Context.AssetFilePath = ctxWrapper.Context.HeapPool.BorrowAndCopy(bakedAssetFilePath);
		ctxWrapper.Context.This = @this;
		return ctxWrapper.DispatchResourceReturningSynchronousOperation(&Execute, new AssetLoadConfig { Callback = callback });
	}

	public TinyFfrAsyncOperation<TResult> LoadAsync<TAsset, TResult, TThis>(TThis @this, ReadOnlySpan<char> bakedAssetFilePath, ReadOnlySpan<char> nameOverride, delegate* managed<AssetLoadContext, TResult> callback) where TAsset : IResource where TResult : struct, IResource<TResult> where TThis : class {
		static TResult Execute(AssetLoadContext ctx, in AssetLoadConfig cfg) {
			ctx.AssetData = ctx.Self.Load<TAsset>(ctx.AssetFilePath!.Value.Span);
			return ((delegate* managed<AssetLoadContext, TResult>) cfg.Callback)(ctx);
		}
		
		var ctxWrapper = _loadSyncHelper.CreateContextWrapper();
		ctxWrapper.Context.SetName(nameOverride);
		ctxWrapper.Context.AssetFilePath = ctxWrapper.Context.HeapPool.BorrowAndCopy(bakedAssetFilePath);
		ctxWrapper.Context.This = @this;
		return ctxWrapper.DispatchResourceReturningAsynchronousOperation(&Execute, new AssetLoadConfig { Callback = callback });
	}
	#endregion
	
	public void ClearBakeryMemory() {
		_resourcesReadyForBaking.Clear(invokeCacheEvictionCallbackOnAllContainedValues: true);
	}

	static void CacheEvictionCallback(object? @this, ResourceStub key, BakedData value) {
		value.Buffer.Dispose();
		((LocalAssetBakery) @this!)._pendingFinalizers.Remove(key);
	}
	
	void ThrowIfThisIsDisposedOrDisabled() {
		ThrowIfThisIsDisposed();
		ThrowIfThisIsDisabled();
	}
	void ThrowIfThisIsDisabled() {
		if (!Enabled) throw new InvalidOperationException("Attempted resource bake on disabled bakery (this is a bug in TinyFFR).");
	}
	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);

	public void Dispose() {
		try {
			if (_isDisposed) return;
			Enabled = false;
			foreach (var val in _inProgressResources.Values) {
				val.Buffer.Dispose();
			}
			_inProgressResources.Dispose();
			_resourcesReadyForBaking.Dispose(invokeCacheEvictionCallbackOnAllContainedValues: true);
			_pendingFinalizers.Dispose();
			lock (_loadedAssetPoolLock) {
				_loadedAssetPool.Dispose(invokeDisposeOnEachItemBeforeRelease: false);
			}
			_loadSyncHelper.Dispose();
		}
		finally {
			_isDisposed = true;
		}
	}
}