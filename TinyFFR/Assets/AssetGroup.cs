// // Created on 2024-08-07 by Ben Bowen
// // (c) Egodystonic / TinyFFR 2024
//
// using System.Buffers.Binary;
//
// namespace Egodystonic.TinyFFR.Assets;
//
// readonly ref struct AssetGroup {
// 	const int HeaderLengthBytes = 4;
// 	const int DisposalFlagIndex = 0;
// 	const int MeshRefCountIndex = 1;
// 	const byte DisposalFlagNonDisposedValue = Byte.MaxValue;
// 	const byte DisposalFlagDisposedValue = Byte.MinValue;
// 	readonly ReadOnlySpan<byte> _groupData;
//
// 	public bool IsDisposed => _groupData[DisposalFlagIndex] == DisposalFlagDisposedValue;
// 	public int MeshCount => _groupData[MeshRefCountIndex];
//
// 	public AssetGroup(ReadOnlySpan<byte> entireGroupData) => _groupData = entireGroupData;
//
// 	public static int GetGroupLengthBytes(int meshCount) {
// 		return HeaderLengthBytes + meshCount * UIntPtr.Size;
// 	}
// 	public static void WriteGroup(Span<byte> dest, ReadOnlySpan<ModelAsset> meshes) {
// 		static byte GetSpanLengthOrThrowIfOutOfRange<T>(ReadOnlySpan<T> span) {
// 			if (span.Length > Byte.MaxValue) {
// 				throw new InvalidOperationException($"It is not currently possible to create or load assets with more than {Byte.MaxValue} {typeof(T).Name}s (actual count is {span.Length}).");
// 			}
// 			return (byte) span.Length;
// 		}
//
// 		dest[DisposalFlagIndex] = DisposalFlagNonDisposedValue;
// 		dest[MeshRefCountIndex] = GetSpanLengthOrThrowIfOutOfRange(meshes);
//
// 		var ptrDest = MemoryMarshal.Cast<byte, UIntPtr>(dest[HeaderLengthBytes..]);
// 		for (var i = 0; i < meshes.Length; ++i) ptrDest[i] = meshes[i].HandleAsPtr;
// 	}
// 	public static (bool IsDisposed, int ByteLength) ReadGroupHeader(ReadOnlySpan<byte> src) {
// 		var byteCount = src[MeshRefCountIndex];
// 		return (src[DisposalFlagIndex] == DisposalFlagDisposedValue, byteCount);
// 	}
// }