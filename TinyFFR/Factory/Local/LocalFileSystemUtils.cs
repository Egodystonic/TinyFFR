// Created on 2025-03-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using System.Threading;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Interop;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

static unsafe class LocalFileSystemUtils {
	const int MaxFilePathLengthChars = LocalAssetLoaderConfig.DefaultMaxAssetFilePathLengthChars;

	public static readonly string ApplicationDataDirectoryPath = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
		"Egodystonic",
		"TinyFFR"
	);

	static readonly string PathTooLongMessage = $"File path exceeds the maximum supported length of {MaxFilePathLengthChars} characters.";

	static readonly ThreadLocal<InteropStringBuffer> FilePathBufferStore = new(
		static () => new InteropStringBuffer(MaxFilePathLengthChars, addOneForNullTerminator: true),
		trackAllValues: true
	);

	public static void ReleaseThreadLocalBuffers() {
		foreach (var pathBuffer in FilePathBufferStore.Values) pathBuffer.Dispose();
	}

	public static void AttemptToEnsureApplicationDataFolderExists() {
		try {
			if (!Directory.Exists(ApplicationDataDirectoryPath)) Directory.CreateDirectory(ApplicationDataDirectoryPath);
		}
		catch (Exception e) when (ExceptionIndicatesGeneralIoError(e)) {
			Console.WriteLine($"Could not ensure existence of data application folder '{ApplicationDataDirectoryPath}': {e}/{e.Message}");
		}
	}

	public static bool ExceptionIndicatesGeneralIoError(Exception e) {
		return e is IOException or DirectoryNotFoundException or UnauthorizedAccessException or PathTooLongException or NotSupportedException;
	}

	static InteropStringBuffer ConvertPathToUtf8(ReadOnlySpan<char> filePath) {
		var result = FilePathBufferStore.Value;
		result.ConvertFromUtf16OrThrowIfBufferTooSmall(filePath, PathTooLongMessage);
		return result;
	}

	public static PooledHeapMemory<byte> ReadFileIntoPooledMemory(IHeapPool heapPool, ReadOnlySpan<char> filePath, string assetKindDescription) {
		var pathBuffer = ConvertPathToUtf8(filePath);
		OpenFileForRead(in pathBuffer.AsRef, out var fileHandle, out var fileLengthBytes).ThrowIfFailure();

		try {
			if (fileLengthBytes > LocalAssetLoader.MaxAssetBufferSizeBytes) {
				LocalAssetLoader.ThrowIfAssetBufferSizeExceedsMaximum(fileLengthBytes, $"{assetKindDescription} '{filePath}'");
			}

			var result = heapPool.Borrow(checked((int) fileLengthBytes));
			try {
				var totalBytesRead = 0;
				while (totalBytesRead < result.Span.Length) {
					ReadFromFile(fileHandle, ref result.Span[totalBytesRead], result.Span.Length - totalBytesRead, out var bytesRead).ThrowIfFailure();
					if (bytesRead <= 0) {
						throw new EndOfStreamException($"Unexpected end of {assetKindDescription} '{filePath}' after {totalBytesRead} of {fileLengthBytes} bytes.");
					}
					totalBytesRead += bytesRead;
				}
			}
			catch {
				result.Dispose();
				throw;
			}
			return result;
		}
		finally {
			CloseFile(fileHandle).ThrowIfFailure();
		}
	}

	public static void WriteFile(ReadOnlySpan<char> filePath, ReadOnlySpan<byte> data) {
		var pathBuffer = ConvertPathToUtf8(filePath);
		WriteFile(in pathBuffer.AsRef, ref MemoryMarshal.GetReference(data), data.Length).ThrowIfFailure();
	}

	#region Native Methods
	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "open_file_for_read")]
	static extern InteropResult OpenFileForRead(
		in byte utf8FilePath,
		out UIntPtr outHandle,
		out long outLengthBytes
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "read_from_file")]
	static extern InteropResult ReadFromFile(
		UIntPtr handle,
		ref byte dest,
		int byteCount,
		out int outBytesRead
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "close_file")]
	static extern InteropResult CloseFile(
		UIntPtr handle
	);

	[DllImport(LocalNativeUtils.NativeLibName, EntryPoint = "write_file")]
	static extern InteropResult WriteFile(
		in byte utf8FilePath,
		ref byte src,
		long byteCount
	);
	#endregion
}
