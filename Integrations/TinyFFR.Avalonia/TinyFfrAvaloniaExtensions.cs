// Created on 2025-08-17 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
using Egodystonic.TinyFFR.Avalonia.Input;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.World;

namespace Egodystonic.TinyFFR.Avalonia;

/// <summary>
/// Extension types specifically for integrating TinyFFR and Avalonia.
/// </summary>
public static class TinyFfrAvaloniaExtensions {
	sealed record UiLoopCompositeDisposable(ApplicationLoop Loop, CancellationTokenSource TokenSource, IDisposable TickSourceDisposable, IDisposable? InputSource) : IDisposable {
		public void Dispose() {
			TokenSource.Cancel();
			TokenSource.Dispose();
			Loop.Dispose();
			TickSourceDisposable.Dispose();
			InputSource?.Dispose();
		}
	}

	sealed class AnimationFrameTickSource : IDisposable {
		static readonly TimeSpan TopLevelUnavailableRetryInterval = TimeSpan.FromMilliseconds(16d);
		readonly Func<TopLevel?> _topLevelResolver;
		readonly Action _tick;
		readonly Action<TimeSpan> _onAnimationFrame;
		readonly Action _retryScheduling;
		readonly Visual? _attachmentSource;
		TimeSpan? _lastHandledFrameTime;
		bool _frameRequestPending;
		bool _retryPending;
		bool _isDisposed;

		public AnimationFrameTickSource(Func<TopLevel?> topLevelResolver, Visual? attachmentSource, Action tick) {
			_topLevelResolver = topLevelResolver;
			_attachmentSource = attachmentSource;
			_tick = tick;
			_onAnimationFrame = OnAnimationFrame;
			_retryScheduling = RetryScheduling;
			if (_attachmentSource != null) {
				_attachmentSource.AttachedToVisualTree += HandleAttachedToVisualTree;
				_attachmentSource.DetachedFromVisualTree += HandleDetachedFromVisualTree;
			}
			ScheduleNextFrame();
		}

		void HandleAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => ScheduleNextFrame();
		void HandleDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e) => _frameRequestPending = false;

		void RetryScheduling() {
			_retryPending = false;
			ScheduleNextFrame();
		}

		void ScheduleNextFrame() {
			if (_isDisposed || _frameRequestPending) return;

			var topLevel = _topLevelResolver();
			if (topLevel == null) {
				if (_retryPending) return;
				_retryPending = true;
				DispatcherTimer.RunOnce(_retryScheduling, TopLevelUnavailableRetryInterval);
				return;
			}

			_frameRequestPending = true;
			topLevel.RequestAnimationFrame(_onAnimationFrame);
		}

		void OnAnimationFrame(TimeSpan frameTime) {
			if (_isDisposed || _lastHandledFrameTime == frameTime) return;
			_lastHandledFrameTime = frameTime;
			_frameRequestPending = false;
			_tick();
			ScheduleNextFrame();
		}

