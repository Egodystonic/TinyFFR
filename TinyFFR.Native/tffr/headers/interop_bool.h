#pragma once
#include <cstdint>

#pragma pack(push, 1)
class interop_bool {
public:
	static constexpr uint8_t true_int_val = static_cast<uint8_t>(255);
	static constexpr uint8_t false_int_val = static_cast<uint8_t>(0);

	static const interop_bool true_val;
	static const interop_bool false_val;

	interop_bool();
	interop_bool(bool b);
	interop_bool(uint8_t i);

	[[nodiscard]] bool to_bool() const;
	[[nodiscard]] uint8_t to_int() const;

	operator bool() const;
	operator uint8_t() const;

private:
	uint8_t _valAsInt;
};
#pragma pack(pop)