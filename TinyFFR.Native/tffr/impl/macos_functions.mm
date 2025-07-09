extern "C" void* get_cocoa_view(NSWindow* nsWindow) {
    NSView* view = [nsWindow contentView];
    return view;
}