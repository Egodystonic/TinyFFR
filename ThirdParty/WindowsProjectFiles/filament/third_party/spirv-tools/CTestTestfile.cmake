# CMake generated Testfile for 
# Source directory: C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools
# Build directory: C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools
# 
# This file includes the relevant testing commands required for 
# testing this directory and lists subdirectories to be tested as well.
if(CTEST_CONFIGURATION_TYPE MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
  add_test([=[spirv-tools-copyrights]=] "C:/Users/ben/AppData/Local/Programs/Python/Python313/python.exe" "utils/check_copyright.py")
  set_tests_properties([=[spirv-tools-copyrights]=] PROPERTIES  WORKING_DIRECTORY "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools" _BACKTRACE_TRIPLES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;355;add_test;C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;0;")
elseif(CTEST_CONFIGURATION_TYPE MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
  add_test([=[spirv-tools-copyrights]=] "C:/Users/ben/AppData/Local/Programs/Python/Python313/python.exe" "utils/check_copyright.py")
  set_tests_properties([=[spirv-tools-copyrights]=] PROPERTIES  WORKING_DIRECTORY "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools" _BACKTRACE_TRIPLES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;355;add_test;C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;0;")
elseif(CTEST_CONFIGURATION_TYPE MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
  add_test([=[spirv-tools-copyrights]=] "C:/Users/ben/AppData/Local/Programs/Python/Python313/python.exe" "utils/check_copyright.py")
  set_tests_properties([=[spirv-tools-copyrights]=] PROPERTIES  WORKING_DIRECTORY "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools" _BACKTRACE_TRIPLES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;355;add_test;C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;0;")
elseif(CTEST_CONFIGURATION_TYPE MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
  add_test([=[spirv-tools-copyrights]=] "C:/Users/ben/AppData/Local/Programs/Python/Python313/python.exe" "utils/check_copyright.py")
  set_tests_properties([=[spirv-tools-copyrights]=] PROPERTIES  WORKING_DIRECTORY "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools" _BACKTRACE_TRIPLES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;355;add_test;C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/CMakeLists.txt;0;")
else()
  add_test([=[spirv-tools-copyrights]=] NOT_AVAILABLE)
endif()
subdirs("external")
subdirs("source")
subdirs("tools")
subdirs("test")
subdirs("examples")
