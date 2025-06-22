cmake_minimum_required(VERSION 3.27)
project(TinyFFRNative)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

file(GLOB_RECURSE TFFR_SOURCES CONFIGURE_DEPENDS
    ${PROJECT_SOURCE_DIR}/tffr/impl/**/*.cpp
    ${PROJECT_SOURCE_DIR}/tffr/headers/**/*.h
)

add_library(TinyFFRNative SHARED ${TFFR_SOURCES})

target_include_directories(TinyFFRNative
    PUBLIC
        ${PROJECT_SOURCE_DIR}/third_party/headers/**/*.h
)

target_link_libraries(TinyFFRNative 
	PUBLIC 
		${PROJECT_SOURCE_DIR}/third_party/binaries/**/*.*
)
