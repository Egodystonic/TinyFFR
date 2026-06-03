// using System;
// using Egodystonic.TinyFFR.Assets.Materials;
// using Egodystonic.TinyFFR.Assets.Meshes;
// using Egodystonic.TinyFFR.Resources;
// using Egodystonic.TinyFFR.Resources.Memory;
//
// namespace Egodystonic.TinyFFR.World;
//
// public readonly struct RenderPrimitive : IDisposableResource<RenderPrimitive, IScenePrimitiveImplProvider> {
// 	readonly ResourceHandle<RenderPrimitive> _handle;
// 	readonly IScenePrimitiveImplProvider _impl;
//
// 	internal IScenePrimitiveImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<RenderPrimitive>();
// 	internal ResourceHandle<RenderPrimitive> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(RenderPrimitive)) : _handle;
//
// 	IScenePrimitiveImplProvider IResource<RenderPrimitive, IScenePrimitiveImplProvider>.Implementation => Implementation;
// 	ResourceHandle<RenderPrimitive> IResource<RenderPrimitive>.Handle => Handle;
//
// 	internal RenderPrimitive(ResourceHandle<RenderPrimitive> handle, IScenePrimitiveImplProvider impl) {
// 		_handle = handle;
// 		_impl = impl;
// 	}
//
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public void SetAsNothing() => Implementation.SetAsNothing(_handle);
//
// 	string IStringSpanNameEnabled.GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
// 	int IStringSpanNameEnabled.GetNameLength() => Implementation.GetNameLength(_handle);
// 	void IStringSpanNameEnabled.CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);
//
// 	static RenderPrimitive IResource<RenderPrimitive>.CreateFromHandleAndImpl(ResourceHandle<RenderPrimitive> handle, IResourceImplProvider impl) {
// 		return new RenderPrimitive(handle, impl as IScenePrimitiveImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
// 	}
// 	public ResourceHandle<RenderPrimitive> GetHandleWithoutDisposeCheck() => _handle;
//
// 	#region Disposal
// 	[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 	public void Dispose() => Implementation.Dispose(_handle);
//
// 	internal bool IsDisposed {
// 		[MethodImpl(MethodImplOptions.AggressiveInlining)]
// 		get => Implementation.IsDisposed(_handle);
// 	}
// 	#endregion
//
// 	public override string ToString() => $"Scene Primitive {(IsDisposed ? "(Disposed)" : $"\"{Implementation.GetNameAsNewStringObject(_handle)}\"")}";
//
// 	#region Equality
// 	public bool Equals(RenderPrimitive other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
// 	public override bool Equals(object? obj) => obj is RenderPrimitive other && Equals(other);
// 	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
// 	public static bool operator ==(RenderPrimitive left, RenderPrimitive right) => left.Equals(right);
// 	public static bool operator !=(RenderPrimitive left, RenderPrimitive right) => !left.Equals(right);
// 	#endregion
// }