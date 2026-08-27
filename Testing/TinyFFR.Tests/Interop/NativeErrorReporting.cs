// Created on 2026-08-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System.Text;
using System.Threading;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Interop;

[TestFixture]
unsafe class NativeErrorReportingTest {
	const int MaxExpectedErrorLength = 1000;
	const string InjectFuncName = "inject_fake_error";
	static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(20d);

	static InteropResult InjectFakeError(string message) {
		var utf8Bytes = Encoding.UTF8.GetBytes(message);
		var nullTerminatedBuffer = new byte[utf8Bytes.Length + 1];
		utf8Bytes.CopyTo(nullTerminatedBuffer, 0);
		fixed (byte* bufferPtr = nullTerminatedBuffer) {
			return LocalNativeUtils.InjectFakeError(bufferPtr);
		}
	}

	[Test]
	public void ShouldRoundTripNativeErrorMessage() {
		const string Message = "Deliberate round trip test message.";

		Assert.IsFalse(InjectFakeError(Message));

		var lastError = LocalNativeUtils.GetLastError();
		Assert.IsTrue(lastError.StartsWith(InjectFuncName, StringComparison.Ordinal), lastError);
		Assert.IsTrue(lastError.EndsWith(Message, StringComparison.Ordinal), lastError);
	}

	[Test]
	public void ShouldSubstitutePlaceholderForNullNativeErrorMessage() {
		Assert.IsFalse(LocalNativeUtils.InjectFakeError(null));

		var lastError = LocalNativeUtils.GetLastError();
		Assert.IsTrue(lastError.StartsWith(InjectFuncName, StringComparison.Ordinal), lastError);
		Assert.IsTrue(lastError.EndsWith("<null>", StringComparison.Ordinal), lastError);
	}

	[Test]
	public void ShouldTruncateOverlongNativeErrorWithoutCrashing() {
		var overlongMessage = new String('x', 10_000);

		Assert.IsFalse(InjectFakeError(overlongMessage));

		var lastError = LocalNativeUtils.GetLastError();
		Assert.LessOrEqual(lastError.Length, MaxExpectedErrorLength);
		Assert.IsTrue(lastError.StartsWith(InjectFuncName, StringComparison.Ordinal), lastError);
		Assert.IsTrue(lastError.EndsWith("...", StringComparison.Ordinal), lastError);
	}

	[Test]
	public void ShouldNotCorruptSubsequentErrorAfterTruncation() {
		const string ShortMessage = "Short message after truncation.";

		Assert.IsFalse(InjectFakeError(new String('x', 10_000)));
		Assert.IsFalse(InjectFakeError(ShortMessage));

		var lastError = LocalNativeUtils.GetLastError();
		Assert.IsTrue(lastError.EndsWith(ShortMessage, StringComparison.Ordinal), lastError);
		Assert.IsFalse(lastError.Contains('x', StringComparison.Ordinal), lastError);
	}

	[Test, Timeout(60_000)]
	public void ShouldKeepNativeErrorMessagesPerThread() {
		const int ThreadCount = 8;
		const int IterationsPerThread = 250;

		using var startSignal = new CountdownEvent(ThreadCount);
		var failures = new List<string>();
		var threads = new Thread[ThreadCount];

		for (var t = 0; t < ThreadCount; ++t) {
			var threadIndex = t;
			threads[t] = new Thread(() => {
				try {
					startSignal.Signal();
					startSignal.Wait(WaitTimeout);

					for (var i = 0; i < IterationsPerThread; ++i) {
						var tokenUniqueToThisThread = $"thread-{threadIndex}-iteration-{i}";
						InjectFakeError(tokenUniqueToThisThread);
						var lastError = LocalNativeUtils.GetLastError();
						if (lastError.EndsWith(tokenUniqueToThisThread, StringComparison.Ordinal)) continue;

						lock (failures) {
							failures.Add($"Thread {threadIndex} iteration {i} expected message ending '{tokenUniqueToThisThread}' but read '{lastError}'.");
						}
						return;
					}
				}
#pragma warning disable CA1031 // "Don't catch/swallow Exception" -- An escaping exception on a background thread would kill the test host
				catch (Exception e) {
#pragma warning restore CA1031
					lock (failures) failures.Add(e.ToString());
				}
			}) { IsBackground = true };
			threads[t].Start();
		}

		foreach (var thread in threads) Assert.IsTrue(thread.Join(WaitTimeout));
		lock (failures) {
			if (failures.Count > 0) Assert.Fail($"{failures.Count} of {ThreadCount} thread(s) failed. First failure: {failures[0]}");
		}
	}
}
