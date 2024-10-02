// Created on 2024-10-02 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

public interface ITrackedDisposable : IDisposable {
	bool IsDisposed { get; }
}