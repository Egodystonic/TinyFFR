// Created on 2026-05-08 by Ben Bowen
// (c) Egodystonic / TinyFFR 2026

namespace Egodystonic.TinyFFR;

/// <summary>
/// A parameter of this type can essentially be ignored (pass <c>default(MethodOverloadStub)</c>).
/// </summary>
/// <remarks>
/// This is a type used to force methods to be overloadable in cases where they otherwise could not be by chaging their arity.
/// </remarks>
public readonly struct MethodOverloadStub;