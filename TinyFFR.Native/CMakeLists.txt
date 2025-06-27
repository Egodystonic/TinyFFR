cmake_minimum_required(VERSION 3.27)
project(TinyFFRNative)

if (WIN32)
	message(FATAL_ERROR "Generally you should prefer building with MSBuild directly on Windows; there's already a ready-to-go project/solution in the repo and this script isn't necessarily kept up-to-date with it. Comment out this FATAL_ERROR line if you really want to use cmake!")
endif()

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

file(GLOB_RECURSE TFFR_SOURCES CONFIGURE_DEPENDS
    ${PROJECT_SOURCE_DIR}/tffr/impl/*.cpp
    ${PROJECT_SOURCE_DIR}/tffr/headers/*.h
)

add_library(TinyFFRNative SHARED ${TFFR_SOURCES})

if (WIN32)
	target_precompile_headers(TinyFFRNative PRIVATE pch.h)
endif()

target_include_directories(TinyFFRNative
    PUBLIC
        ${PROJECT_SOURCE_DIR}/
        ${PROJECT_SOURCE_DIR}/tffr/headers/
        ${PROJECT_SOURCE_DIR}/third_party/headers/
        ${PROJECT_SOURCE_DIR}/third_party/headers/filament/
)

if(WIN32)
	target_compile_definitions(TinyFFRNative PRIVATE TFFR_WIN)
	file(GLOB FILAMENT_LIBS "${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/*.lib")
    target_link_libraries(TinyFFRNative 
		PRIVATE 
			${PROJECT_SOURCE_DIR}/third_party/binaries/assimp/release/assimp-vc143-mt.lib
			${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/release/SDL2.lib
			${FILAMENT_LIBS}
			gdi32.lib
			user32.lib
			opengl32.lib
	)
	target_compile_options(TinyFFRNative PRIVATE
        /permissive- /GS /W3 /Gy /Zc:wchar_t /Gm- /O2 /sdl /Zc:inline /fp:precise /WX- /Zc:forScope /Gd /Oi /MT /FC /EHsc /nologo /diagnostics:column 
    )
elseif(UNIX)
	target_compile_definitions(TinyFFRNative PRIVATE TFFR_LINUX)
	file(GLOB FILAMENT_LIBS "${PROJECT_SOURCE_DIR}/third_party/binaries/filament/release/*.a")
    target_link_libraries(TinyFFRNative 
		PRIVATE 
			${PROJECT_SOURCE_DIR}/third_party/binaries/assimp/release/libassimp.so
			${PROJECT_SOURCE_DIR}/third_party/binaries/sdl/release/libSDL2.so
			${FILAMENT_LIBS}
	)
endif()