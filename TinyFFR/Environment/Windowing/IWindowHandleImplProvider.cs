// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

interface IWindowHandleImplProvider {
	int GetTitleMaxLength();
	int GetTitle(WindowPtr ptr, Span<char> dest);
	void SetTitle(WindowPtr ptr, ReadOnlySpan<char> src);
	bool IsDisposed(WindowPtr ptr);
	void Dispose(WindowPtr ptr);
}