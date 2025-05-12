// Created on 2024-08-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.World;

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

	public LightType Type {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetType(_handle);
	}

	public ColorVect Color {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetColor(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetColor(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColor(ColorVect color) => Color = color;

	public Angle ColorHue {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Hue;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithHue(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorHue(Angle hue) => ColorHue = hue;

	public float ColorSaturation {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Saturation;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithSaturation(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorSaturation(float saturation) => ColorSaturation = saturation;

	public float ColorLightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Color.Lightness;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Color = Color.WithLightness(value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetColorLightness(float lightness) => ColorLightness = lightness;

	public float Brightness {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetUniversalBrightness(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetUniversalBrightness(_handle, value);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetBrightness(float brightness) => Brightness = brightness;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	public void AdjustColorHueBy(Angle adjustment) => Color = Color.WithHueAdjustedBy(adjustment);
	public void AdjustColorSaturationBy(float adjustment) => Color = Color.WithSaturationAdjustedBy(adjustment);
	public void AdjustColorLightnessBy(float adjustment) => Color = Color.WithLightnessAdjustedBy(adjustment);
	public void AdjustBrightnessBy(float adjustment) => Implementation.AdjustBrightnessBy(_handle, adjustment);
	public void ScaleBrightnessBy(float scalar) => Implementation.ScaleBrightnessBy(_handle, scalar);

	#region Disposal
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	public override string ToString() => $"{(IsDisposed ? "Light (Disposed)" : $"{Type} \"{GetNameAsNewStringObject()}\"")}";

	public TLight As<TLight>() where TLight : ILight<TLight> => TLight.FromBaseLight(this);

	internal static void ThrowIfInvalidType(Light input, LightType requiredType) {
		if (input.Type == requiredType) return;
		throw TypeUtils.InvalidCast(input, requiredType, input.Type);
	}

	#region Equality
	public bool Equals(Light other) => _handle == other._handle && _impl.Equals(other._impl);
	public bool Equals<TLight>(TLight other) where TLight : ILight => Equals(other.AsBaseLight());
	public override bool Equals(object? obj) => obj is ILight other && Equals(other);
	public override int GetHashCode() => HashCode.Combine((UIntPtr) _handle, _impl);
	public static bool operator ==(Light left, Light right) => left.Equals(right);
	public static bool operator !=(Light left, Light right) => !left.Equals(right);
	#endregion
}