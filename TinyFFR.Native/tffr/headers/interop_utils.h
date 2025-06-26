#pragma once

#ifdef TFFR_WIN
#define EXPORT_FUNC extern "C" __declspec(dllexport)
#else
#define EXPORT_FUNC extern "C"
#endif

class interop_utils {
public:
	static constexpr int error_msg_buf_len = 1001;
	static constexpr int error_msg_len = error_msg_buf_len - 1;
	static char* error_msg_buffer;
	static char* err_msg_concat_space;

	static void combine_in_concat_space(const char* a);
	static void combine_in_concat_space(const char* a, const char* b);
	static void combine_in_concat_space(const char* a, const char* b, const char* c);
	static void combine_in_concat_space(const char* a, const char* b, const char* c, const char* d);
	static void combine_in_concat_space(const char* a, const char* b, const char* c, const char* d, const char* e);
	static void copy_concat_space_to_err_buffer();

	static void safe_copy_string(char* dest, size_t destLenBytes, const char* src);

	static void int_str(char* inputArray, size_t inputArrayLen, int val);
};