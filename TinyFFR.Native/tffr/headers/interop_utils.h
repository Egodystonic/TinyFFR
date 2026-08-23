#pragma once

#ifdef TFFR_WIN
#define EXPORT_FUNC extern "C" __declspec(dllexport)
#else
#define EXPORT_FUNC extern "C"
#endif

class interop_utils {
public:
	static constexpr int error_msg_buf_len = 1001;
	static thread_local char error_msg_buffer[error_msg_buf_len];
	static thread_local char err_msg_concat_space[error_msg_buf_len];

	static size_t append_truncating(char* dest, size_t destLenBytes, size_t curLen, const char* src) noexcept;

	template<typename... TParts>
	static void combine_into(char* dest, size_t destLenBytes, TParts... parts) noexcept {
		if (dest == nullptr || destLenBytes == 0U) return;
		dest[0] = '\0';
		size_t used = 0U;
		((used = append_truncating(dest, destLenBytes, used, parts)), ...);
	}

	template<typename... TParts>
	static void combine_in_concat_space(TParts... parts) noexcept {
		combine_into(err_msg_concat_space, error_msg_buf_len, parts...);
	}

	static void copy_concat_space_to_err_buffer() noexcept;

	static void safe_copy_string(char* dest, size_t destLenBytes, const char* src);

	static void int_str(char* inputArray, size_t inputArrayLen, int val);
	static void float_str(char* inputArray, size_t inputArrayLen, float val);
};
