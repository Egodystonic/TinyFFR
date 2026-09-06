#import <QuartzCore/QuartzCore.h>

extern "C" void macos_mark_metal_layer_opaque(void* metalLayer) {
    CAMetalLayer* layer = (CAMetalLayer*) metalLayer;
    layer.opaque = YES;
}