		public void Dispose() {
			if (_isDisposed) return;
			_isDisposed = true;
			if (_attachmentSource != null) {
				_attachmentSource.AttachedToVisualTree -= HandleAttachedToVisualTree;
				_attachmentSource.DetachedFromVisualTree -= HandleDetachedFromVisualTree;
			}
		}
	}

	const int DefaultUiLoopTickRateHz = 60;

	/// <summary>
	/// Starts a render/animation loop that ticks from a timer on the Avalonia dispatcher.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the timer-driven alternative to <c>StartAvaloniaUiLoop()</c>, which ticks once per Avalonia animation frame instead and is usually the better choice.
	/// Use this method where you specifically want a fixed tick rate that is independent of the display's refresh rate.
	/// </para>
	/// <para>
	/// This schedules your tick on Avalonia's own dispatcher loop rather than creating a TinyFFR application loop that drives itself, which is
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
	public static IDisposable StartAvaloniaUiLoopViaDispatcher(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = default, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(tickCallback);
		return StartAvaloniaDispatcherLoop(@this, tickCallback, null, tickRateHz, priority, name);
	}

	/// <summary>
	/// Starts a render/animation loop that ticks from a timer on the Avalonia dispatcher, supplying TinyFFR input gathered from the given element.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the timer-driven alternative to <c>StartAvaloniaUiLoop()</c>, which ticks once per Avalonia animation frame instead and is usually the better choice.
	/// Use this method where you specifically want a fixed tick rate that is independent of the display's refresh rate.
	/// </para>
	/// <para>
	/// This is the overload to use where you want TinyFFR's own input abstraction. The retriever handed to your callback is fed
	/// from Avalonia's input events rather than from TinyFFR's usual polling, so the same input-handling code you would write for a
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
	public static IDisposable StartAvaloniaUiLoopViaDispatcher(this ILocalApplicationLoopBuilder @this, InputElement inputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback, int tickRateHz = DefaultUiLoopTickRateHz, DispatcherPriority priority = default, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(tickCallback);
		ArgumentNullException.ThrowIfNull(inputSource);
		var uiInputSource = new UiInputSource(inputSource);
		try {
			return StartAvaloniaDispatcherLoop(@this, CreateUiInputTickCallback(uiInputSource, tickCallback), uiInputSource, tickRateHz, priority, name);
		}
		catch {
			uiInputSource.Dispose();
			throw;
		}
	}

	/// <summary>
	/// Starts a render/animation loop that ticks once per Avalonia animation frame.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This ticks once per Avalonia animation frame (<c>TopLevel.RequestAnimationFrame</c>), i.e. at the display's refresh rate, rather than from a timer. It drives a TinyFFR application loop from that clock
	/// rather than letting the loop drive itself, which is what keeps everything on the UI thread and stops TinyFFR's usual input polling from
	/// stealing events the framework needs. To tick from a fixed-rate timer instead, use <c>StartAvaloniaUiLoopViaDispatcher()</c>.
	/// </para>
	/// <para>
	/// The callback is invoked on the UI thread, so it is safe to touch both your UI and your TinyFFR objects inside it. Render
	/// your scene there, as you would inside a standalone application loop.
	/// </para>
	/// <para>
	/// If <paramref name="frameRateCapHz"/> is set, ticks that arrive before the next one is due are skipped. Because ticks can only happen on display frames, the
	/// achieved rate is the refresh rate divided by a whole number: for example a cap of <c>100</c> on a 144Hz display results in roughly 72 ticks per second.
	/// </para>
	/// <para>
	/// Dispose the returned object to stop the loop.
	/// </para>
	/// </remarks>
	/// <param name="this">The application loop builder to create the underlying loop with.</param>
	/// <param name="tickCallback">The callback to invoke on the UI thread each tick. Its argument is the time elapsed since the previous tick.</param>
	/// <param name="frameRateCapHz">An optional maximum number of ticks per second, or <see langword="null"/> (the default) to tick on every Avalonia animation frame. Must be positive if set.</param>
	/// <param name="name">The name to give the underlying application loop resource. May be left empty.</param>
	public static IDisposable StartAvaloniaUiLoop(this ILocalApplicationLoopBuilder @this, Action<TimeSpan> tickCallback, int? frameRateCapHz = null, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(tickCallback);
		return StartAvaloniaAnimationFrameLoop(@this, tickCallback, null, null, frameRateCapHz, name);
	}

	/// <summary>
	/// Starts a render/animation loop that ticks once per Avalonia animation frame, supplying TinyFFR input gathered from the given element.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This ticks once per Avalonia animation frame (<c>TopLevel.RequestAnimationFrame</c>), i.e. at the display's refresh rate, rather than from a timer. It drives a TinyFFR application loop from that clock
	/// rather than letting the loop drive itself, which is what keeps everything on the UI thread and stops TinyFFR's usual input polling from
	/// stealing events the framework needs. To tick from a fixed-rate timer instead, use <c>StartAvaloniaUiLoopViaDispatcher()</c>.
	/// </para>
	/// <para>
	/// The callback is invoked on the UI thread, so it is safe to touch both your UI and your TinyFFR objects inside it. Render
	/// your scene there, as you would inside a standalone application loop.
	/// </para>
	/// <para>
	/// If <paramref name="frameRateCapHz"/> is set, ticks that arrive before the next one is due are skipped. Because ticks can only happen on display frames, the
	/// achieved rate is the refresh rate divided by a whole number: for example a cap of <c>100</c> on a 144Hz display results in roughly 72 ticks per second.
	/// </para>
	/// <para>
	/// The retriever handed to your callback is fed from Avalonia's input events rather than from TinyFFR's usual polling, so the same input-handling code you
	/// would write for a standalone application works unchanged. Input is scoped to the element you nominate: keyboard events are observed only whilst it has
	/// focus, and mouse buttons and the wheel whilst the pointer is over it. Game controllers are not supported under UI framework integration, and there is no
	/// equivalent of locking the cursor.
	/// </para>
	/// <para>
	/// Dispose the returned object to stop the loop; doing so also unsubscribes from the element's events.
	/// </para>
	/// </remarks>
	/// <param name="this">The application loop builder to create the underlying loop with.</param>
	/// <param name="inputSource">The element whose input events are observed. Usually the scene view itself, though a containing window may be passed to widen the scope. Note that mouse positions are reported relative to this element, so it must be the scene view for those to line up with the rendered image.</param>
	/// <param name="tickCallback">The callback to invoke on the UI thread each tick. Its arguments are the time elapsed since the previous tick and the input accumulated over that time.</param>
	/// <param name="frameRateCapHz">An optional maximum number of ticks per second, or <see langword="null"/> (the default) to tick on every Avalonia animation frame. Must be positive if set.</param>
	/// <param name="name">The name to give the underlying application loop resource. May be left empty.</param>
	public static IDisposable StartAvaloniaUiLoop(this ILocalApplicationLoopBuilder @this, InputElement inputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback, int? frameRateCapHz = null, ReadOnlySpan<char> name = default) {
		ArgumentNullException.ThrowIfNull(tickCallback);
		ArgumentNullException.ThrowIfNull(inputSource);
		var uiInputSource = new UiInputSource(inputSource);
		try {
			return StartAvaloniaAnimationFrameLoop(@this, CreateUiInputTickCallback(uiInputSource, tickCallback), uiInputSource, inputSource, frameRateCapHz, name);
		}
		catch {
			uiInputSource.Dispose();
			throw;
		}
	}

	static Action<TimeSpan> CreateUiInputTickCallback(UiInputSource uiInputSource, Action<TimeSpan, ILatestInputRetriever> tickCallback) {
		return deltaTime => {
			uiInputSource.Iterate();
			tickCallback(deltaTime, uiInputSource.Retriever);
		};
	}

	static ApplicationLoop CreateUiDrivenLoop(ILocalApplicationLoopBuilder builder, int? frameRateCapHz, ReadOnlySpan<char> name) {
		return builder.CreateLoop(new LocalApplicationLoopCreationConfig {
			FrameRateCapHz = frameRateCapHz,
			IterationShouldPumpSystemEventQueue = false,
			Name = name,
			FrameTimingPrecisionBusyWaitTime = TimeSpan.Zero
		});
	}

	static IDisposable StartAvaloniaAnimationFrameLoop(ILocalApplicationLoopBuilder builder, Action<TimeSpan> tickCallback, IDisposable? inputSource, Visual? topLevelSource, int? frameRateCapHz, ReadOnlySpan<char> name) {
		var loop = CreateUiDrivenLoop(builder, frameRateCapHz, name);
		var cancellationTokenSource = new CancellationTokenSource();
		var stopToken = cancellationTokenSource.Token;

		Func<TopLevel?> topLevelResolver = topLevelSource != null
			? () => TopLevel.GetTopLevel(topLevelSource)
			: ResolveApplicationTopLevel;

		var tickSource = new AnimationFrameTickSource(
			topLevelResolver,
			topLevelSource,
			() => {
				if (stopToken.IsCancellationRequested) return;
				if (loop.TryIterateOnce(out var deltaTime)) tickCallback(deltaTime);
			}
		);

		return new UiLoopCompositeDisposable(loop, cancellationTokenSource, tickSource, inputSource);
	}

	static TopLevel? ResolveApplicationTopLevel() {
		return Application.Current?.ApplicationLifetime switch {
			IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow,
			ISingleViewApplicationLifetime singleView => singleView.MainView != null ? TopLevel.GetTopLevel(singleView.MainView) : null,
			_ => null
		};
	}

	static IDisposable StartAvaloniaDispatcherLoop(ILocalApplicationLoopBuilder builder, Action<TimeSpan> tickCallback, IDisposable? inputSource, int tickRateHz, DispatcherPriority priority, ReadOnlySpan<char> name) {
		if (tickRateHz <= 0) tickRateHz = DefaultUiLoopTickRateHz;

		var loop = CreateUiDrivenLoop(builder, null, name);
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
	public static Size AsSize<T>(this XYPair<T> @this) where T : unmanaged, INumber<T> => new(@this.ToVector2());
}