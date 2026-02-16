// Created on 2026-02-15 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

using System;
using System.Reflection.Metadata;
using System.Security;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Environment;
using Egodystonic.TinyFFR.Environment.Input;
using Egodystonic.TinyFFR.Environment.Input.Local;
using Egodystonic.TinyFFR.Factory.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Rendering.Local.Sync;
using Egodystonic.TinyFFR.Resources;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Assets.Meshes.Local;

readonly struct MorphingAnimationData : IAnimationData {
	public UIntPtr Handle => throw new NotImplementedException();
	public float DefaultCompletionTimeSeconds => throw new NotImplementedException();
}