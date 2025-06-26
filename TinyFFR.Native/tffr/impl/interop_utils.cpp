#include "pch.h"
#include "interop_utils.h"


char* interop_utils::error_msg_buffer = new char[interop_utils::error_msg_buf_len];
char* interop_utils::err_msg_concat_space = new char[interop_utils::error_msg_buf_len];

void interop_utils::safe_copy_string(char* dest, size_t destLenBytes, const char* src) {
	auto srcLen = strlen(src);
	if (dest == nullptr || src == nullptr || srcLen >= destLenBytes) throw std::runtime_error{ "Prevented unsafe string copy" };
	strcpy(dest, src);
}

size_t accumulate_str(size_t cur_space_taken, const char* s) {
	interop_utils::safe_copy_string(interop_utils::err_msg_concat_space + cur_space_taken, interop_utils::error_msg_buf_len - cur_space_taken, s);
	return cur_space_taken + strlen(s);
}

void interop_utils::combine_in_concat_space(const char* a) {
	accumulate_str(0, a);
}

void interop_utils::combine_in_concat_space(const char* a, const char* b) {
	accumulate_str(accumulate_str(0, a), b);
}

void interop_utils::combine_in_concat_space(const char* a, const char* b, const char* c) {
	accumulate_str(accumulate_str(accumulate_str(0, a), b), c);
}

void interop_utils::combine_in_concat_space(const char* a, const char* b, const char* c, const char* d) {
	accumulate_str(accumulate_str(accumulate_str(accumulate_str(0, a), b), c), d);
}

void interop_utils::combine_in_concat_space(const char* a, const char* b, const char* c, const char* d, const char* e) {
	accumulate_str(accumulate_str(accumulate_str(accumulate_str(accumulate_str(0, a), b), c), d), e);
}

void interop_utils::copy_concat_space_to_err_buffer() {
	interop_utils::safe_copy_string(interop_utils::error_msg_buffer, interop_utils::error_msg_buf_len, interop_utils::err_msg_concat_space);
}

void interop_utils::int_str(char* inputArray, size_t inputArrayLen, int val) {
	snprintf(inputArray, inputArrayLen, "%d", val);
}


EXPORT_FUNC char* get_err_buffer() {
	return interop_utils::error_msg_buffer;
}

EXPORT_FUNC char inject_fake_error() {
	interop_utils::combine_in_concat_space("This is a fake error deliberately injected.");
	interop_utils::copy_concat_space_to_err_buffer();
	return 0;
}