using System;
using System.Numerics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Local;
using Timer = System.Windows.Forms.Timer;

namespace Egodystonic.TinyFFR.WinForms {
	public static class TinyFfrWinFormsExtensions {
		sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, Timer Timer) : IDisposable {
			public void Dispose() {
				TokenSource.Cancel();
				TokenSource.Dispose();
				Loop.Dispose();
				Timer.Stop();
				Timer.Dispose();
			}
		}

		const int DefaultUiLoopTickRateHz = 60;

		public static IDisposable StartWinFormsUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, ReadOnlySpan<char> name = default) {
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

			var timer = new Timer();
			timer.Tick += (_, __) => {
				if (stopToken.IsCancellationRequested) return;
				tickCallback(loop.IterateOnce());
			};
			timer.Interval = (int) TimeSpan.FromSeconds(1d / tickRateHz).TotalMilliseconds;
			timer.Start();

			return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, timer);
		}

		public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
		public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> {
			var thisCast = @this.Cast<int>();
			return new Size(thisCast.X, thisCast.Y);
		}
	}
}
