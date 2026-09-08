// Created on 2026-09-07 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Benchmarks.Harness;

static unsafe class TinyFfrThread {
	static readonly SemaphoreSlim WorkReady = new(0, 1);
	static readonly SemaphoreSlim WorkDone = new(0, 1);
	static Thread? _thread;
	static delegate*<void> _work;
	static Exception? _error;
	static bool _stopRequested;

	public static bool IsRunning => _thread != null;

	public static void Start() {
		if (_thread != null) return;

		_stopRequested = false;
		_thread = new Thread(Loop) {
			IsBackground = true,
			Name = "TinyFFR Benchmark Primary Thread"
		};
		_thread.Start();
	}

	public static void Invoke(delegate*<void> work) {
		if (_thread == null) {
			work();
			return;
		}

		_work = work;
		WorkReady.Release();
		WorkDone.Wait();

		if (_error is not { } error) return;
		_error = null;
		throw new InvalidOperationException($"Benchmark work threw on the TinyFFR primary thread: {error.Message}", error);
	}

	public static void Stop() {
		if (_thread is not { } thread) return;

		_stopRequested = true;
		WorkReady.Release();
		WorkDone.Wait();
		thread.Join();
		_thread = null;
	}

	static void Loop() {
		while (true) {
			WorkReady.Wait();

			if (_stopRequested) {
				WorkDone.Release();
				return;
			}

			try {
				_work();
			}
			catch (Exception e) {
				_error = e;
			}

			WorkDone.Release();
		}
	}
}
