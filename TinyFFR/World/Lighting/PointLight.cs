// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

public readonly struct PointLight : ILight<PointLight>, IEquatable<PointLight> {
	#region Base Light Impl
	readonly Light _base;
	internal Light Base => _base == default ? throw InvalidObjectException.InvalidDefault<PointLight>() : _base;
	internal ILightImplProvider Implementation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Implementation;
	}
	internal ResourceHandle<Light> Handle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Handle;
	}
	ILightImplProvider IResource<Light, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<Light> IResource<Light>.Handle => Handle;
	internal PointLight(Light @base) => _base = @base;
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Base.Dispose();
	public static implicit operator Light(PointLight operand) => operand.Base;
	public static explicit operator PointLight(Light operand) {
		Light.ThrowIfInvalidType(operand, LightType.PointLight);
		return new(operand);
	}
	static PointLight ILight<PointLight>.FromBaseLight(Light l) => (PointLight) l;
	static Light IResource<Light>.CreateFromHandleAndImpl(ResourceHandle<Light> handle, IResourceImplProvider impl) {
		return new Light(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	public override string ToString() => $"Point{Base}";
	#endregion

	#region Equality
	bool IEquatable<Light>.Equals(Light other) => _base.Equals(other);
	public bool Equals(PointLight other) => _base.Equals(other._base);
	public override bool Equals(object? obj) => obj is PointLight other && Equals(other);
	public override int GetHashCode() => _base.GetHashCode();
	public static bool operator ==(PointLight left, PointLight right) => left.Equals(right);
	public static bool operator !=(PointLight left, PointLight right) => !left.Equals(right);
	#endregion

	public ReadOnlySpan<char> Name {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Name;
	}

	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Position;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetPosition(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Base.Color;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Base.SetColor(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Color = color;

	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightLumens(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightLumens(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float lumens) => Brightness = lumens;

	public float MaxIlluminationRadius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightMaxIlluminationRadius(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightMaxIlluminationRadius(Handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetFalloffRange(float range) => MaxIlluminationRadius = range;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void MoveBy(Vect translation) => Base.MoveBy(translation);
}