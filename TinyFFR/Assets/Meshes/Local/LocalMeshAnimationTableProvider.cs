// Created on 2026-09-30 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

sealed unsafe class LocalMeshAnimationTableProvider : IResourceDirectory<MeshAnimation>, IResourceDirectory<MeshNode>, IDisposable {
	readonly LocalFactoryGlobalObjectGroup _globals;
	readonly ArrayPoolBackedObjectPool<LocalMeshAnimationTable, LocalMeshAnimationTableProvider> _tablePool;
	readonly ArrayPoolBackedVector<LocalMeshAnimationTable> _activeTables = new();
	int _directoryVersion = 0;
	bool _isDisposed = false;

	public LocalMeshAnimationTableProvider(LocalFactoryGlobalObjectGroup globals) {
		ArgumentNullException.ThrowIfNull(globals);
		_globals = globals;
		_tablePool = new(&CreateNewTable, this);
	}

	static LocalMeshAnimationTable CreateNewTable(LocalMeshAnimationTableProvider @this) => new(@this._globals, @this);

	public LocalMeshAnimationTable Rent() {
		ThrowIfThisIsDisposed();
		var result = _tablePool.Rent();
		_activeTables.Add(result);
		++_directoryVersion;
		return result;
	}

	public void Return(LocalMeshAnimationTable table) {
		ThrowIfThisIsDisposed();
		if (!_activeTables.Remove(table)) throw new InvalidOperationException("Given animation table was not rented from this provider (this is a bug in TinyFFR).");
		table.Recycle();
		_tablePool.Return(table);
		++_directoryVersion;
	}

	internal void NotifyAnimationsChanged() => ++_directoryVersion;

	#region Resource Directory
	IndirectEnumerable<object, MeshAnimation> IResourceDirectory<MeshAnimation>.AllActiveInstances {
		get {
			static LocalMeshAnimationTableProvider CastSelf(object self) => self as LocalMeshAnimationTableProvider ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetCount(object self) {
				var provider = CastSelf(self);
				var count = 0;
				for (var i = 0; i < provider._activeTables.Count; ++i) count += provider._activeTables[i].Count;
				return count;
			}
			static int GetVersion(object self) => CastSelf(self)._directoryVersion;
			static MeshAnimation GetItem(object self, int index) {
				var provider = CastSelf(self);
				for (var i = 0; i < provider._activeTables.Count; ++i) {
					var table = provider._activeTables[i];
					if (index < table.Count) return table.GetAnimationAtUnstableIndex(index);
					index -= table.Count;
				}
				throw new InvalidOperationException($"Index '{index}' out of range.");
			}

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
	public bool ResourceNameMatchIsMatching(MeshAnimation resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var handle = resource.GetHandleWithoutDisposeCheck();
		var resourceName = _globals.GetMandatoryResourceName(handle.Ident);
		return allowPartialMatch
			? resourceName.Contains(name, comparisonType)
			: resourceName.Equals(name, comparisonType);
	}

	IndirectEnumerable<object, MeshNode> IResourceDirectory<MeshNode>.AllActiveInstances {
		get {
			static LocalMeshAnimationTableProvider CastSelf(object self) => self as LocalMeshAnimationTableProvider ?? throw new InvalidOperationException($"Enumeration invoked on {self?.GetType().Name}.");
			static int GetNodeCount(LocalMeshAnimationTable table) => table.SkeletonIsSet ? table.GetNodeCount() : 0;
			static int GetCount(object self) {
				var provider = CastSelf(self);
				var count = 0;
				for (var i = 0; i < provider._activeTables.Count; ++i) count += GetNodeCount(provider._activeTables[i]);
				return count;
			}
			static int GetVersion(object self) => CastSelf(self)._directoryVersion;
			static MeshNode GetItem(object self, int index) {
				var provider = CastSelf(self);
				for (var i = 0; i < provider._activeTables.Count; ++i) {
					var table = provider._activeTables[i];
					var nodeCount = GetNodeCount(table);
					if (index < nodeCount) return table.GetNode(index);
					index -= nodeCount;
				}
				throw new InvalidOperationException($"Index '{index}' out of range.");
			}

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
	public bool ResourceNameMatchIsMatching(MeshNode resource, ReadOnlySpan<char> name, bool allowPartialMatch, StringComparison comparisonType) {
		var nameLen = resource.GetNameLength();
		using var nameBuffer = _globals.HeapPool.Borrow<char>(nameLen);
		resource.CopyName(nameBuffer.Span);
		return allowPartialMatch
			? nameBuffer.Span.Contains(name, comparisonType)
			: nameBuffer.Span.Equals(name, comparisonType);
	}
	#endregion

	public override string ToString() => _isDisposed ? "TinyFFR Local Mesh Animation Table Provider [Disposed]" : "TinyFFR Local Mesh Animation Table Provider";

	#region Disposal
	public void Dispose() {
		if (_isDisposed) return;
		try {
			for (var i = _activeTables.Count - 1; i >= 0; --i) {
				var table = _activeTables[i];
				table.Recycle();
				_tablePool.Return(table);
			}
			_activeTables.Dispose();
			_tablePool.Dispose(invokeDisposeOnEachItemBeforeRelease: true);
		}
		finally {
			_isDisposed = true;
		}
	}

	void ThrowIfThisIsDisposed() => ObjectDisposedException.ThrowIf(_isDisposed, this);
	#endregion
}
