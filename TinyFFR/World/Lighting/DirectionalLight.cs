// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A preset brightness for a <see cref="DirectionalLight"/>, named after the lighting condition it matches.
/// </summary>
/// <remarks>
/// <para>
/// Use with <see cref="DirectionalLight.SetBrightness(DirectionalLightBrightnessPreset)"/>, or <see cref="DirectionalLightBrightnessExtensions.ToBrightnessValue"/> to get the
/// equivalent <see cref="DirectionalLight.Brightness"/> value (for example, to pass when creating a light).
/// </para>
/// <para>
/// Each preset lights a scene completely on its own: a directional light at a given preset, viewed through a camera using the <see cref="CameraExposurePreset"/> of the same name,
/// is correctly exposed with no other lights needed. The same is true of the <see cref="SceneBackdropBrightnessPreset"/> of the same name. Using both together adds their light,
/// so lower the camera's exposure (or the brightness of one of them) to compensate.
/// </para>
/// </remarks>
public enum DirectionalLightBrightnessPreset {
	/// <summary>
	/// The light of a well-lit interior, such as an office (600 lux).
	/// Pairs with <see cref="CameraExposurePreset.InsideBrightLighting"/> (the default camera exposure).
	/// </summary>
	InsideBrightLighting = 0,
	/// <summary>
	/// Direct midday sunlight on a clear day (125,000 lux). This is the default brightness of a <see cref="DirectionalLight"/>.
	/// Pairs with <see cref="CameraExposurePreset.OutsideMidday"/>.
	/// </summary>
	Midday,
	/// <summary>
	/// The light of an overcast day, as a single directional light (10,000 lux).
	/// Pairs with <see cref="CameraExposurePreset.OutsideOvercast"/>.
	/// </summary>
	Overcast,
	/// <summary>
	/// The low sun shortly after sunrise or before sunset (2,500 lux).
	/// Pairs with <see cref="CameraExposurePreset.OutsideSunriseSunset"/>.
	/// </summary>
	SunriseSunset,
	/// <summary>
	/// The fading light just after sunset, as a single directional light (40 lux).
	/// Pairs with <see cref="CameraExposurePreset.OutsideTwilight"/>.
	/// </summary>
	Twilight,
	/// <summary>
	/// The light of a full moon (0.25 lux). Pairs with <see cref="CameraExposurePreset.OutsideFullMoon"/>.
	/// </summary>
	FullMoon,
	/// <summary>
	/// The light of a moonless night, lit only by starlight and airglow, as a single directional light (0.005 lux).
	/// Pairs with <see cref="CameraExposurePreset.OutsideStarlight"/>.
	/// </summary>
	Starlight,
}

/// <summary>
/// Extension methods for <see cref="DirectionalLightBrightnessPreset"/>.
/// </summary>
public static class DirectionalLightBrightnessExtensions {
	extension(DirectionalLightBrightnessPreset @this) {
		/// <summary>
		/// Returns the illuminance, in lux, of the lighting condition this preset represents.
		/// </summary>
		public float ToLux() {
			return @this switch {
				DirectionalLightBrightnessPreset.Midday => 125_000f,
				DirectionalLightBrightnessPreset.Overcast => 10_000f,
				DirectionalLightBrightnessPreset.SunriseSunset => 2_500f,
				DirectionalLightBrightnessPreset.Twilight => 40f,
				DirectionalLightBrightnessPreset.FullMoon => 0.25f,
				DirectionalLightBrightnessPreset.Starlight => 0.005f,
				_ /* InsideBrightLighting */ => DirectionalLight.DefaultLux
			};
		}

		/// <summary>
		/// Returns the <see cref="DirectionalLight.Brightness"/> value that this preset represents.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float ToBrightnessValue() => DirectionalLight.LuxToBrightness(@this.ToLux());
	}
}

