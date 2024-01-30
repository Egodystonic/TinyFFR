#include "pch.h"
#include "environment/native_impl_loop.h"

#include "utils_and_constants.h"

void native_impl_loop::iterate_events() {
	
}
StartExportedFunc(iterate_events) {
	native_impl_loop::iterate_events();
	EndExportedFunc
}
