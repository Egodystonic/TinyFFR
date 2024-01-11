// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using System;
using Egodystonic.TinyFFR.Memory;

namespace Egodystonic.TinyFFR.Environment;

public sealed class Window : IPoolable<Window, WindowOptions> {
	static readonly TffrObjectPool<Window, WindowOptions> _objPool = new();

	Window() { }
	static Window IPoolable<Window, WindowOptions>.InstantiateNew() => new();
	public static Window Create() => Create(new());
	public static Window Create(in WindowOptions options) => _objPool.GetOne(options);
	public void Dispose() => _objPool.ReturnOne(this);
	unsafe void IPoolable<Window, WindowOptions>.Reinitialize(in WindowOptions initParams) {
		var msg = stackalloc byte[1000];
		NativeWindowFactory.CreateWindow((IntPtr) msg);
	}
}