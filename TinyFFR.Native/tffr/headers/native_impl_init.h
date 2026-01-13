#pragma once

#if !defined(TFFR_WIN) && !defined(TFFR_LINUX) && !defined(TFFR_MACOS)
#error "Require definition of at least one of TFFR_ platform specifier"
#endif
#include "utils_and_constants.h"

typedef void* BufferIdentity;
typedef void(*deallocate_asset_buffer_delegate)(BufferIdentity bufferIdentity);
typedef void(*log_notify_delegate)();

class native_impl_init {
public:
	static filament::Engine* filament_engine_ptr;
	static deallocate_asset_buffer_delegate deallocation_delegate;
	static log_notify_delegate log_delegate;
	
	static void exec_once_only_initialization();
	static void set_buffer_deallocation_delegate(deallocate_asset_buffer_delegate deallocationDelegate);
	static void set_log_notify_delegate(log_notify_delegate logNotifyDelegate);
	static void notify_of_log_msg();

	static void on_factory_build(interop_bool enableVsync, uint32_t commandBufferSizeMb, interop_bool furtherReduceMemoryUsage, int32_t renderingApiIndex);
	static void on_factory_teardown();
};