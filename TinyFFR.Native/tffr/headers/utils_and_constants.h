#pragma once

#include "interop_utils.h"
#include "interop_result.h"

#pragma region Alloc/Dealloc

#define AlignedNew(type, alignment) new(_aligned_malloc(sizeof(type), alignment)) type

#define DeleteAndNull(ptr)			\
{									\
	delete (ptr); (ptr) = nullptr;	\
}									\

#define DeleteAndNullArray(ptr)			\
{										\
	delete[] (ptr); (ptr) = nullptr;	\
}										\

#pragma endregion

#pragma region Export and Interop

typedef uint8_t interop_bool;
#define interop_bool_true ((uint8_t) 255)
#define interop_bool_false ((uint8_t) 0)

#define PushSafeStructPacking _Pragma("pack(push, 1)")
#define PopSafeStructPacking _Pragma("pack(pop)")

#define ExportFuncFail(msg)																	\
{																							\
	interop_utils::combine_in_concat_space(func_name, " -> ", msg); \
	interop_utils::copy_concat_space_to_err_buffer(); \
	return interop_result::failure_int_val;															\
}

#define MacroStr(s) #s

#define StartExportedFunc(funcName, ...)																			\
	extern "C" __declspec(dllexport) uint8_t funcName(__VA_ARGS__) {					\
	static const char* func_name = MacroStr(funcName); \
	try																												\

#define EndExportedFunc									\
		return interop_result::success_int_val;					\
	}													\
	catch (std::exception& e) {							\
		ExportFuncFail(e.what());						\
	}													\
	catch (...) {										\
		ExportFuncFail("Unknown exception occurred.");	\
	}													\

#pragma endregion

#pragma region Misc

#define ReturnUnlessNull(ptr, ...)	\
	{ \
	if ((ptr) != nullptr) return (ptr); \
	interop_utils::combine_in_concat_space(__VA_ARGS__); \
	throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#define ThrowIfNull(ptr, ...)	\
	if ((ptr) == nullptr) { \
		interop_utils::combine_in_concat_space(__VA_ARGS__); \
		throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#define ThrowIfNegative(val, ...) \
	if ((val) < 0) { \
		interop_utils::combine_in_concat_space(__VA_ARGS__); \
		throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#define ThrowIfNotPositive(val, ...) \
	if ((val) <= 0) { \
		interop_utils::combine_in_concat_space(__VA_ARGS__); \
		throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#define ThrowIfNotZero(val, ...) \
	if ((val) != 0) { \
		interop_utils::combine_in_concat_space(__VA_ARGS__); \
		throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#define Throw(...) \
	{ \
	interop_utils::combine_in_concat_space(__VA_ARGS__); \
	throw std::exception{ interop_utils::err_msg_concat_space }; \
	} \

#pragma endregion