cmake_minimum_required(VERSION 3.27)
project(TinyFFR.Native)

if (WIN32)
	message(FATAL_ERROR "Generally you should prefer building with MSBuild directly on Windows; there's already a ready-to-go project/solution in the repo and this script isn't necessarily kept up-to-date with it. Comment out this FATAL_ERROR line if you really want to use cmake!")
endif()

# set C++ standard to C++20
set(CMAKE_CXX_STANDARD 20)
# require the C++ standard
set(CMAKE_CXX_STANDARD_REQUIRED ON)
# disable compiler-specific extensions (we must be cross-plat compatible)
set(CMAKE_CXX_EXTENSIONS OFF)

if(UNIX AND NOT APPLE)
	# these two lines ensure that clang doesn't build the output .so with its ELF file set to look for the linked-in binaries hard-coded in their specific location they were during build (kind of insane that that's the default)
	# in other words, thanks to these two lines, the built .so will look for dependent libraries in its own folder rather than wherever they were when linked against
	set(CMAKE_INSTALL_RPATH "$ORIGIN")
	set(CMAKE_BUILD_WITH_INSTALL_RPATH ON)
elseif(APPLE)
	# same as above but apple-flavoured
	set(CMAKE_INSTALL_RPATH "@loader_path")
	set(CMAKE_BUILD_WITH_INSTALL_RPATH ON)
endif()

# this sets up TFFR_SOURCES which are all the .h and .cpp files in our lib
file(GLOB_RECURSE TFFR_SOURCES CONFIGURE_DEPENDS
    ${PROJECT_SOURCE_DIR}/tffr/impl/*.cpp
    ${PROJECT_SOURCE_DIR}/tffr/headers/*.h
)

# Add MacOS support functions (objective C)
if(APPLE)
    list(APPEND TFFR_SOURCES ${PROJECT_SOURCE_DIR}/tffr/impl/macos_functions.mm)
endif()

# this informs cmake that we want to build a library called "TinyFFR.Native"
add_library(TinyFFR.Native SHARED ${TFFR_SOURCES})

# add precompiled header support on Windows. On Linux this tends to just fail badly and it's not necessary
if (WIN32)
	target_precompile_headers(TinyFFR.Native PRIVATE pch.h)
endif()

# add all the header include directories
target_include_directories(TinyFFR.Native
    PUBLIC
        ${PROJECT_SOURCE_DIR}/
        ${PROJECT_SOURCE_DIR}/tffr/headers/
        ${PROJECT_SOURCE_DIR}/third_party/headers/
        ${PROJECT_SOURCE_DIR}/third_party/headers/filament/
)

# build!
if(WIN32)
	target_compile_definitions(TinyFFR.Native PRIVATE TFFR_WIN)
	file(GLOB FILAMENT_LIBS "${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/*.lib")
    target_link_libraries(TinyFFR.Native 
		PRIVATE 
			${PROJECT_SOURCE_DIR}/third_party/binaries/assimp/release/assimp-vc143-mt.lib
			${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/release/SDL2.lib
			${FILAMENT_LIBS}
			gdi32.lib
			user32.lib
			opengl32.lib
	)
	target_compile_options(TinyFFR.Native PRIVATE
        /permissive- /GS /W3 /Gy /Zc:wchar_t /Gm- /O2 /sdl /Zc:inline /fp:precise /WX- /Zc:forScope /Gd /Oi /MT /FC /EHsc /nologo /diagnostics:column 
    )
elseif(UNIX AND NOT APPLE)
	target_compile_definitions(TinyFFR.Native PRIVATE TFFR_LINUX)
    target_link_libraries(TinyFFR.Native 
		PRIVATE 
			${PROJECT_SOURCE_DIR}/third_party/binaries/assimp/$<LOWER_CASE:$<CONFIG>>/libassimp.so
			debug
				${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/$<LOWER_CASE:$<CONFIG>>/libSDL2d.so
			optimized
				${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/$<LOWER_CASE:$<CONFIG>>/libSDL2.so
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libfilament.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libbackend.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libbluegl.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libbluevk.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libfilabridge.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libfilaflat.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libutils.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libgeometry.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libsmol-v.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libibl.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libabseil.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libktxreader.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libimage.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/$<LOWER_CASE:$<CONFIG>>/libzstd.a
	)
elseif(APPLE)
	target_compile_definitions(TinyFFR.Native PRIVATE TFFR_MACOS)
    target_link_libraries(TinyFFR.Native 
		PRIVATE
			"-framework CoreFoundation"
			"-framework CoreVideo"
			"-framework CoreGraphics"
			"-framework Metal"
			"-framework QuartzCore"
			"-framework Cocoa"  
			${PROJECT_SOURCE_DIR}/third_party/binaries/assimp/release/libassimp.dylib
			${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/release/libSDL2.dylib
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libfilament.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libbackend.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libbluegl.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libbluevk.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libfilabridge.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libfilaflat.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libutils.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libgeometry.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libsmol-v.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libibl.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libabseil.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libktxreader.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libimage.a
			${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/libzstd.a
	)
endif()
