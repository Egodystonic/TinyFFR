#pragma once

#include "utils_and_constants.h"

#include <cstdio>

class native_impl_file_io {
public:
	static void open_file_for_read(const char* utf8FilePath, void** outHandle, int64_t* outLengthBytes);
	static int32_t read_from_file(void* handle, uint8_t* dest, int32_t byteCount);
	static void close_file(void* handle);
	static void write_file(const char* utf8FilePath, const uint8_t* src, int64_t byteCount);
};
