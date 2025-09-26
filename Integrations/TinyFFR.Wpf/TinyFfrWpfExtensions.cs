using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;

namespace Egodystonic.TinyFFR.Wpf {
	public static class TinyFfrWpfExtensions {
		sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, DispatcherTimer DispatcherTimer) : IDisposable {
			public void Dispose() {
				TokenSource.Cancel();
				TokenSource.Dispose();
				Loop.Dispose();
				DispatcherTimer.Stop();
			}
		}

		const int DefaultUiLoopTickRateHz = 60;

		public static IDisposable StartWpfUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = DispatcherPriority.Normal, ReadOnlySpan<char> name = default) {
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

			var dispatcherTimerDisposable = new DispatcherTimer(
				interval: TimeSpan.FromSeconds(1d / tickRateHz),
				priority,
				callback: (_, __) => {
					if (stopToken.IsCancellationRequested) return;
					tickCallback(loop.IterateOnce());
				},
				dispatcher: Application.Current.Dispatcher
			);

			return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, dispatcherTimerDisposable);
		}


		public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
		public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> {
			var thisAsV2 = @this.ToVector2();
			return new Size(thisAsV2.X, thisAsV2.Y);
		}
	}
}
