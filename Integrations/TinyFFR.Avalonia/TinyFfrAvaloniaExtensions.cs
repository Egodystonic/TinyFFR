// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Numerics;
using Avalonia;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

public static class TinyFfrAvaloniaExtensions {
	public static Renderer CreateAvaloniaRenderer(this IRendererBuilder @this, Scene scene, Camera camera, ReadOnlySpan<char> name = default) {
		return @this.CreateAvaloniaRenderer(scene, camera, new CameraCreationConfig { Name = name });
	}
	public static Renderer CreateAvaloniaRenderer(this IRendererBuilder @this, Scene scene, Camera camera, in CameraCreationConfig config) {

	}
	
	public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());

	public static IDisposable BeginIteratingOnUiThread(this ApplicationLoop @this) {
		
		DispatcherTimer.Run // TODO
	}
}