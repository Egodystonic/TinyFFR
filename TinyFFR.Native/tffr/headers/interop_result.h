#pragma once
#include <cstdint>

#pragma pack(push, 1)
class interop_result {
public:
	static constexpr uint8_t success_int_val = static_cast<uint8_t>(255);
	static constexpr uint8_t failure_int_val = static_cast<uint8_t>(0);

	static const interop_result success_val;
	static const interop_result failure_val;

	interop_result();
	interop_result(bool b);
	interop_result(uint8_t i);

	[[nodiscard]] bool to_bool() const;
	[[nodiscard]] uint8_t to_int() const;

	operator bool() const;
	operator uint8_t() const;

private:
	uint8_t _valAsInt;
};
#pragma pack(pop)