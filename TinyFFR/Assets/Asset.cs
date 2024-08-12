// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Assets;

public readonly unsafe struct Asset : IEquatable<Asset>, IDisposable {
	internal enum AssetType {
		Undefined,
		AssetGroup,
		Mesh
	}

	internal readonly AssetType Type;
	internal readonly AssetHandle Handle;

	internal UIntPtr HandleAsPtr {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => (UIntPtr) Handle;
	}

	internal Asset(AssetType type, AssetHandle handle) {
		Type = type;
		Handle = handle;
	}

	public void Dispose() {
		ThrowIfInvalid();
		switch (Type) {
			case AssetType.AssetGroup:

				break;
			case AssetType.Mesh:
				((MeshAsset) this).Dispose();
				break;
		}
	}

	public bool Equals(Asset other) {
		ThrowIfInvalid();
		return Type == other.Type && Handle == other.Handle;
	}
	public override bool Equals(object? obj) => obj is Asset other && Equals(other);
	public override int GetHashCode() {
		ThrowIfInvalid();
		return HashCode.Combine((int) Type, unchecked((int) (long) Handle));
	}
	public static bool operator ==(Asset left, Asset right) => left.Equals(right);
	public static bool operator !=(Asset left, Asset right) => !left.Equals(right);

	internal void ThrowIfInvalid() {
		if (!Enum.IsDefined(Type) || Type == AssetType.Undefined) throw InvalidObjectException.InvalidDefault<Asset>();
	}

	public static implicit operator Asset(MeshAsset meshAsset) { meshAsset.ThrowIfInvalid(); return new Asset(AssetType.Mesh, meshAsset.Handle); }
	public static explicit operator MeshAsset(Asset asset) => asset.Downcast(AssetType.Mesh, new MeshAsset(asset.Handle));

	T Downcast<T>(AssetType requiredType, T potentialResult) {
		ThrowIfInvalid();
		if (Type == requiredType) return potentialResult;

		throw new InvalidOperationException($"Can not cast this {nameof(Asset)} to a {typeof(T).Name}, it is not of type '{requiredType}' (its type is '{Type}').");
	}
}