// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A preset brightness for a <see cref="SpotLight"/>, named after the real-world light source it matches.
/// </summary>
/// <remarks>
/// <para>
/// Use with <see cref="SpotLight.SetBrightness(SpotLightBrightnessPreset)"/>, or <see cref="SpotLightBrightnessExtensions.ToBrightnessValue"/> to get the equivalent
/// <see cref="SpotLight.Brightness"/> value (for example, to pass when creating a light).
/// </para>
/// <para>
/// Each preset is defined by how intensely the real light source shines along its beam (its luminous intensity, in candela; see <see cref="SpotLightBrightnessExtensions.ToCandela"/>),
/// which is how real spot lights such as flashlights and headlights are rated. The centre of the light's cone therefore appears as bright as the real thing.
/// </para>
/// <para>
/// The presets are calibrated for standard spot lights. A spot light created with <see cref="SpotLightCreationConfig.IsHighQuality"/> concentrates its whole output
/// in to its cone, so appears brighter the narrower its cone is.
/// </para>
/// </remarks>
public enum SpotLightBrightnessPreset {
	/// <summary>
	/// A typical handheld flashlight (a beam intensity of 3,000 candela). This is the default brightness of a <see cref="SpotLight"/>.
	/// </summary>
	FlashlightTypical = 0,
	/// <summary>
	/// A small or weak flashlight, such as one on a keyring (a beam intensity of 500 candela).
	/// </summary>
	FlashlightDim,
	/// <summary>
	/// A powerful flashlight (a beam intensity of 20,000 candela).
	/// </summary>
	FlashlightBright,
	/// <summary>
	/// A car headlight on full beam (a beam intensity of 30,000 candela).
	/// </summary>
	CarHeadlight,
	/// <summary>
	/// A theatre or stage spotlight (a beam intensity of 100,000 candela).
	/// </summary>
	StageSpotlight,
	/// <summary>
	/// A searchlight (a beam intensity of 1,000,000 candela).
	/// </summary>
	Searchlight,
	/// <summary>
	/// A desk or reading lamp (a beam intensity of 150 candela).
	/// </summary>
	DeskLamp
}

/// <summary>
/// Extension methods for <see cref="SpotLightBrightnessPreset"/>.
/// </summary>
public static class SpotLightBrightnessExtensions {
	extension(SpotLightBrightnessPreset @this) {
		/// <summary>
		/// Returns the beam intensity, in candela, of the real light source this preset represents (see <see cref="SpotLight.BrightnessCandela"/>).
		/// </summary>
		public float ToCandela() {
			return @this switch {
				SpotLightBrightnessPreset.FlashlightDim => 500f,
				SpotLightBrightnessPreset.FlashlightBright => 20_000f,
				SpotLightBrightnessPreset.CarHeadlight => 30_000f,
				SpotLightBrightnessPreset.StageSpotlight => 100_000f,
				SpotLightBrightnessPreset.Searchlight => 1_000_000f,
				SpotLightBrightnessPreset.DeskLamp => 150f,
				_ /* FlashlightTypical */ => SpotLight.DefaultCandela
			};
		}

		/// <summary>
		/// Returns the <see cref="SpotLight.Brightness"/> value that this preset represents.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float ToBrightnessValue() => SpotLight.CandelaToBrightness(@this.ToCandela());
	}
}


