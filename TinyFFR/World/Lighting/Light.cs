// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

/// <summary>
/// A light in a scene, of any kind.
/// </summary>
/// <remarks>
/// This is the kind-agnostic view of a light: it exposes everything every light has (colour, brightness, whether it casts shadows) but none of the properties
/// specific to one kind. Use <see cref="As{TLight}"/> (or an explicit cast) to get back to the specific type, or <see cref="Type"/> to find out which kind a light actually is. A
/// <see cref="Light"/> and the specific light it came from refer to the same underlying light, so changes through one are visible through the other and disposing
/// either disposes the light itself.
/// </remarks>
public readonly struct Light : ILight, IDisposable, IEquatable<Light>, IStringSpanNameEnabled {
	readonly ResourceHandle _handle;
	readonly ILightImplProvider _impl;

	internal ILightImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<Light>();
	internal ResourceHandle Handle => IsDisposed ? throw new ObjectDisposedException(nameof(Light)) : _handle;

	internal Light(ResourceHandle handle, ILightImplProvider impl) {
		_handle = handle;
		_impl = impl;
	}

	Light ILight.AsBaseLight() => this;

	/// <inheritdoc />
	public LightType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetType(_handle);
	}

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
	/// How much light this emits, where <c>1f</c> is the default strength for this kind of light.
	/// </summary>
	/// <remarks>
	/// How this relates to a physical unit depends on which kind of light this is; see <see cref="PointLight.Brightness"/>, <see cref="SpotLight.Brightness"/> or
	/// <see cref="DirectionalLight.Brightness"/> for the exact mapping. Negative and non-finite values are treated as <c>0f</c>.
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

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	/// <inheritdoc />
	public override string ToString() => $"{(IsDisposed ? "Light (Disposed)" : $"{Type}Light \"{GetNameAsNewStringObject()}\"")}";

	/// <summary>
	/// Reinterprets this light as a specific kind of light.
	/// </summary>
	/// <remarks>
	/// This is a reinterpretation, not a conversion: the result refers to the same underlying light. Check <see cref="Type"/> first — asking for the wrong kind
	/// throws an exception.
	/// </remarks>
	/// <typeparam name="TLight">The kind of light to reinterpret this as, such as <see cref="PointLight"/>.</typeparam>
	/// <exception cref="InvalidCastException">Thrown if attempting to cast this light as a specific kind of the wrong type.</exception>
	public TLight As<TLight>() where TLight : ILight<TLight> => TLight.FromBaseLight(this);

	internal static void ThrowIfInvalidType(Light input, LightType requiredType) {
		if (input.Type == requiredType) return;
		throw TypeUtils.InvalidCast(input, requiredType, input.Type);
	}

	#region Equality
	/// <inheritdoc />
	public bool Equals(Light other) => _handle == other._handle && ReferenceEquals(_impl, other._impl);
	/// <summary>
	/// Returns whether this light and <paramref name="other"/> refer to the same underlying light.
	/// </summary>
	/// <remarks>
	/// Lets a <see cref="Light"/> be compared against a specific light type without converting either first.
	/// </remarks>
	/// <typeparam name="TLight">The kind of the light being compared against.</typeparam>
	/// <param name="other">The light to compare with this one.</param>
	public bool Equals<TLight>(TLight other) where TLight : ILight => Equals(other.AsBaseLight());
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ILight other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	/// <summary>
	/// <see cref="Equals(Light)"/>
	/// </summary>
	public static bool operator ==(Light left, Light right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(Light)"/>
	/// </summary>
	public static bool operator !=(Light left, Light right) => !left.Equals(right);
	#endregion
}