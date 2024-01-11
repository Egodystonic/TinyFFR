// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

namespace Egodystonic.TinyFFR.Memory;

interface IPoolable<out TSelf, TInitParams> where TSelf : IPoolable<TSelf, TInitParams> where TInitParams : struct {
	static abstract TSelf InstantiateNew();
	void Reinitialize(in TInitParams initParams);
}