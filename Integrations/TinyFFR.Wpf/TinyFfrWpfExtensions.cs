using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;
using Egodystonic.TinyFFR.Wpf.Input;

namespace Egodystonic.TinyFFR.Wpf {
	public static class TinyFfrWpfExtensions {
		sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, DispatcherTimer DispatcherTimer, IDisposable? InputSource) : IDisposable {
			public void Dispose() {
				TokenSource.Cancel();
				TokenSource.Dispose();
				Loop.Dispose();
				DispatcherTimer.Stop();
				InputSource?.Dispose();
			}
		}

		const int DefaultUiLoopTickRateHz = 60;

		public static IDisposable StartWpfUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = DispatcherPriority.Normal, ReadOnlySpan<char> name = default) {
			return StartWpfUiLoop(@this, tickCallback, null, tickRateHz, priority, name);
		}

		public static IDisposable StartWpfUiLoop(this ILocalApplicationLoopBuilder @this, UIElement inputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = DispatcherPriority.Normal, ReadOnlySpan<char> name = default) {
			ArgumentNullException.ThrowIfNull(tickCallback);
			ArgumentNullException.ThrowIfNull(inputSource);

			var uiInputSource = new UiInputSource(inputSource);
			try {
				return StartWpfUiLoop(
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

		static IDisposable StartWpfUiLoop(ILocalApplicationLoopBuilder builder, Action<TimeSpan> tickCallback, IDisposable? inputSource, int tickRateHz, DispatcherPriority priority, ReadOnlySpan<char> name) {
			if (tickRateHz <= 0) tickRateHz = DefaultUiLoopTickRateHz;

			var loop = builder.CreateLoop(new LocalApplicationLoopCreationConfig {
				FrameRateCapHz = null,
				IterationShouldRefreshGlobalInputStates = false,
				Name = name,
				FrameTimingPrecisionBusyWaitTime = TimeSpan.Zero
			});
			var dispatcherTimerCancellationTokenSource = new CancellationTokenSource();
			var stopToken = dispatcherTimerCancellationTokenSource.Token;

			var dispatcherTimerDisposable = new DispatcherTimer(
				interval: TimeSpan.FromSeconds(1d / tickRateHz),
				priority,
				callback: (_, __) => {
					if (stopToken.IsCancellationRequested) return;
					tickCallback(loop.IterateOnce());
				},
				dispatcher: Application.Current.Dispatcher
			);

			return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, dispatcherTimerDisposable, inputSource);
		}


		public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
		public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> {
			var thisAsV2 = @this.ToVector2();
			return new Size(thisAsV2.X, thisAsV2.Y);
		}
	}
}
