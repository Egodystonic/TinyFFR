// Created on 2024-09-26 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources.Memory;

public interface IStringSpanNameEnabled {
	string Name { get; }
	int GetNameUsingSpan(Span<char> dest);
	int GetNameSpanLength();
}