#include "pch.h"
#include "native_impl_file_io.h"

#include "utils_and_constants.h"

#include <filesystem>

// Maintainer's note: These exist so the managed side can open files by passing a UTF-8 path buffer it already
// owns. System.IO.File takes a 'string' and .NET offers no span overload, so the loader was forced to
// materialise one per call; that string was the entire remaining per-load allocation in the asset paths.
// The path is turned into a std::filesystem::path via char8_t so that non-ASCII paths still resolve on
// Windows, where a narrow fopen would interpret the bytes as ANSI.

namespace {
	std::FILE* open_path(const char* utf8FilePath, bool forWriting) {
		const std::filesystem::path filePath { reinterpret_cast<const char8_t*>(utf8FilePath) };
#ifdef TFFR_WIN
		return _wfopen(filePath.c_str(), forWriting ? L"wb" : L"rb");
#else
		return std::fopen(filePath.c_str(), forWriting ? "wb" : "rb");
#endif
	}

	int seek_from_start_or_end(std::FILE* fileHandle, int64_t offset, int origin) {
#ifdef TFFR_WIN
		return _fseeki64(fileHandle, offset, origin);
#else
		return fseeko(fileHandle, static_cast<off_t>(offset), origin);
#endif
	}

	int64_t tell_position(std::FILE* fileHandle) {
#ifdef TFFR_WIN
		return _ftelli64(fileHandle);
#else
		return static_cast<int64_t>(ftello(fileHandle));
#endif
	}
}

void native_impl_file_io::open_file_for_read(const char* utf8FilePath, void** outHandle, int64_t* outLengthBytes) {
	ThrowIfNull(utf8FilePath, "File path was null.");
	ThrowIfNull(outHandle, "Out handle pointer was null.");
	ThrowIfNull(outLengthBytes, "Out length pointer was null.");

	auto* fileHandle = open_path(utf8FilePath, false);
	ThrowIfNull(fileHandle, "Could not open file for reading: ", utf8FilePath);

	if (seek_from_start_or_end(fileHandle, 0, SEEK_END) != 0) {
		std::fclose(fileHandle);
		Throw("Could not seek to end of file: ", utf8FilePath);
	}

	const auto lengthBytes = tell_position(fileHandle);
	if (lengthBytes < 0) {
		std::fclose(fileHandle);
		Throw("Could not determine length of file: ", utf8FilePath);
	}

	if (seek_from_start_or_end(fileHandle, 0, SEEK_SET) != 0) {
		std::fclose(fileHandle);
		Throw("Could not seek back to start of file: ", utf8FilePath);
	}

	*outHandle = fileHandle;
	*outLengthBytes = lengthBytes;
}

int32_t native_impl_file_io::read_from_file(void* handle, uint8_t* dest, int32_t byteCount) {
	ThrowIfNull(handle, "File handle was null.");
	ThrowIfNull(dest, "Destination buffer was null.");
	ThrowIfNegative(byteCount, "Byte count was negative.");
	if (byteCount == 0) return 0;

	auto* fileHandle = static_cast<std::FILE*>(handle);
	const auto bytesRead = std::fread(dest, 1U, static_cast<size_t>(byteCount), fileHandle);
	ThrowIfNotZero(std::ferror(fileHandle), "Error while reading from file.");
	return static_cast<int32_t>(bytesRead);
}

void native_impl_file_io::close_file(void* handle) {
	if (handle == nullptr) return;
	std::fclose(static_cast<std::FILE*>(handle));
}

void native_impl_file_io::write_file(const char* utf8FilePath, const uint8_t* src, int64_t byteCount) {
	ThrowIfNull(utf8FilePath, "File path was null.");
	ThrowIfNegative(byteCount, "Byte count was negative.");
	if (byteCount > 0) ThrowIfNull(src, "Source buffer was null.");

	auto* fileHandle = open_path(utf8FilePath, true);
	ThrowIfNull(fileHandle, "Could not open file for writing: ", utf8FilePath);

	if (byteCount > 0) {
		const auto bytesWritten = std::fwrite(src, 1U, static_cast<size_t>(byteCount), fileHandle);
		if (bytesWritten != static_cast<size_t>(byteCount)) {
			std::fclose(fileHandle);
			Throw("Could not write all bytes to file: ", utf8FilePath);
		}
	}

	ThrowIfNotZero(std::fclose(fileHandle), "Could not close file after writing: ", utf8FilePath);
}

StartExportedFunc(open_file_for_read, const char* utf8FilePath, void** outHandle, int64_t* outLengthBytes) {
	native_impl_file_io::open_file_for_read(utf8FilePath, outHandle, outLengthBytes);
	EndExportedFunc
}

StartExportedFunc(read_from_file, void* handle, uint8_t* dest, int32_t byteCount, int32_t* outBytesRead) {
	*outBytesRead = native_impl_file_io::read_from_file(handle, dest, byteCount);
	EndExportedFunc
}

StartExportedFunc(close_file, void* handle) {
	native_impl_file_io::close_file(handle);
	EndExportedFunc
}

StartExportedFunc(write_file, const char* utf8FilePath, const uint8_t* src, int64_t byteCount) {
	native_impl_file_io::write_file(utf8FilePath, src, byteCount);
	EndExportedFunc
}
