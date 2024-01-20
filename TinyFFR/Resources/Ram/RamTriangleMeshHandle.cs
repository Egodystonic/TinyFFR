// Created on 2024-01-09 by Ben Bowen
// (c) Egodystonic / TinyFFR 2024

using Egodystonic.TinyFFR.Assets;

namespace Egodystonic.TinyFFR.Resources.Ram;

public readonly record struct RamTriangleMeshHandle : IDisposable {
	public ReadOnlySpan<MeshTriangle> Data => MemoryMarshal.Cast<byte, MeshTriangle>(Handle.Data);
	
	internal RamResourceHandle Handle { get; }

	public RamTriangleMeshHandle(RamResourceHandle handle) { Handle = handle; }

	public void Dispose() => Handle.Dispose();

	public static implicit operator RamResourceHandle(RamTriangleMeshHandle operand) => operand.Handle;
	public static explicit operator RamTriangleMeshHandle(RamResourceHandle operand) => new(operand);
}