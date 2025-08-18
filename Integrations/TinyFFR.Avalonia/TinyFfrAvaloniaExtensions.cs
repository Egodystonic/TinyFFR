// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.Numerics;
using Avalonia;
using Egodystonic.TinyFFR;

namespace TinyFFR.Avalonia;

public static class TinyFfrAvaloniaExtensions {
	public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());
}