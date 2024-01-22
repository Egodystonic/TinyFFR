// Created on 2024-01-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Environment.Windowing;

interface IWindowHandleImplProvider {
	int GetTitleMaxLength();
	int GetTitle(WindowPtr ptr, Span<char> dest);
	void SetTitle(WindowPtr ptr, ReadOnlySpan<char> src);

	XYPair GetSize(WindowPtr ptr);
	void SetSize(WindowPtr ptr, XYPair newDimensions);

	XYPair GetPosition(WindowPtr ptr);
	void SetPosition(WindowPtr ptr, XYPair newDimensions);

	bool IsDisposed(WindowPtr ptr);
	void Dispose(WindowPtr ptr);
}