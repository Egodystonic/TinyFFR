#include "pch.h"
#include "assets/native_impl_texture_compression.h"

#include <cstring>
#include <mutex>

#include "bc7enc/rgbcx.cpp"
#include "bc7enc/bc7enc.cpp"
#include "bc7enc/bc7decomp.cpp"

static constexpr uint32_t BlockDimension = 4U;
static constexpr uint32_t BlockTexelCount = BlockDimension * BlockDimension;
static constexpr uint32_t BlockSourceByteCount = BlockTexelCount * 4U; // All compression expected on RGBA8

static constexpr uint32_t DxtLevelEffortMap[] = { 0U, 5U, 8U, 11U, 14U, 18U };
static constexpr uint32_t Bc7UberLevelEffortMap[] = { 0U, 0U, 1U, 2U, 3U, 4U };
static constexpr uint32_t Bc7MaxPartitionsEffortMap[] = { 0U, 16U, 32U, 48U, 64U, 64U };

static std::once_flag encoders_init_flag;

void native_impl_texture_compression::initialize_encoders() {
	std::call_once(encoders_init_flag, []() {
		rgbcx::init();
		bc7enc_compress_block_init();
	});
}

int32_t native_impl_texture_compression::get_format_block_size_bytes(int32_t formatId) {
	switch (formatId) {
		case TFFR_COMPRESSION_FORMAT_BC1_SRGB: return 8;
		case TFFR_COMPRESSION_FORMAT_BC3_SRGB:
		case TFFR_COMPRESSION_FORMAT_BC5:
		case TFFR_COMPRESSION_FORMAT_BC7_SRGB:
		case TFFR_COMPRESSION_FORMAT_BC7_LINEAR: return 16;
		default: Throw("Unknown or uncompressed texture compression format ID.");
	}
}

static void gather_block(const uint8_t* srcRgba, uint32_t width, uint32_t height, uint32_t blockX, uint32_t blockY, uint8_t* destBlock) {
	for (uint32_t y = 0U; y < BlockDimension; ++y) {
		const uint32_t sourceY = std::min(blockY * BlockDimension + y, height - 1U);
		for (uint32_t x = 0U; x < BlockDimension; ++x) {
			const uint32_t sourceX = std::min(blockX * BlockDimension + x, width - 1U);
			const uint8_t* sourceTexel = srcRgba + (static_cast<size_t>(sourceY) * width + sourceX) * 4U;
			uint8_t* destTexel = destBlock + (y * BlockDimension + x) * 4U;
			destTexel[0] = sourceTexel[0];
			destTexel[1] = sourceTexel[1];
			destTexel[2] = sourceTexel[2];
			destTexel[3] = sourceTexel[3];
		}
	}
}

void native_impl_texture_compression::compress_texture_level(const void* srcRgbaPtr, uint32_t width, uint32_t height, int32_t formatId, int32_t effortLevel, void* destPtr, int32_t destLen) {
	ThrowIfNull(srcRgbaPtr, "Source pointer was null.");
	ThrowIfNull(destPtr, "Destination pointer was null.");
	ThrowIfNegative(destLen, "Destination length was negative.");
	ThrowIf(width == 0U || height == 0U, "Texture level dimensions must both be positive.");
	ThrowIf(effortLevel < 0 || effortLevel > 5, "Effort level was out of range.");
	initialize_encoders();

	const uint32_t blocksWide = (width + BlockDimension - 1U) / BlockDimension;
	const uint32_t blocksHigh = (height + BlockDimension - 1U) / BlockDimension;
	const int32_t blockSizeBytes = get_format_block_size_bytes(formatId);
	const int64_t requiredBytes = static_cast<int64_t>(blocksWide) * blocksHigh * blockSizeBytes;
	ThrowIf(requiredBytes > destLen, "Destination buffer was too small for the compressed texture level.");

	bc7enc_compress_block_params bc7Params{};
	if (formatId == TFFR_COMPRESSION_FORMAT_BC7_SRGB || formatId == TFFR_COMPRESSION_FORMAT_BC7_LINEAR) {
		bc7enc_compress_block_params_init(&bc7Params);
		if (formatId == TFFR_COMPRESSION_FORMAT_BC7_SRGB) bc7enc_compress_block_params_init_perceptual_weights(&bc7Params);
		else bc7enc_compress_block_params_init_linear_weights(&bc7Params);
		bc7Params.m_uber_level = Bc7UberLevelEffortMap[effortLevel];
		bc7Params.m_max_partitions = Bc7MaxPartitionsEffortMap[effortLevel];
	}

	const uint8_t* srcRgba = static_cast<const uint8_t*>(srcRgbaPtr);
	uint8_t* dest = static_cast<uint8_t*>(destPtr);
	uint8_t blockTexels[BlockSourceByteCount];

	for (uint32_t blockY = 0u; blockY < blocksHigh; ++blockY) {
		for (uint32_t blockX = 0u; blockX < blocksWide; ++blockX) {
			gather_block(srcRgba, width, height, blockX, blockY, blockTexels);
			uint8_t* destBlock = dest + (static_cast<size_t>(blockY) * blocksWide + blockX) * blockSizeBytes;

			switch (formatId) {
				case TFFR_COMPRESSION_FORMAT_BC1_SRGB:
					rgbcx::encode_bc1(DxtLevelEffortMap[effortLevel], destBlock, blockTexels, false, false);
					break;
				case TFFR_COMPRESSION_FORMAT_BC3_SRGB:
					rgbcx::encode_bc3(DxtLevelEffortMap[effortLevel], destBlock, blockTexels);
					break;
				case TFFR_COMPRESSION_FORMAT_BC5:
					rgbcx::encode_bc5(destBlock, blockTexels, 0, 1, 4);
					break;
				case TFFR_COMPRESSION_FORMAT_BC7_SRGB:
				case TFFR_COMPRESSION_FORMAT_BC7_LINEAR:
					bc7enc_compress_block(destBlock, blockTexels, &bc7Params);
					break;
				default:
					Throw("Unknown or uncompressed texture compression format ID.");
			}
		}
	}
}
StartExportedFunc(compress_texture_level, const void* srcRgbaPtr, uint32_t width, uint32_t height, int32_t formatId, int32_t effortLevel, void* destPtr, int32_t destLen) {
	native_impl_texture_compression::compress_texture_level(srcRgbaPtr, width, height, formatId, effortLevel, destPtr, destLen);
	EndExportedFunc
}

