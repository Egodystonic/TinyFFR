// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H
#define _CRT_SECURE_NO_WARNINGS

// add headers that you want to pre-compile here
#include <string>
#include <cstdint>
#include <exception>
#include <stdexcept>

#include "sdl/SDL.h"

#include "filament/filament/Engine.h"
#include "filament/math/half.h"
#include "filament/math/mat2.h"
#include "filament/math/mat3.h"
#include "filament/math/mat4.h"
#include "filament/math/norm.h"
#include "filament/math/quat.h"
#include "filament/math/scalar.h"
#include "filament/math/TMatHelpers.h"
#include "filament/math/TQuatHelpers.h"
#include "filament/math/TVecHelpers.h"
#include "filament/math/vec2.h"
#include "filament/math/vec3.h"
#include "filament/math/vec4.h"
#include "utils/Panic.h"

#endif //PCH_H
