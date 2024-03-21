// Created on 2024-03-18 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Resources;

public interface IRefEnumerable<out TItem> {
	int Count { get; }
	TItem ElementAt(int index);
}