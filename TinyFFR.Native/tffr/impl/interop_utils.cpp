#include "pch.h"
#include "interop_utils.h"
#include "utils_and_constants.h"


thread_local char interop_utils::error_msg_buffer[interop_utils::error_msg_buf_len]{};
thread_local char interop_utils::err_msg_concat_space[interop_utils::error_msg_buf_len]{};

size_t interop_utils::append_truncating(char* dest, size_t destLenBytes, size_t curLen, const char* src) noexcept {
	if (dest == nullptr || destLenBytes == 0U) return curLen;

	const auto maxLen = destLenBytes - 1U;
	if (curLen >= maxLen) {
		dest[maxLen] = '\0';
		return maxLen;
	}

	if (src == nullptr) src = "<null>";

	const auto remaining = maxLen - curLen;
	size_t i = 0U;
	while (i < remaining && src[i] != '\0') {
		dest[curLen + i] = src[i];
		++i;
	}
	dest[curLen + i] = '\0';

	if (src[i] != '\0' && destLenBytes >= 4U) {
		dest[destLenBytes - 4U] = '.';
		dest[destLenBytes - 3U] = '.';
		dest[destLenBytes - 2U] = '.';
		dest[destLenBytes - 1U] = '\0';
		return maxLen;
	}

	return curLen + i;
}

void interop_utils::copy_concat_space_to_err_buffer() noexcept {
	append_truncating(error_msg_buffer, error_msg_buf_len, 0U, err_msg_concat_space);
}

void interop_utils::safe_copy_string(char* dest, size_t destLenBytes, const char* src) {
	if (dest == nullptr || src == nullptr) throw std::runtime_error{ "Prevented unsafe string copy" };
	auto srcLen = strlen(src);
	if (srcLen >= destLenBytes) throw std::runtime_error{ "Prevented unsafe string copy" };
	strcpy(dest, src);
}

void interop_utils::int_str(char* inputArray, size_t inputArrayLen, int val) {
	snprintf(inputArray, inputArrayLen, "%d", val);
}
void interop_utils::float_str(char* inputArray, size_t inputArrayLen, float val) {
	snprintf(inputArray, inputArrayLen, "%f", val);
}


EXPORT_FUNC char* get_err_buffer() {
	return interop_utils::error_msg_buffer;
}

EXPORT_FUNC int32_t get_err_buffer_length() {
	return interop_utils::error_msg_buf_len;
}

StartExportedFunc(inject_fake_error, const char* msg) {
	Throw(msg == nullptr ? "<null>" : msg);
	EndExportedFunc
}
