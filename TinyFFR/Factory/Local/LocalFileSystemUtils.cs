// Created on 2025-03-23 by Ben Bowen
// (c) Egodystonic / TinyFFR 2025

using System.IO;
using Egodystonic.TinyFFR.Assets.Local;
using Egodystonic.TinyFFR.Resources.Memory;

namespace Egodystonic.TinyFFR.Factory.Local;

static class LocalFileSystemUtils {
	public static readonly string ApplicationDataDirectoryPath = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
		"Egodystonic",
		"TinyFFR"
	);

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

	public static PooledHeapMemory<byte> ReadFileIntoPooledMemory(IHeapPool heapPool, string filePath, string assetKindDescription) {
		using var fileHandle = File.OpenHandle(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.SequentialScan);
		var fileLengthBytes = RandomAccess.GetLength(fileHandle);
		if (fileLengthBytes > LocalAssetLoader.MaxAssetBufferSizeBytes) {
			LocalAssetLoader.ThrowIfAssetBufferSizeExceedsMaximum(fileLengthBytes, $"{assetKindDescription} '{filePath}'");
		}

		var result = heapPool.Borrow(checked((int) fileLengthBytes));
		try {
			var totalBytesRead = 0;
			while (totalBytesRead < result.Span.Length) {
				var bytesRead = RandomAccess.Read(fileHandle, result.Span[totalBytesRead..], totalBytesRead);
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
}
