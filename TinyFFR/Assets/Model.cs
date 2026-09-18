// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Resources;
using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;

namespace Egodystonic.TinyFFR.Assets;

/// <summary>
/// One mesh paired with one material; everything needed to describe an object, short of putting it somewhere.
/// </summary>
/// <remarks>
/// <para>
/// Loading a model file yields models rather than loose meshes and materials, which is what lets a whole scene's worth of
/// objects be created in one step. Creating an instance of a model is what actually puts it in a scene; one model can back any
/// number of instances.
/// </para>
/// <para>
/// A model does not own its mesh or material, so disposing it leaves both intact.
/// </para>
/// </remarks>
public readonly struct Model : IDisposableResource<Model, IModelImplProvider> {
	readonly ResourceHandle<Model> _handle;
	readonly IModelImplProvider _impl;

	internal ResourceHandle<Model> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Model)) : _handle;
	internal IModelImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Model>();

	IModelImplProvider IResource<Model, IModelImplProvider>.Implementation => Implementation;
	ResourceHandle<Model> IResource<Model>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The geometry this model uses.
	/// </summary>
	public Mesh Mesh {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMesh(_handle);
	}
	/// <summary>
	/// The surface appearance this model uses.
	/// </summary>
	public Material Material {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetMaterial(_handle);
	}

	internal Model(ResourceHandle<Model> handle, IModelImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static Model IResource<Model>.CreateFromHandleAndImpl(ResourceHandle<Model> handle, IResourceImplProvider impl) {
		return new Model(handle, impl as IModelImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<Model> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<Model> IResource<Model>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Disposal
	/// <summary>
	/// Disposes this model.
	/// </summary>
	/// <remarks>
	/// The mesh and material are not disposed; a model does not own them.
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"Model {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Equality
	/// <inheritdoc />
	public bool Equals(Model other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is Model other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// Returns whether the two given models are the same model.
	/// </summary>
	/// <param name="left">The first model to compare.</param>
	/// <param name="right">The second model to compare.</param>
	public static bool operator ==(Model left, Model right) => left.Equals(right);
	/// <summary>
	/// Returns whether the two given models are different models.
	/// </summary>
	/// <param name="left">The first model to compare.</param>
	/// <param name="right">The second model to compare.</param>
	public static bool operator !=(Model left, Model right) => !left.Equals(right);
	#endregion
}