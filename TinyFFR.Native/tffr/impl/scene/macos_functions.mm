extern "C" void* macos_get_cocoa_view(NSWindow* nsWindow) {
    NSView* view = [nsWindow contentView];
    return view;
}