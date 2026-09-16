// Created on 2024-01-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Assets.Materials;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Local;
using Egodystonic.TinyFFR.Rendering;
using Egodystonic.TinyFFR.Resources;

namespace Egodystonic.TinyFFR.Environment;

/// <summary>
/// A resource that helps maintain a target framerate by tracking loop timing states, and helps collect/iterate local input values.
/// </summary>
public readonly struct ApplicationLoop : IDisposableResource<ApplicationLoop, IApplicationLoopImplProvider> {
	readonly ResourceHandle<ApplicationLoop> _handle;
	readonly IApplicationLoopImplProvider _impl;

	internal IApplicationLoopImplProvider Implementation => _impl ?? throw InvalidObjectException.InvalidDefault<ApplicationLoop>();
	internal ResourceHandle<ApplicationLoop> Handle => IsDisposed ? throw new ObjectDisposedException(nameof(ApplicationLoop)) : _handle;

	IApplicationLoopImplProvider IResource<ApplicationLoop, IApplicationLoopImplProvider>.Implementation => Implementation;
	ResourceHandle<ApplicationLoop> IResource<ApplicationLoop>.Handle => Handle;
	ResourceIdent IResource.Ident => Handle.Ident;
	IResourceImplProvider IResource.Implementation => Implementation;
	ResourceStub IResource.AsStub => new(Handle.Ident, Implementation);

	/// <summary>
	/// The input states and events captured by the most recent iteration of this loop.
	/// </summary>
	/// <remarks>
	/// Everything this exposes describes the state as of the last <see cref="IterateOnce"/>/<see cref="TryIterateOnce"/> call. The same instance is returned
	/// every time (meaning you can retain the reference and use it to always get the latest input state if you wish).
	/// </remarks>
	public ILatestInputRetriever Input {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetInputStateProvider(_handle);
	}

	/// <summary>
	/// Whether each iteration should also transcribe keyboard input as text (see <see cref="ILatestKeyboardAndMouseInputRetriever.TranscribedText"/>), which is what you want when the user is typing in to your application.
	/// </summary>
	/// <remarks>
	/// Text transcription asks the operating system for the characters the user's keystrokes actually produce, taking their keyboard layout, modifier keys and any
	/// input method (used to type languages whose character set is larger than a keyboard) in to account. That is a different question from which physical keys are
	/// down, which is what the rest of the input API answers. Whilst transcription is enabled some keystrokes may be reported only as transcribed text and not as
	/// key events, because the operating system's text input handling can consume them; accordingly, prefer to enable this only while the user is actually typing.
	/// </remarks>
	public bool EnableInputTextTranscription {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetEnableInputTextTranscription(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetEnableInputTextTranscription(_handle, value);
	}

	/// <summary>
	/// The minimum amount of time this loop will let elapse between the start of one iteration and the start of the next, or <see cref="TimeSpan.Zero"/> for no limit.
	/// </summary>
	/// <remarks>
	/// This is a floor on the iteration interval, not a guarantee of one: if an iteration and the work you do after it together take longer than this, the next
	/// iteration simply begins late. <see cref="TargetFrameRate"/> expresses the same setting as a frame rate instead (i.e. the reciprocal of this value). Setting
	/// one necessarily changes the other.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative value.</exception>
	public TimeSpan TargetIterationInterval {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTargetIterationInterval(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTargetIterationInterval(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="TargetIterationInterval"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="newValue">The new target interval. Must not be negative; use <see cref="TimeSpan.Zero"/> for no limit.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newValue"/> is negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTargetIterationInterval(TimeSpan newValue) => TargetIterationInterval = newValue;

	/// <summary>
	/// The maximum number of iterations per second this loop will run at, or <see langword="null"/> for no cap.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Note that if <see cref="RendererBuilderConfig.EnableVSync"/> was set to <c>true</c> (i.e. vsync is enabled; the default) usually you'll want to leave this
	/// as <c>null</c> so the render loop follows the target <see cref="Display"/>'s refresh rate.
	/// </para>
	/// <para>
	/// This is <see cref="TargetIterationInterval"/> expressed as a rate rather than a duration; setting either changes the other. Like that property this is a cap
	/// and not a guarantee: the loop will not run faster than this, but it can run slower if an iteration takes longer than the cap allows for. The value read back
	/// is rounded to the nearest whole number of iterations per second, so it may not exactly match a value previously set.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value that is zero or negative. Use <see langword="null"/> for no cap.</exception>
	public int? TargetFrameRate {
		get {
			var targetInterval = TargetIterationInterval;
			if (targetInterval <= TimeSpan.Zero) return null;
			return (int) Math.Round(TimeSpan.FromSeconds(1d) / targetInterval, 0, MidpointRounding.AwayFromZero);
		}
		set {
			if (value <= 0) {
				throw new ArgumentOutOfRangeException(nameof(value), value, $"Target frame rate must be a positive value, or 'null' for no cap.");
			}
			TargetIterationInterval = value != null ? (TimeSpan.FromSeconds(1d) / value.Value) : TimeSpan.Zero;
		}
	}
	/// <summary>
	/// Sets <see cref="TargetFrameRate"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="newValue">The new target frame rate. Must be positive, or <see langword="null"/> for no cap.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newValue"/> is zero or negative.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTargetFrameRate(int? newValue) => TargetFrameRate = newValue;

	/// <summary>
	/// How much of each iteration may be spent executing pending asynchronous work that must run on the primary thread, expressed as a fraction of the measured
	/// iteration interval; or <see langword="null"/> for no limit.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Some asynchronous operations (such as loading assets) must complete part of their work back on the thread that owns the renderer. That work queues up and is
	/// executed by this loop, at the start of each iteration. This property bounds how long the loop is willing to spend doing so. There are three modes:
	/// </para>
	/// <para>
	/// <see langword="null"/>: No limit. Every pending task is executed before the iteration proceeds. A large backlog can therefore consume an entire iteration or
	/// more, meaning nothing is rendered until the backlog drains, giving the impression that the application has stalled for an unbounded amount of time (though it
	/// does complete all the work in the shortest total time).
	/// </para>
	/// <para>
	/// <c>0f</c>: Exactly one pending task is executed per iteration. Pending work still progresses rather than starving entirely, but never occupies more than a
	/// single task's worth of any given iteration.
	/// </para>
	/// <para>
	/// Any positive value (the default is <c>0.25f</c>): That fraction of the measured iteration interval is used as the per-iteration budget. Because the budget
	/// scales with the observed iteration time it adapts automatically to the display's refresh rate, including when the pacing comes from vsync rather than from
	/// <see cref="TargetFrameRate"/>.
	/// </para>
	/// <para>
	/// The most useful way to think about a positive value is its effect on frame rate: whilst there is pending work, the resulting frame rate settles at
	/// approximately <c>(nominal frame rate / (1 + fraction))</c>, and the share of each frame given over to pending work settles at approximately
	/// <c>(fraction / (1 + fraction))</c>. So <c>0.25f</c> costs about a fifth of each frame, <c>1f</c> halves the frame rate, and <c>3f</c> quarters it.
	/// </para>
	/// <para>
	/// Values greater than <c>1f</c> are permitted. They are intended for loading screens, where devoting most of each frame to pending work is desirable: unlike
	/// <see langword="null"/>, a large fraction still guarantees a predictable frame cadence rather than letting a backlog consume the frame entirely. Returns
	/// diminish above roughly <c>8f</c>, which already yields about 89% of each frame. These relationships are asymptotic and remain stable at every fraction; no
	/// value causes runaway feedback.
	/// </para>
	/// <para>
	/// The interval the fraction is applied to deliberately excludes the time previously spent executing these tasks. Measuring the raw iteration interval instead
	/// would let the budget feed back in to itself (more task time makes iterations longer, which grants more task time), so the budget is keyed on the iteration
	/// interval as it would be without any such work in it.
	/// </para>
	/// <para>
	/// In all modes at least one pending task is executed per iteration when any are pending, because the budget is only checked between tasks and never during one.
	/// That also means a single long-running task can still overrun the frame. The first iteration of a loop has no measured interval yet and executes exactly one
	/// pending task: granting an unbounded budget before any interval has been measured would let a startup backlog push the very first frame over the display's
	/// refresh interval, which the measurement would then adopt as its baseline.
	/// </para>
	/// <para>
	/// <see cref="TryIterateOnce"/> executes pending work on every call, including calls that return <see langword="false"/> because the next iteration is not yet
	/// due. Pass <c>executePendingPrimaryThreadCooperativeTasks: false</c> to either iteration method to skip executing pending work entirely (though this will
	/// mean async tasks make no progress).
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative or non-finite value. Use <see langword="null"/> for no limit.</exception>
	public float? TargetPerFrameAsyncCooperativeTaskTimeFraction {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTargetPerFrameAsyncCooperativeTaskTimeFraction(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTargetPerFrameAsyncCooperativeTaskTimeFraction(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="TargetPerFrameAsyncCooperativeTaskTimeFraction"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="newValue">The new fraction. Must be finite and not negative, or <see langword="null"/> for no limit.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newValue"/> is negative or non-finite.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTargetPerFrameAsyncCooperativeTaskTimeFraction(float? newValue) => TargetPerFrameAsyncCooperativeTaskTimeFraction = newValue;

	/// <summary>
	/// The mean frame rate across the most recent iterations of this loop, or <c>0f</c> if it has not yet been iterated.
	/// </summary>
	/// <remarks>
	/// This and the other <c>FramesPerSecondRecent</c> properties are calculated over a rolling window of the most recently completed iterations (256 of them by
	/// default; configurable when the loop is built). Because it is a mean of frame times rather than of frame rates, an occasional long frame moves it less than
	/// you might expect — consult <see cref="FramesPerSecondRecentMin"/> for the worst case.
	/// </remarks>
	public float FramesPerSecondRecentAverage {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFramesPerSecondRecentAverage(_handle);
	}

	/// <summary>
	/// The lowest frame rate observed across the most recent iterations of this loop (i.e. the longest single iteration in the rolling window), or <c>0f</c> if it has not yet been iterated.
	/// </summary>
	/// <remarks>
	/// This is usually the most informative of the frame rate properties when judging perceived smoothness, as it reflects the worst stutter a user would have
	/// noticed rather than the typical case.
	/// </remarks>
	public float FramesPerSecondRecentMin {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFramesPerSecondRecentMin(_handle);
	}

	/// <summary>
	/// The highest frame rate observed across the most recent iterations of this loop (i.e. the shortest single iteration in the rolling window), or <c>0f</c> if it has not yet been iterated.
	/// </summary>
	public float FramesPerSecondRecentMax {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFramesPerSecondRecentMax(_handle);
	}

	/// <summary>
	/// The frame rate implied by the single most recent iteration of this loop, or <c>0f</c> if it has not yet been iterated.
	/// </summary>
	/// <remarks>
	/// This is simply the reciprocal of the last delta time, so it fluctuates from iteration to iteration and is rarely what you want to show a user directly;
	/// <see cref="FramesPerSecondRecentAverage"/> is the steadier figure.
	/// </remarks>
	public float FramesPerSecondLatest {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetFramesPerSecondLatest(_handle);
	}

	internal ApplicationLoop(ResourceHandle<ApplicationLoop> handle, IApplicationLoopImplProvider impl) {
		ArgumentNullException.ThrowIfNull(impl);
		_handle = handle;
		_impl = impl;
	}

	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public string GetNameAsNewStringObject() => Implementation.GetNameAsNewStringObject(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int GetNameLength() => Implementation.GetNameLength(_handle);
	/// <inheritdoc/>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void CopyName(Span<char> destinationBuffer) => Implementation.CopyName(_handle, destinationBuffer);

	static ApplicationLoop IResource<ApplicationLoop>.CreateFromHandleAndImpl(ResourceHandle<ApplicationLoop> handle, IResourceImplProvider impl) {
		return new ApplicationLoop(handle, impl as IApplicationLoopImplProvider ?? throw new InvalidOperationException($"Impl was '{impl}'."));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal ResourceHandle<ApplicationLoop> GetHandleWithoutDisposeCheck() => _handle;
	ResourceHandle<ApplicationLoop> IResource<ApplicationLoop>.GetHandleWithoutDisposeCheck() => GetHandleWithoutDisposeCheck();

	/// <summary>
	/// The sum of every delta time this loop has returned so far; i.e. how long this loop has been running, as measured by the loop itself.
	/// </summary>
	/// <remarks>
	/// This is the value to use as a clock for anything that should stay in step with the simulation (animation phases, timed events, shader time inputs), because
	/// it advances only when the loop iterates. It is settable, which lets you offset or rewind that clock; <see cref="ResetTotalIteratedTime"/> zeroes it.
	/// </remarks>
	public TimeSpan TotalIteratedTime {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTotalIteratedTime(_handle);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		set => Implementation.SetTotalIteratedTime(_handle, value);
	}
	/// <summary>
	/// Sets <see cref="TotalIteratedTime"/>; provided as a method for use in contexts where a property setter can not be invoked.
	/// </summary>
	/// <param name="newValue">The new total iterated time.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // Method can be obsoleted and ultimately removed once https://github.com/dotnet/roslyn/issues/45284 is fixed
	public void SetTotalIteratedTime(TimeSpan newValue) => TotalIteratedTime = newValue;

	/// <summary>
	/// How much longer this loop would wait before beginning its next iteration, or <see cref="TimeSpan.Zero"/> if the next iteration is already due.
	/// </summary>
	/// <remarks>
	/// This is the amount of time <see cref="IterateOnce"/> would spend waiting if called right now, and is equivalently the amount of time for which
	/// <see cref="TryIterateOnce"/> would keep returning <see langword="false"/>. With no cap set (see <see cref="TargetIterationInterval"/>) this is always
	/// <see cref="TimeSpan.Zero"/>.
	/// </remarks>
	public TimeSpan TimeUntilNextIteration {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.GetTimeUntilNextIteration(_handle);
	}

	/// <summary>
	/// Waits (blocks the caller) until the next iteration is due, then iterates this loop once (which refreshes <see cref="Input"/>) and returns the delta time.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The returned delta time is the time that has elapsed since the <i>previous</i> iteration returned, not the time this call spent iterating. It therefore
	/// includes everything your own code did between the two calls as well as any time this method spent waiting, which is what makes it the correct multiplier for
	/// movement and other rate-based simulation: scaling by it keeps your application running at the same speed regardless of frame rate.
	/// </para>
	/// <para>
	/// This method blocks until the next iteration is due, according to <see cref="TargetIterationInterval"/>. Use <see cref="TryIterateOnce"/> instead where you
	/// have other work to do whilst waiting, or where this loop is driven by something that has its own timing.
	/// </para>
	/// </remarks>
	/// <param name="executePendingPrimaryThreadCooperativeTasks">
	/// Whether this iteration may also execute pending asynchronous work queued for the primary thread; see <see cref="TargetPerFrameAsyncCooperativeTaskTimeFraction"/>,
	/// which governs how much of the iteration such work may consume. Pass <see langword="false"/> to skip it entirely for this iteration.
	/// </param>
	/// <returns>The time elapsed since the previous iteration.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public TimeSpan IterateOnce(bool executePendingPrimaryThreadCooperativeTasks = true) => Implementation.IterateOnce(_handle, executePendingPrimaryThreadCooperativeTasks);

	/// <summary>
	/// Iterates this loop once (which refreshes <see cref="Input"/>) and returns <c>true</c> <i>only</i> if the next iteration is already due,
	/// otherwise returns <c>false</c> immediately.
	/// </summary>
	/// <remarks>
	/// As with <see cref="IterateOnce"/>, the delta time written to <paramref name="outDeltaTime"/> is the time that has elapsed since the previous iteration
	/// returned, not the time this call spent iterating. Note that pending asynchronous work is executed on every call, including calls that return
	/// <see langword="false"/>.
	/// </remarks>
	/// <param name="outDeltaTime">When this method returns <see langword="true"/>, the time elapsed since the previous iteration; otherwise <see cref="TimeSpan.Zero"/>.</param>
	/// <param name="executePendingPrimaryThreadCooperativeTasks">
	/// Whether this call may also execute pending asynchronous work queued for the primary thread; see <see cref="TargetPerFrameAsyncCooperativeTaskTimeFraction"/>,
	/// which governs how much of the iteration such work may consume. Pass <see langword="false"/> to skip it entirely for this call.
	/// </param>
	/// <returns><see langword="true"/> if this loop was iterated, or <see langword="false"/> if the next iteration was not yet due.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool TryIterateOnce(out TimeSpan outDeltaTime, bool executePendingPrimaryThreadCooperativeTasks = true) => Implementation.TryIterateOnce(_handle, out outDeltaTime, executePendingPrimaryThreadCooperativeTasks);

	/// <summary>
	/// Sets <see cref="TotalIteratedTime"/> back to <see cref="TimeSpan.Zero"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ResetTotalIteratedTime() => TotalIteratedTime = TimeSpan.Zero;

	/// <inheritdoc />
	public override string ToString() => $"Application Loop {(IsDisposed ? "(Disposed)" : $"\"{GetNameAsNewStringObject()}\"")}";

	#region Disposal
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Dispose() => Implementation.Dispose(_handle);

	internal bool IsDisposed {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get => Implementation.IsDisposed(_handle);
	}
	#endregion

	#region Equality
	/// <inheritdoc />
	public bool Equals(ApplicationLoop other) => _handle.Equals(other._handle) && ReferenceEquals(_impl, other._impl);
	/// <inheritdoc />
	public override bool Equals(object? obj) => obj is ApplicationLoop other && Equals(other);
	/// <inheritdoc />
	public override int GetHashCode() => HashCode.Combine(_handle, _impl);
	/// <summary>
	/// <see cref="Equals(ApplicationLoop)"/>
	/// </summary>
	public static bool operator ==(ApplicationLoop left, ApplicationLoop right) => left.Equals(right);
	/// <summary>
	/// <see cref="Equals(ApplicationLoop)"/>
	/// </summary>
	public static bool operator !=(ApplicationLoop left, ApplicationLoop right) => !left.Equals(right);
	#endregion
}