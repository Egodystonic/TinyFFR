// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;

namespace Egodystonic.TinyFFR.Environment.Local;

/// <summary>
/// Configuration for the local <see cref="IApplicationLoopBuilder"/> itself, supplied when the factory is created. Settings that vary per application loop are given to <see cref="IApplicationLoopBuilder.CreateLoop(in ApplicationLoopCreationConfig)"/> instead.
/// </summary>
public sealed record LocalApplicationLoopBuilderConfig {
	/// <summary>
	/// The default value for <see cref="FrameRateBufferSizeLog2"/> (<c>8</c>, i.e. 256 iterations).
	/// </summary>
	public const int DefaultFrameRateBufferSizeLog2 = 8;
	/// <summary>
	/// The maximum permitted value for <see cref="FrameRateBufferSizeLog2"/> (<c>16</c>, i.e. 65,536 iterations).
	/// </summary>
	public const int MaxFrameRateBufferSizeLog2 = 16;
	/// <summary>
	/// The default value for <see cref="TargetPerFrameAsyncCooperativeTaskTimeFraction"/>: <c>0.25</c>.
	/// </summary>
	public const float DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction = 0.25f;

	/// <summary>
	/// The base-2 logarithm of the number of recent iterations whose timings are retained for each application loop's frame rate statistics; i.e. a value of
	/// <c>n</c> retains <c>2^n</c> iterations. Must be in the range <c>1 &lt;= n &lt;= </c><see cref="MaxFrameRateBufferSizeLog2"/>. Defaults to <see cref="DefaultFrameRateBufferSizeLog2"/>.
	/// </summary>
	/// <remarks>
	/// This sets the window that <see cref="ApplicationLoop.FramesPerSecondRecentAverage"/>, <see cref="ApplicationLoop.FramesPerSecondRecentMin"/> and
	/// <see cref="ApplicationLoop.FramesPerSecondRecentMax"/> are calculated over. A larger window gives steadier figures that react more slowly to a change in
	/// performance, at the cost of retaining more memory per loop. It is expressed as a power of two so that the statistics can be maintained without division.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a value outside the range <c>1 &lt;= n &lt;= </c><see cref="MaxFrameRateBufferSizeLog2"/>.</exception>
	public int FrameRateBufferSizeLog2 {
		get;
		init {
			if (value is <= 0 or > MaxFrameRateBufferSizeLog2) {
				throw new ArgumentOutOfRangeException(nameof(FrameRateBufferSizeLog2), value, $"Must be between 1 and {MaxFrameRateBufferSizeLog2}.");
			}
			field = value;
		}
	} = DefaultFrameRateBufferSizeLog2;

	/// <summary>
	/// The initial value of <see cref="ApplicationLoop.TargetPerFrameAsyncCooperativeTaskTimeFraction"/> for loops created by this builder: how much of each
	/// iteration may be spent executing pending asynchronous work that must run on the primary thread, expressed as a fraction of the measured iteration interval;
	/// or <see langword="null"/> for no limit. Must be finite and not negative. Defaults to <see cref="DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Some asynchronous operations (such as loading assets) must complete part of their work back on the thread that owns the renderer. That work queues up and is
	/// executed by the application loop, at the start of each iteration. This value bounds how long the loop is willing to spend doing so. There are three modes:
	/// </para>
	/// <para>
	/// <see langword="null"/>: No limit. Every pending task is executed before the iteration proceeds. A large backlog can therefore consume an entire frame or
	/// more, meaning nothing is rendered until the backlog drains, giving the impression that the application has stalled for an unbounded amount of time (though it
	/// does complete all the work in the shortest total time).
	/// </para>
	/// <para>
	/// <c>0f</c>: Exactly one pending task is executed per iteration. Pending work still progresses rather than starving entirely, but never occupies more than a
	/// single task's worth of any given frame.
	/// </para>
	/// <para>
	/// Any positive value (the default): That fraction of the measured iteration interval is used as the per-iteration budget. Because the budget scales with the
	/// observed frame time it adapts automatically to the display's refresh rate, including when frame pacing comes from vsync rather than from
	/// <see cref="LocalApplicationLoopCreationConfig.FrameRateCapHz"/>.
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
	/// would let the budget feed back in to itself (more task time makes frames longer, which grants more task time), so the budget is keyed on the iteration
	/// interval as it would be without any such work in it.
	/// </para>
	/// <para>
	/// In all modes at least one pending task is executed per iteration when any are pending, because the budget is only checked between tasks and never during one.
	/// That also means a single long-running task can still overrun the frame. The first iteration of a loop has no measured interval yet and executes exactly one
	/// pending task: granting an unbounded budget before any interval has been measured would let a startup backlog push the very first frame over the display's
	/// refresh interval, which the measurement would then adopt as its baseline.
	/// </para>
	/// <para>
	/// <see cref="ApplicationLoop.TryIterateOnce"/> executes pending work on every call, including calls that return <see langword="false"/> because the next frame
	/// is not yet due. Pass <c>executePendingPrimaryThreadCooperativeTasks: false</c> to either iteration method to skip executing pending work entirely.
	/// </para>
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when set to a negative or non-finite value. Use <see langword="null"/> for no limit.</exception>
	public float? TargetPerFrameAsyncCooperativeTaskTimeFraction {
		get;
		init {
			if (value is { } v && !v.IsNonNegativeAndFinite()) {
				throw new ArgumentOutOfRangeException(nameof(TargetPerFrameAsyncCooperativeTaskTimeFraction), value, $"Must be a finite, non-negative value (use null for no limit).");
			}
			field = value;
		}
	} = DefaultTargetPerFrameAsyncCooperativeTaskTimeFraction;
}
