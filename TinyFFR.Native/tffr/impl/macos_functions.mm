#import <QuartzCore/QuartzCore.h>
#import <Metal/Metal.h>

#include "utils_and_constants.h"

extern "C" void macos_mark_metal_layer_opaque(void* metalLayer) {
    CAMetalLayer* layer = (CAMetalLayer*) metalLayer;
    layer.opaque = YES;
}

extern "C" void macos_set_metal_layer_vsync(void* metalLayer, interop_bool enabled) {
    CAMetalLayer* layer = (CAMetalLayer*) metalLayer;
    layer.displaySyncEnabled = enabled ? YES : NO;
}

extern "C" void* macos_create_metal_shared_event() {
    id<MTLDevice> device = MTLCreateSystemDefaultDevice();
    if (device == nil) return nullptr;
    id<MTLSharedEvent> sharedEvent = [device newSharedEvent];
    [device release];
    return (void*) sharedEvent;
}

extern "C" uint64_t macos_get_metal_shared_event_value(void* sharedEvent) {
    return ((id<MTLSharedEvent>) sharedEvent).signaledValue;
}

extern "C" void macos_set_metal_shared_event_value(void* sharedEvent, uint64_t value) {
    ((id<MTLSharedEvent>) sharedEvent).signaledValue = value;
}
