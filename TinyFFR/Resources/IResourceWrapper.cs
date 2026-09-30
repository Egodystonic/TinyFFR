// Created on 2026-09-29 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR.Resources;

interface IResourceWrapper<out TSelf, in TResource> where TResource : IResource<TResource> {
	static abstract TSelf Wrap(TResource resource);
}
