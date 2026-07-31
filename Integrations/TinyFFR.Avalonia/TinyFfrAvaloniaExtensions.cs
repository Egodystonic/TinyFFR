// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Avalonia.Input;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

public static class TinyFfrAvaloniaExtensions {
	sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, IDisposable DispatcherTimerDisposable, IDisposable? InputSource) : IDisposable {
		public void Dispose() {
			TokenSource.Cancel();
			TokenSource.Dispose();
			Loop.Dispose();
			DispatcherTimerDisposable.Dispose();
			InputSource?.Dispose();
		}
	}

	const int DefaultUiLoopTickRateHz = 60;

	public static IDisposable StartAvaloniaUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = default, ReadOnlySpan<char> name = default) {
		return StartAvaloniaUiLoop(@this, tickCallback, null, tickRateHz, priority, name);
	}

	public static IDisposable StartAvaloniaUiLoop(this ILocalApplicationLoopBuilder @this, InputElement inputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = default, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(tickCallback);
		ArgumentNullException.ThrowIfNull(inputSource);

		var uiInputSource = new UiInputSource(inputSource);
		try {
			return StartAvaloniaUiLoop(
				@this,
				deltaTime => {
					// ReSharper disable AccessToDisposedClosure Closed-over var is only disposed if loop creation fails anyway
					uiInputSource.Iterate();
					tickCallback(deltaTime, uiInputSource.Retriever);
					// ReSharper restore AccessToDisposedClosure
				},
				uiInputSource,
				tickRateHz,
				priority,
				name
			);
		}
		catch {
			uiInputSource.Dispose();
			throw;
		}
	}

	static IDisposable StartAvaloniaUiLoop(ILocalApplicationLoopBuilder builder, Action<TimeSpan> tickCallback, IDisposable? inputSource, int tickRateHz, DispatcherPriority priority, ReadOnlySpan<char> name) {
		if (tickRateHz <= 0) tickRateHz = DefaultUiLoopTickRateHz;

		var loop = builder.CreateLoop(new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = null,
			IterationShouldRefreshGlobalInputStates = false,
			Name = name,
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

		return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, dispatcherTimerDisposable, inputSource);
	}

	public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());
}