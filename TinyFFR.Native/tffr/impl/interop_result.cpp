#include "pch.h"
#include "interop_result.h"

const interop_result interop_result::success_val{ success_int_val };
const interop_result interop_result::failure_val{ failure_int_val };

interop_result::interop_result() : _valAsInt(static_cast<uint8_t>(failure_int_val)) {}
interop_result::interop_result(const bool b) : _valAsInt(b ? static_cast<uint8_t>(success_int_val) : static_cast<uint8_t>(failure_int_val)) {}
interop_result::interop_result(const uint8_t i) : _valAsInt(i == 0 ? failure_int_val : success_int_val) {}

bool interop_result::to_bool() const { return _valAsInt != failure_int_val; }
uint8_t interop_result::to_int() const { return _valAsInt; }

interop_result::operator bool() const { return to_bool(); }
interop_result::operator uint8_t() const { return to_int(); }

