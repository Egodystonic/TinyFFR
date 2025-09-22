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
	public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());

	public static void BeginIteratingOnUiThread(this ApplicationLoop @this, Action<TimeSpan> tickAction, CancellationToken stopToken, DispatcherPriority priority = default) {
		DispatcherTimer.Run(
			action: () => {
				if (@this.TryIterateOnce(out var dt)) tickAction(dt);
				return !stopToken.IsCancellationRequested;
			},
			interval: @this.DesiredIterationInterval,
			priority
		);
	}
}