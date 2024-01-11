#include "pch.h"
#include "interop_bool.h"

const interop_bool interop_bool::true_val{ true_int_val };
const interop_bool interop_bool::false_val{ false_int_val };

interop_bool::interop_bool() : _valAsInt(static_cast<uint8_t>(false_int_val)) {}
interop_bool::interop_bool(const bool b) : _valAsInt(b ? static_cast<uint8_t>(true_int_val) : static_cast<uint8_t>(false_int_val)) {}
interop_bool::interop_bool(const uint8_t i) : _valAsInt(i == 0 ? false_int_val : true_int_val) {}

bool interop_bool::to_bool() const { return _valAsInt != false_int_val; }
uint8_t interop_bool::to_int() const { return _valAsInt; }

interop_bool::operator bool() const { return to_bool(); }
interop_bool::operator uint8_t() const { return to_int(); }