/// <summary>
/// A light that arrives from one direction across the entire scene, like sunlight. Created via the factory's <see cref="ILightBuilder"/>.
/// </summary>
/// <remarks>
/// A directional light has no position and no falloff: it is treated as coming from infinitely far away, so every object in the scene is lit from the same angle
/// with the same intensity. This is what makes it the right way to model the sun or moon. It can also draw a visible disc in the sky; see
/// <see cref="SetSunDiscParameters"/>.
/// </remarks>
public readonly struct DirectionalLight : ILight<DirectionalLight>, IOrientedSceneObject {
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.DirectionalLight;

	/// <summary>
	/// The largest permitted <see cref="Brightness"/>: <c>1E+15f</c>.
	/// </summary>
	public const float MaxBrightness = 1E+15f;
	/// <summary>
	/// How many lux a directional light casts at a <see cref="Brightness"/> of <c>1f</c>: <c>600</c>, roughly that of indoor bright lighting.
	/// </summary>
	public const float DefaultLux = 600f;

	readonly ResourceHandle<DirectionalLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<DirectionalLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(DirectionalLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<DirectionalLight>();

	ILightImplProvider IResource<DirectionalLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<DirectionalLight> IResource<DirectionalLight>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal DirectionalLight(ResourceHandle<DirectionalLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static DirectionalLight IResource<DirectionalLight>.CreateFromHandleAndImpl(ResourceHandle<DirectionalLight> handle, IResourceImplProvider impl) {
		return new DirectionalLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<DirectionalLight> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<DirectionalLight> IResource<DirectionalLight>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Light Type Casting
	/// <inheritdoc />
	public Light AsBaseLight() => new(_handle, _impl);

	/// <summary>
	/// Reinterprets this directional light as a kind-agnostic <see cref="Light"/>.
	/// </summary>
	/// <param name="operand">The directional light to reinterpret.</param>
	public static implicit operator Light(DirectionalLight operand) => operand.AsBaseLight();
	/// <summary>
	/// Reinterprets a kind-agnostic <see cref="Light"/> as a directional light.
	/// </summary>
	/// <remarks>
	/// Explicit because it is only meaningful when the light really is a directional light; check <see cref="Light.Type"/> first.
	/// </remarks>
	/// <param name="operand">The light to reinterpret.</param>
	public static explicit operator DirectionalLight(Light operand) => operand.As<DirectionalLight>();
	static DirectionalLight ILight<DirectionalLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.Directional);
		return new((ResourceHandle<DirectionalLight>) l.Handle, l.Implementation);
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
	/// How much light this emits as a unitless scalar. Doubling this value corresponds to a perceived doubling
	/// in output brightness, halving corresponds to a perceived halving of brightness, etc.
	/// </summary>
	/// <remarks>
	/// You can also use <see cref="SetBrightness(DirectionalLightBrightnessPreset)"/> to set a value using a preset.
	/// </remarks>
	/// <seealso cref="BrightnessLux"/>
	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUniversalBrightness(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUniversalBrightness(_handle, value);
	}
	/// <summary>
	/// How much light this emits in lux. This property is offered as an alternative to the unitless <see cref="Brightness"/>
	/// for workflows that prefer real-world light setups.
	/// </summary>
	public float BrightnessLux {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BrightnessToLux(Brightness);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Brightness = LuxToBrightness(value);
	}
	/// <summary>
	/// Sets <see cref="Brightness"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="brightness">The new value for <see cref="Brightness"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Brightness = brightness;
	/// <summary>
	/// Sets <see cref="Brightness"/> to match the given time of day or weather.
	/// </summary>
	/// <param name="preset">The preset to apply.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="preset"/> is not a defined <see cref="DirectionalLightBrightnessPreset"/> value.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBrightness(DirectionalLightBrightnessPreset preset) => Brightness = preset.ToBrightnessValue();

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

	#region DirectionalLight Specific
	static LightType ILight<DirectionalLight>.SelfType { get; } = LightType.Directional;
	LightType ILight.Type => LightType.Directional;

	/// <summary>
	/// The direction this light travels in; i.e. the direction its rays move, not the direction towards the light source.
	/// </summary>
	/// <remarks>
	/// For a sun overhead this points downward. Because the light has no position, this direction is the only thing that determines how objects are lit and which
	/// way their shadows fall.
	/// </remarks>
	public Direction Direction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetDirectionalLightDirection(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetDirectionalLightDirection(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="Direction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="direction">The new value for <see cref="Direction"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetDirection(Direction direction) => Direction = direction;

	Rotation IOrientedSceneObject.Rotation {
		get => Rotation.FromStartAndEndDirection(DirectionalLightCreationConfig.DefaultInitialDirection, Direction);
		set => Direction = DirectionalLightCreationConfig.DefaultInitialDirection * value;
	}

	Quaternion IOrientedSceneObject.RotationQuaternion {
		get => Rotation.FromStartAndEndDirection(DirectionalLightCreationConfig.DefaultInitialDirection, Direction).ToQuaternion();
		set => Direction = DirectionalLightCreationConfig.DefaultInitialDirection.RotatedBy(value);
	}

	/// <inheritdoc />
	public void RotateBy(Rotation rotation) => Direction *= rotation;
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion) => Direction = Direction.RotatedBy(rotationQuaternion);

	/// <summary>
	/// Configures the visible disc this light draws in the sky, in the way the sun appears as a bright disc rather than merely lighting the scene.
	/// </summary>
	/// <remarks>
	/// The disc is drawn against the scene's backdrop at the point the light comes from, so it is only visible where the backdrop is.
	/// </remarks>
	/// <param name="config">How the disc should look.</param>
	public void SetSunDiscParameters(SunDiscConfig config) => Implementation.SetDirectionalLightSunDiscParameters(Handle, config);

	/// <summary>
	/// Converts an illuminance in lux to the equivalent <see cref="Brightness"/> value.
	/// </summary>
	/// <param name="lux">The illuminance to convert. Negative and non-finite values return <c>0f</c>.</param>
	public static float LuxToBrightness(float lux) {
		if (!lux.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(lux / DefaultLux, MaxBrightness);
	}

	/// <summary>
	/// Converts a <see cref="Brightness"/> value to the illuminance it represents, in lux.
	/// </summary>
	/// <param name="brightness">The brightness to convert. Negative and non-finite values are treated as <c>0f</c>.</param>
	public static float BrightnessToLux(float brightness) {
		return BrightnessToLuxNoClamp(ClampBrightnessToValidRange(brightness));
	}

	internal static float BrightnessToLuxNoClamp(float brightness) {
		return DefaultLux * brightness;
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
	public bool Equals(DirectionalLight other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator ==(DirectionalLight left, DirectionalLight right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator !=(DirectionalLight left, DirectionalLight right) => !left.Equals(right);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator ==(Light left, DirectionalLight right) => right.Equals(left);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator !=(Light left, DirectionalLight right) => !right.Equals(left);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator ==(DirectionalLight left, Light right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(DirectionalLight)"/>
	/// </summary>
	public static bool operator !=(DirectionalLight left, Light right) => !left.Equals(right);
	#endregion
}