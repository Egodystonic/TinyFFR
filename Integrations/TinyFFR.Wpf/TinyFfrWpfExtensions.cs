using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Threading;
using Egodystonic.TinyFFR.Wpf.Input;

namespace Egodystonic.TinyFFR.Wpf {
	/// <summary>
	/// Extension types specifically for integrating TinyFFR and WPF.
	/// </summary>
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

		/// <summary>
		/// Starts a render/animation loop that ticks on the WPF dispatcher.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This schedules your tick on WPF's own dispatcher loop rather than creating a TinyFFR application loop that drives itself, which is
		/// what keeps everything on the UI thread and stops TinyFFR's usual input polling from stealing events the framework needs.
		/// </para>
		/// <para>
		/// The callback is invoked on the UI thread, so it is safe to touch both your UI and your TinyFFR objects inside it. Render
		/// your scene there, as you would inside a standalone application loop.
		/// </para>
		/// <para>
		/// Dispose the returned object to stop the loop.
		/// </para>
		/// </remarks>
		/// <param name="this">The application loop builder to create the underlying loop with.</param>
		/// <param name="tickCallback">The callback to invoke on the UI thread each tick. Its argument is the time elapsed since the previous tick.</param>
		/// <param name="tickRateHz">How many times per second to tick. Defaults to <c>60</c>. The framework's own compositor and dispatcher may cap the rate achieved, and the interval between ticks will vary more than in a standalone application. Values of zero or less are treated as the default.</param>
		/// <param name="priority">The dispatcher priority to schedule the tick at.</param>
		/// <param name="name">The name to give the underlying application loop resource. May be left empty.</param>
		public static IDisposable StartWpfUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = DispatcherPriority.Normal, ReadOnlySpan<char> name = default) {
			return StartWpfUiLoop(@this, tickCallback, null, tickRateHz, priority, name);
		}

		/// <summary>
		/// Starts a render/animation loop that ticks on the WPF dispatcher, supplying TinyFFR input gathered from the given element.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This is the overload to use where you want TinyFFR's own input abstraction. The retriever handed to your callback is fed
		/// from WPF's input events rather than from TinyFFR's usual polling, so the same input-handling code you would write for a
		/// standalone application works unchanged.
		/// </para>
		/// <para>
		/// Input is scoped to the element you nominate: keyboard events are observed only whilst it has focus, and mouse buttons and
		/// the wheel whilst the pointer is over it. Keys and buttons are released automatically when focus or capture is lost, so
		/// switching away from your application never leaves a key stuck down.
		/// </para>
		/// <para>
		/// Game controllers are not supported under UI framework integration, and there is no equivalent of locking the cursor.
		/// </para>
		/// <para>
		/// Dispose the returned object to stop the loop; doing so also unsubscribes from the element's events.
		/// </para>
		/// </remarks>
		/// <param name="this">The application loop builder to create the underlying loop with.</param>
		/// <param name="inputSource">The element whose input events are observed. Usually the scene view itself, though a containing window may be passed to widen the scope. Note that mouse positions are reported relative to this element, so it must be the scene view for those to line up with the rendered image.</param>
		/// <param name="tickCallback">The callback to invoke on the UI thread each tick. Its arguments are the time elapsed since the previous tick and the input accumulated over that time.</param>
		/// <param name="tickRateHz">How many times per second to tick. Defaults to <c>60</c>. The framework's own compositor and dispatcher may cap the rate achieved, and the interval between ticks will vary more than in a standalone application. Values of zero or less are treated as the default.</param>
		/// <param name="priority">The dispatcher priority to schedule the tick at.</param>
		/// <param name="name">The name to give the underlying application loop resource. May be left empty.</param>
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


		/// <summary>
		/// Converts a framework size to an <see cref="XYPair{T}"/>.
		/// </summary>
		/// <param name="this">The size to convert.</param>
		public static XYPair<double> AsXyPair(this Size @this) => new(@this.Width, @this.Height);
		/// <summary>
		/// Converts an <see cref="XYPair{T}"/> to a framework size.
		/// </summary>
		/// <typeparam name="T">The numeric type of the pair's components.</typeparam>
		/// <param name="this">The pair to convert.</param>
		public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> {
			var thisAsV2 = @this.ToVector2();
			return new Size(thisAsV2.X, thisAsV2.Y);
		}
	}
}