void native_impl_texture_compression::decompress_texture_level(const void* srcBlocksPtr, int32_t srcLen, uint32_t width, uint32_t height, int32_t formatId, void* destRgbaPtr, int32_t destLen) {
	ThrowIfNull(srcBlocksPtr, "Source pointer was null.");
	ThrowIfNull(destRgbaPtr, "Destination pointer was null.");
	ThrowIfNegative(srcLen, "Source length was negative.");
	ThrowIfNegative(destLen, "Destination length was negative.");
	ThrowIf(width == 0U || height == 0U, "Texture level dimensions must both be positive.");
	initialize_encoders();

	const uint32_t blocksWide = (width + BlockDimension - 1u) / BlockDimension;
	const uint32_t blocksHigh = (height + BlockDimension - 1u) / BlockDimension;
	const int32_t blockSizeBytes = get_format_block_size_bytes(formatId);
	ThrowIf(static_cast<int64_t>(blocksWide) * blocksHigh * blockSizeBytes > srcLen, "Source buffer was too small for the compressed texture level.");
	ThrowIf(static_cast<int64_t>(width) * height * 4 > destLen, "Destination buffer was too small for the decompressed texture level.");

	const uint8_t* src = static_cast<const uint8_t*>(srcBlocksPtr);
	uint8_t* destRgba = static_cast<uint8_t*>(destRgbaPtr);
	uint8_t blockTexels[BlockSourceByteCount];

	for (uint32_t blockY = 0u; blockY < blocksHigh; ++blockY) {
		for (uint32_t blockX = 0u; blockX < blocksWide; ++blockX) {
			const uint8_t* srcBlock = src + (static_cast<size_t>(blockY) * blocksWide + blockX) * blockSizeBytes;
			std::memset(blockTexels, 0, sizeof(blockTexels));

			switch (formatId) {
				case TFFR_COMPRESSION_FORMAT_BC1_SRGB:
					rgbcx::unpack_bc1(srcBlock, blockTexels);
					break;
				case TFFR_COMPRESSION_FORMAT_BC3_SRGB:
					rgbcx::unpack_bc3(srcBlock, blockTexels);
					break;
				case TFFR_COMPRESSION_FORMAT_BC5:
					rgbcx::unpack_bc5(srcBlock, blockTexels, 0, 1, 4);
					break;
				case TFFR_COMPRESSION_FORMAT_BC7_SRGB:
				case TFFR_COMPRESSION_FORMAT_BC7_LINEAR:
					bc7decomp::unpack_bc7(srcBlock, reinterpret_cast<bc7decomp::color_rgba*>(blockTexels));
					break;
				default:
					Throw("Unknown or uncompressed texture compression format ID.");
			}

			for (uint32_t y = 0u; y < BlockDimension; ++y) {
				const uint32_t destY = blockY * BlockDimension + y;
				if (destY >= height) break;
				for (uint32_t x = 0u; x < BlockDimension; ++x) {
					const uint32_t destX = blockX * BlockDimension + x;
					if (destX >= width) break;
					const uint8_t* sourceTexel = blockTexels + (y * BlockDimension + x) * 4u;
					uint8_t* destTexel = destRgba + (static_cast<size_t>(destY) * width + destX) * 4u;
					destTexel[0] = sourceTexel[0];
					destTexel[1] = sourceTexel[1];
					destTexel[2] = sourceTexel[2];
					destTexel[3] = sourceTexel[3];
				}
			}
		}
	}
}
StartExportedFunc(decompress_texture_level, const void* srcBlocksPtr, int32_t srcLen, uint32_t width, uint32_t height, int32_t formatId, void* destRgbaPtr, int32_t destLen) {
	native_impl_texture_compression::decompress_texture_level(srcBlocksPtr, srcLen, width, height, formatId, destRgbaPtr, destLen);
	EndExportedFunc
}
