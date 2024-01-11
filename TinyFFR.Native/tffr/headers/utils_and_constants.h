#pragma once

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

static constexpr int FailMessageMemSize = 1000;
static constexpr int MaxFailMessageLength = FailMessageMemSize - 1;

#define PushSafeStructPacking _Pragma("pack(push, 1)")
#define PopSafeStructPacking _Pragma("pack(pop)")

#define ExportFuncFail(msg)																	\
{																							\
	strncat_s(ptrToFailMessageMemory, FailMessageMemSize, msg, MaxFailMessageLength);		\
	return interop_bool::false_val;															\
}

#define StartExportedFunc(funcName, ...)																			\
	extern "C" __declspec(dllexport) uint8_t funcName(char* ptrToFailMessageMemory, __VA_ARGS__) {					\
	try																												\

#define EndExportedFunc									\
		return interop_bool::true_val;					\
	}													\
	catch (std::exception& e) {							\
		ExportFuncFail(e.what());						\
	}													\
	catch (...) {										\
		ExportFuncFail("Unknown exception occurred.");	\
	}													\

#pragma endregion