/// <summary>
/// A light that radiates from a single point but is confined to a cone, like a torch or a stage spotlight. Created via the factory's <see cref="ILightBuilder"/>.
/// </summary>
/// <remarks>
/// A spot light is a <see cref="PointLight"/> with a direction and a cone: <see cref="ConeDirection"/> aims it, <see cref="ConeAngle"/> sets how wide the pool of
/// light is, and <see cref="IntenseBeamAngle"/> sets how sharply the light fades towards the cone's edge.
/// </remarks>
public readonly struct SpotLight : ILight<SpotLight>, IPositionedSceneObject, IOrientedSceneObject {
	static SceneObjectType ISceneObject.SceneObjectType { get; } = SceneObjectType.SpotLight;

	/// <summary>
	/// The largest permitted <see cref="Brightness"/>: <c>1E+15f</c>.
	/// </summary>
	public const float MaxBrightness = 1E+15f;
	/// <summary>
	/// The beam intensity, in candela, of a spot light at a <see cref="Brightness"/> of <c>1f</c>: <c>3,000</c>, that of a typical handheld flashlight
	/// (see <see cref="SpotLightBrightnessPreset.FlashlightTypical"/>).
	/// </summary>
	public const float DefaultCandela = 3_000f;
	/// <summary>
	/// The narrowest permitted <see cref="ConeAngle"/>: <c>1°</c>.
	/// </summary>
	public static readonly Angle MinConeAngle = 1f;
	/// <summary>
	/// The widest permitted <see cref="ConeAngle"/>: <c>180°</c>.
	/// </summary>
	public static readonly Angle MaxConeAngle = 180f;

	readonly ResourceHandle<SpotLight> _handle;
	readonly ILightImplProvider _impl;

	internal ResourceHandle<SpotLight> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(SpotLight)) : _handle;
	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<SpotLight>();

	ILightImplProvider IResource<SpotLight, ILightImplProvider>.Implementation => Implementation;
	ResourceHandle<SpotLight> IResource<SpotLight>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	internal SpotLight(ResourceHandle<SpotLight> handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	static SpotLight IResource<SpotLight>.CreateFromHandleAndImpl(ResourceHandle<SpotLight> handle, IResourceImplProvider impl) {
		return new SpotLight(handle, impl as ILightImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<SpotLight> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<SpotLight> IResource<SpotLight>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	#region Light Type Casting
	/// <inheritdoc />
	public Light AsBaseLight() => new(_handle, _impl);

	/// <summary>
	/// Reinterprets this spot light as a kind-agnostic <see cref="Light"/>.
	/// </summary>
	/// <param name="operand">The spot light to reinterpret.</param>
	public static implicit operator Light(SpotLight operand) => operand.AsBaseLight();
	/// <summary>
	/// Reinterprets a kind-agnostic <see cref="Light"/> as a spot light.
	/// </summary>
	/// <remarks>
	/// Explicit because it is only meaningful when the light really is a spot light; check <see cref="Light.Type"/> first.
	/// </remarks>
	/// <param name="operand">The light to reinterpret.</param>
	public static explicit operator SpotLight(Light operand) => operand.As<SpotLight>();
	static SpotLight ILight<SpotLight>.FromBaseLight(Light l) {
		Light.ThrowIfInvalidType(l, LightType.Spot);
		return new((ResourceHandle<SpotLight>) l.Handle, l.Implementation);
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
	/// You can also use <see cref="SetBrightness(SpotLightBrightnessPreset)"/> to set a value using a preset.
	/// </remarks>
	/// <seealso cref="BrightnessCandela"/>
	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUniversalBrightness(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUniversalBrightness(_handle, value);
	}
	/// <summary>
	/// How intensely this shines along the centre of its cone, in candela (the unit real spot lights such as flashlights and headlights are rated in). This property is
	/// offered as an alternative to the unitless <see cref="Brightness"/> for workflows that prefer real-world light setups.
	/// </summary>
	/// <remarks>
	/// This describes a standard spot light. A spot light created with <see cref="SpotLightCreationConfig.IsHighQuality"/> concentrates its whole output in to its cone,
	/// so shines more intensely than this value, increasingly so the narrower its cone is.
	/// </remarks>
	public float BrightnessCandela {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => BrightnessToCandela(Brightness);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Brightness = CandelaToBrightness(value);
	}
	/// <summary>
	/// Sets <see cref="Brightness"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="brightness">The new value for <see cref="Brightness"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Brightness = brightness;
	/// <summary>
	/// Sets <see cref="Brightness"/> to match the given real-world light source.
	/// </summary>
	/// <param name="preset">The preset to apply.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="preset"/> is not a defined <see cref="SpotLightBrightnessPreset"/> value.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void SetBrightness(SpotLightBrightnessPreset preset) => Brightness = preset.ToBrightnessValue();

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

	#region SpotLight Specific
	static LightType ILight<SpotLight>.SelfType { get; } = LightType.Spot;
	LightType ILight.Type => LightType.Spot;

	/// <inheritdoc />
	public Location Position {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightPosition(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightPosition(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="Position"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="position">The new value for <see cref="Position"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetPosition(Location position) => Position = position;

	/// <summary>
	/// How far down its cone this light reaches, in metres; beyond this distance it contributes no light at all.
	/// </summary>
	/// <remarks>
	/// A real light never quite stops, but continuing to calculate a contribution too faint to see is wasted work, so lights are given a finite reach. Setting this
	/// too small for the light's brightness produces a visible edge where the light cuts off.
	/// </remarks>
	public float MaxIlluminationDistance {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightMaxIlluminationDistance(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightMaxIlluminationDistance(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="MaxIlluminationDistance"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="distance">The new value for <see cref="MaxIlluminationDistance"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetMaxIlluminationDistance(float distance) => MaxIlluminationDistance = distance;

	/// <summary>
	/// Which way this light points; i.e. the direction its cone of light travels along.
	/// </summary>
	public Direction ConeDirection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightConeDirection(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightConeDirection(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="ConeDirection"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="direction">The new value for <see cref="ConeDirection"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetConeDirection(Direction direction) => ConeDirection = direction;

	/// <summary>
	/// How wide this light's cone is. Clamped to between <see cref="MinConeAngle"/> and <see cref="MaxConeAngle"/>.
	/// </summary>
	/// <remarks>
	/// This is the full width of the cone, not the angle from its centre to its edge, so <c>90°</c> describes a cone spreading <c>45°</c> in every direction from
	/// <see cref="ConeDirection"/>. Nothing outside the cone receives any light from this source.
	/// </remarks>
	public Angle ConeAngle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightConeAngle(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightConeAngle(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="ConeAngle"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="angle">The new value for <see cref="ConeAngle"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetConeAngle(Angle angle) => ConeAngle = angle;

	/// <summary>
	/// How wide the fully-lit centre of this light's cone is; between this angle and <see cref="ConeAngle"/> the light fades out to nothing.
	/// </summary>
	/// <remarks>
	/// This is what controls how hard or soft the edge of the pool of light looks. Setting it close to <see cref="ConeAngle"/> gives a sharply-defined circle, as a
	/// theatre spotlight has; setting it much smaller gives a soft glow that fades gradually outward. Like <see cref="ConeAngle"/> it is a full width rather than a
	/// half-angle.
	/// </remarks>
	public Angle IntenseBeamAngle {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetSpotLightIntenseBeamAngle(Handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetSpotLightIntenseBeamAngle(Handle, value);
	}
	/// <summary>
	/// Sets <see cref="IntenseBeamAngle"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="angle">The new value for <see cref="IntenseBeamAngle"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetIntenseBeamAngle(Angle angle) => IntenseBeamAngle = angle;

	Rotation IOrientedSceneObject.Rotation {
		get => Rotation.FromStartAndEndDirection(SpotLightCreationConfig.DefaultInitialConeDirection, ConeDirection);
		set => ConeDirection = SpotLightCreationConfig.DefaultInitialConeDirection * value;
	}

	Quaternion IOrientedSceneObject.RotationQuaternion {
		get => Rotation.FromStartAndEndDirection(SpotLightCreationConfig.DefaultInitialConeDirection, ConeDirection).ToQuaternion();
		set => ConeDirection = SpotLightCreationConfig.DefaultInitialConeDirection.RotatedBy(value);
	}

	/// <inheritdoc />
	public void MoveBy(Vect translation) => Position += translation;
	/// <inheritdoc />
	public void RotateBy(Rotation rotation) => ConeDirection *= rotation;
	/// <inheritdoc />
	public void RotateBy(Quaternion rotationQuaternion) => ConeDirection = ConeDirection.RotatedBy(rotationQuaternion);

	/// <summary>
	/// Converts a beam intensity in candela to the equivalent <see cref="Brightness"/> value.
	/// </summary>
	/// <param name="candela">The beam intensity to convert. Negative and non-finite values return <c>0f</c>.</param>
	public static float CandelaToBrightness(float candela) {
		if (!candela.IsNonNegativeAndFinite()) return 0f;
		return Single.Min(MathF.Sqrt(candela / DefaultCandela), MaxBrightness);
	}

	/// <summary>
	/// Converts a <see cref="Brightness"/> value to the beam intensity it represents, in candela.
	/// </summary>
	/// <param name="brightness">The brightness to convert. Negative and non-finite values are treated as <c>0f</c>.</param>
	public static float BrightnessToCandela(float brightness) {
		var clamped = ClampBrightnessToValidRange(brightness);
		return DefaultCandela * clamped * clamped;
	}

	internal static float BrightnessToNativeLumensNoClamp(float brightness) {
		return DefaultCandela * brightness * brightness * 4f * MathF.PI;
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
	public bool Equals(SpotLight other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ILight other && AsBaseLight().Equals(other.AsBaseLight());
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator ==(SpotLight left, SpotLight right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator !=(SpotLight left, SpotLight right) => !left.Equals(right);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator ==(Light left, SpotLight right) => right.Equals(left);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator !=(Light left, SpotLight right) => !right.Equals(left);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator ==(SpotLight left, Light right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(SpotLight)"/>
	/// </summary>
	public static bool operator !=(SpotLight left, Light right) => !left.Equals(right);
	#endregion
}