// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Numerics;
using Avalonia;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

public static class TinyFfrAvaloniaExtensions {
	sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, IDisposable DispatcherTimerDisposable) : IDisposable {
		public void Dispose() {
			TokenSource.Cancel();
			TokenSource.Dispose();
			Loop.Dispose();
			DispatcherTimerDisposable.Dispose();
		}
	}

	const int DefaultUiLoopTickRateHz = 60;

	public static IDisposable StartAvaloniaUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = default, ReadOnlySpan<char> name = default) {
		if (tickRateHz <= 0) tickRateHz = DefaultUiLoopTickRateHz;
		
		var loop = @this.CreateLoop(new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = null,
			IterationShouldRefreshGlobalInputStates = false,
			Name = name,
			WaitForVSync = false,
			FrameTimingPrecisionBusyWaitTime = TimeSpan.Zero
		});
		var dispatcherTimerCancellationTokenSource = new CancellationTokenSource();
		var stopToken = dispatcherTimerCancellationTokenSource.Token;
		
		var dispatcherTimerDisposable = DispatcherTimer.Run(
			action: () => {
				if (stopToken.IsCancellationRequested) return false;
				tickCallback(loop.IterateOnce());
				return !stopToken.IsCancellationRequested;
			},
			interval: TimeSpan.FromSeconds(1d / tickRateHz),
			priority
		);

		return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, dispatcherTimerDisposable);
	}

	public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());
}