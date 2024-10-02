#pragma once

typedef void* BufferIdentity;
typedef void(*deallocate_asset_buffer_delegate)(BufferIdentity bufferIdentity);

class native_impl_init {
public:
	static filament::Engine* filament_engine_ptr;
	static deallocate_asset_buffer_delegate deallocation_delegate;

	static void initialize_all();
	static void set_buffer_deallocation_delegate(deallocate_asset_buffer_delegate deallocationDelegate);
};