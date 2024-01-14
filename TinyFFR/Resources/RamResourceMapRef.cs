// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR;

readonly struct RamResourceMapRef {
	public MappedResourceType Type { get; }
	public int LookupHash { get; } // TODO use upper 16 bits as a rolling incrementer and bottom 16 bits as index 
}