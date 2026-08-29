#pragma once

#include "utils_and_constants.h"

#define TFFR_COMPRESSION_FORMAT_NONE 0
#define TFFR_COMPRESSION_FORMAT_BC1_SRGB 1
#define TFFR_COMPRESSION_FORMAT_BC3_SRGB 2
#define TFFR_COMPRESSION_FORMAT_BC5 3
#define TFFR_COMPRESSION_FORMAT_BC7_SRGB 4
#define TFFR_COMPRESSION_FORMAT_BC7_LINEAR 5

class native_impl_texture_compression {
public:
	static void initialize_encoders();
	static int32_t get_format_block_size_bytes(int32_t formatId);
	static void compress_texture_level(const void* srcRgbaPtr, uint32_t width, uint32_t height, int32_t formatId, int32_t effortLevel, void* destPtr, int32_t destLen);
	static void decompress_texture_level(const void* srcBlocksPtr, int32_t srcLen, uint32_t width, uint32_t height, int32_t formatId, void* destRgbaPtr, int32_t destLen);
};
