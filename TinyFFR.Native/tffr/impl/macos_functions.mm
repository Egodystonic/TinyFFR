#import <QuartzCore/QuartzCore.h>

#include "utils_and_constants.h"

extern "C" void macos_mark_metal_layer_opaque(void* metalLayer) {
    CAMetalLayer* layer = (CAMetalLayer*) metalLayer;
    layer.opaque = YES;
}

extern "C" void macos_set_metal_layer_vsync(void* metalLayer, interop_bool enabled) {
    CAMetalLayer* layer = (CAMetalLayer*) metalLayer;
    layer.displaySyncEnabled = enabled ? YES : NO;
}
