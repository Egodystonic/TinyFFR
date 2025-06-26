#pragma once

#if !defined(TFFR_WIN) && !defined(TFFR_LINUX) && !defined(TFFR_MACOS)
#error "Require definition of at least one of TFFR_ platform specifier"
#endif

typedef void* BufferIdentity;
typedef void(*deallocate_asset_buffer_delegate)(BufferIdentity bufferIdentity);
typedef void(*log_notify_delegate)();

class native_impl_init {
public:
	static filament::Engine* filament_engine_ptr;
	static deallocate_asset_buffer_delegate deallocation_delegate;
	static log_notify_delegate log_delegate;
	
	static void initialize_all();
	static void set_buffer_deallocation_delegate(deallocate_asset_buffer_delegate deallocationDelegate);
	static void set_log_notify_delegate(log_notify_delegate logNotifyDelegate);
	static void notify_of_log_msg();
};