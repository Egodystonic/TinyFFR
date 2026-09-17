// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A light that radiates outward in every direction from a single point, like a bare bulb.
/// </summary>
/// <remarks>
/// Point lights are the workhorse of scene lighting: lamps, fires, torches and glowing objects are all point lights. Their brightness falls away with distance, and
/// <see cref="MaxIlluminationRadius"/> sets the distance beyond which they stop contributing altogether.
/// </remarks>
public readonly struct PointLight : ILight<PointLight>, IPositionedSceneObject {
	/// <summary>
	/// The largest permitted <see cref="Brightness"/>: <c>1E+15f</c>.
	/// </summary>
	public const float MaxBrightness = 1E+15f;
	/// <summary>
	/// How many lumens a point light emits at a <see cref="Brightness"/> of <c>1f</c>: <c>1,250,000</c>.
	/// </summary>
	public const float DefaultLumens = 1_250_000f;

	readonly ResourceHandle<PointLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<PointLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(PointLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<PointLight>();

	ILightImplProvider IResource<PointLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<PointLight> IResource<PointLight>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal PointLight(ResourceHandle<PointLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static PointLight IResource<PointLight>.CreateFromHandleAndImpl(ResourceHandle<PointLight> handle, IResourceImplProvider impl) {
		return new PointLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<PointLight> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<PointLight> IResource<PointLight>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Light Type Casting
	/// <inheritdoc />
	public Light AsBaseLight() => new(_handle, _impl);

	/// <summary>
	/// Reinterprets this point light as a kind-agnostic <see cref="Light"/>.
	/// </summary>
	/// <param name="operand">The point light to reinterpret.</param>
	public static implicit operator Light(PointLight operand) => operand.AsBaseLight();
	/// <summary>
	/// Reinterprets a kind-agnostic <see cref="Light"/> as a point light.
	/// </summary>
	/// <remarks>
	/// Explicit because it is only meaningful when the light really is a point light; check <see cref="Light.Type"/> first.
	/// </remarks>
	/// <param name="operand">The light to reinterpret.</param>
	public static explicit operator PointLight(Light operand) => operand.As<PointLight>();
	static PointLight ILight<PointLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.Point);
		return new((ResourceHandle<PointLight>) l.Handle, l.Implementation);
	}
	/// <inheritdoc />
	public override string ToString() => AsBaseLight().ToString();
	#endregion

	#region Common Light Members
	/// <inheritdoc />
	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetColor(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetColor(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Color"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="color">The new value for <see cref="Color"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Color = color;

	/// <inheritdoc />
	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Hue;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithHue(value);
	}
	/// <summary>
	/// Sets <see cref="ColorHue"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="hue">The new value for <see cref="ColorHue"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => ColorHue = hue;

	/// <inheritdoc />
	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Saturation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithSaturation(value);
	}
	/// <summary>
	/// Sets <see cref="ColorSaturation"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="saturation">The new value for <see cref="ColorSaturation"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => ColorSaturation = saturation;

	/// <inheritdoc />
	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Lightness;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithLightness(value);
	}
	/// <summary>
	/// Sets <see cref="ColorLightness"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="lightness">The new value for <see cref="ColorLightness"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => ColorLightness = lightness;

	/// <summary>
	/// How much light this emits, where <c>1f</c> corresponds to <see cref="DefaultLumens"/>. Clamped to between <c>0f</c> and <see cref="MaxBrightness"/>.
	/// </summary>
	/// <remarks>
	/// Note that the relationship is <i>quadratic</i>, not linear: the light emitted is <see cref="DefaultLumens"/> multiplied by the square of this value, so
	/// doubling the brightness quadruples the light. Use <see cref="LumensToBrightness"/> and <see cref="BrightnessToLumens"/> to work in lumens directly. Negative
	/// and non-finite values are treated as <c>0f</c>.
	/// </remarks>
	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUniversalBrightness(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUniversalBrightness(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Brightness"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="brightness">The new value for <see cref="Brightness"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Brightness = brightness;

	/// <inheritdoc />
	public bool CastsShadows {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetIsShadowCaster(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetIsShadowCaster(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="CastsShadows"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="castsShadows">The new value for <see cref="CastsShadows"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetCastsShadows(bool castsShadows) => CastsShadows = castsShadows;

	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	/// <inheritdoc />
	public void AdjustColorHueBy(Angle adjustment) => Color = Color.WithHueAdjustedBy(adjustment);
	/// <inheritdoc />
	public void AdjustColorSaturationBy(float adjustment) => Color = Color.WithSaturationAdjustedBy(adjustment);
	/// <inheritdoc />
	public void AdjustColorLightnessBy(float adjustment) => Color = Color.WithLightnessAdjustedBy(adjustment);
	/// <inheritdoc />
	public void AdjustBrightnessBy(float adjustment) => Implementation.AdjustBrightnessBy(_handle, adjustment);
	/// <inheritdoc />
	public void ScaleBrightnessBy(float scalar) => Implementation.ScaleBrightnessBy(_handle, scalar);

	void ILight.SetShadowFidelity(LightShadowFidelityData fidelityArgs) => SetShadowFidelity(fidelityArgs);
	internal void SetShadowFidelity(LightShadowFidelityData fidelityArgs) => Implementation.SetShadowFidelity(_handle, fidelityArgs);
	#endregion

	#region PointLight Specific
	static LightType ILight<PointLight>.SelfType { get; } = LightType.Point;
	LightType ILight.Type => LightType.Point;

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightPosition(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <summary>
	/// How far this light reaches, in metres; beyond this distance it contributes no light at all.
	/// </summary>
	/// <remarks>
	/// A real light never quite stops, but continuing to calculate a contribution too faint to see is wasted work, so lights are given a finite reach. Setting this
	/// too small for the light's brightness produces a visible edge where the light cuts off.
	/// </remarks>
	public float MaxIlluminationRadius {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetPointLightMaxIlluminationRadius(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetPointLightMaxIlluminationRadius(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="MaxIlluminationRadius"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="range">The new value for <see cref="MaxIlluminationRadius"/>, in metres.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaxIlluminationRange(float range) => MaxIlluminationRadius = range;

	/// <inheritdoc />
	public void MoveBy(Vect translation) => Position += translation;

	/// <summary>
	/// Converts a light output in lumens to the equivalent <see cref="Brightness"/> value.
	/// </summary>
	/// <param name="lumens">The light output to convert. Negative and non-finite values return <c>0f</c>.</param>
	public static float LumensToBrightness(float lumens) {
		if (!lumens.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(MathF.Sqrt(lumens / DefaultLumens), MaxBrightness);
	}

	/// <summary>
	/// Converts a <see cref="Brightness"/> value to the light output it represents, in lumens.
	/// </summary>
	/// <param name="brightness">The brightness to convert. Negative and non-finite values are treated as <c>0f</c>.</param>
	public static float BrightnessToLumens(float brightness) {
		return BrightnessToLumensNoClamp(ClampBrightnessToValidRange(brightness));
	}

	internal static float BrightnessToLumensNoClamp(float brightness) {
		return DefaultLumens * brightness * brightness;
	}

	internal static float ClampBrightnessToValidRange(float input) {
		if (!input.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(input, MaxBrightness);
	}
	#endregion

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	/// <summary>
	/// Returns whether this light and <paramref name="other"/> refer to the same underlying light.
	/// </summary>
	/// <param name="other">The light to compare with this one.</param>
	public bool Equals(Light other) => AsBaseLight().Equals(other);
	/// <inheritdoc />
	public bool Equals(PointLight other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator ==(PointLight left, PointLight right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator !=(PointLight left, PointLight right) => !left.Equals(right);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator ==(Light left, PointLight right) => right.Equals(left);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator !=(Light left, PointLight right) => !right.Equals(left);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator ==(PointLight left, Light right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(PointLight)"/>
	/// </summary>
	public static bool operator !=(PointLight left, Light right) => !left.Equals(right);
	#endregion
}