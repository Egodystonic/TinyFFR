using System;
using System.Numerics;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.WinForms.Input;
using Timer = System.Windows.Forms.Timer;

namespace Egodystonic.TinyFFR.WinForms {
	public static class TinyFfrWinFormsExtensions {
		sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, Timer Timer, IDisposable? InputSource) : IDisposable {
			public void Dispose() {
				TokenSource.Cancel();
				TokenSource.Dispose();
				Loop.Dispose();
				Timer.Stop();
				Timer.Dispose();
				InputSource?.Dispose();
			}
		}

		const int DefaultUiLoopTickRateHz = 60;

		public static IDisposable StartWinFormsUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, ReadOnlySpan<char> name = default) {
			return StartWinFormsUiLoop(@this, tickCallback, null, tickRateHz, name);
		}

		public static IDisposable StartWinFormsUiLoop(this ILocalApplicationLoopBuilder @this, Control inputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, ReadOnlySpan<char> name = default) {
			ArgumentNullException.ThrowIfNull(tickCallback);
			ArgumentNullException.ThrowIfNull(inputSource);

			var uiInputSource = new UiInputSource(inputSource);
			try {
				return StartWinFormsUiLoop(
					@this,
					deltaTime => {
						// ReSharper disable AccessToDisposedClosure Closed-over var is only disposed if loop creation fails anyway
						uiInputSource.Iterate();
						tickCallback(deltaTime, uiInputSource.Retriever);
						// ReSharper restore AccessToDisposedClosure
					},
					uiInputSource,
					tickRateHz,
					name
				);
			}
			catch {
				uiInputSource.Dispose();
				throw;
			}
		}

		static IDisposable StartWinFormsUiLoop(ILocalApplicationLoopBuilder builder, Action<TimeSpan> tickCallback, IDisposable? inputSource, int tickRateHz, ReadOnlySpan<char> name) {
			if (tickRateHz <= 0) tickRateHz = DefaultUiLoopTickRateHz;

			var loop = builder.CreateLoop(new LocalApplicationLoopCreationConfig {
				FrameRateCapHz = null,
				IterationShouldRefreshGlobalInputStates = false,
				Name = name,
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

			return new UiLoopCompositeDisposable(loop, dispatcherTimerCancellationTokenSource, timer, inputSource);
		}

		public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
		public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> {
			var thisCast = @this.Cast<int>();
			return new Size(thisCast.X, thisCast.Y);
		}
	}
}
