# Install script for directory: C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/third_party/spirv-tools/source

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "install")
endif()
string(REGEX REPLACE "/$" "" CMAKE_INSTALL_PREFIX "${CMAKE_INSTALL_PREFIX}")

# Set the install configuration name.
if(NOT DEFINED CMAKE_INSTALL_CONFIG_NAME)
  if(BUILD_TYPE)
    string(REGEX REPLACE "^[^A-Za-z0-9_]+" ""
           CMAKE_INSTALL_CONFIG_NAME "${BUILD_TYPE}")
  else()
    set(CMAKE_INSTALL_CONFIG_NAME "Release")
  endif()
  message(STATUS "Install configuration: \"${CMAKE_INSTALL_CONFIG_NAME}\"")
endif()

# Set the component getting installed.
if(NOT CMAKE_INSTALL_COMPONENT)
  if(COMPONENT)
    message(STATUS "Install component: \"${COMPONENT}\"")
    set(CMAKE_INSTALL_COMPONENT "${COMPONENT}")
  else()
    set(CMAKE_INSTALL_COMPONENT)
  endif()
endif()

# Is this installation the result of a crosscompile?
if(NOT DEFINED CMAKE_CROSSCOMPILING)
  set(CMAKE_CROSSCOMPILING "FALSE")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/opt/cmake_install.cmake")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/reduce/cmake_install.cmake")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/fuzz/cmake_install.cmake")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/link/cmake_install.cmake")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/lint/cmake_install.cmake")
endif()

if(NOT CMAKE_INSTALL_LOCAL_ONLY)
  # Include the install script for the subdirectory.
  include("C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/diff/cmake_install.cmake")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Debug/SPIRV-Tools.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Release/SPIRV-Tools.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/MinSizeRel/SPIRV-Tools.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/RelWithDebInfo/SPIRV-Tools.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Debug/SPIRV-Tools-shared.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Release/SPIRV-Tools-shared.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/MinSizeRel/SPIRV-Tools-shared.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/RelWithDebInfo/SPIRV-Tools-shared.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Debug/SPIRV-Tools-shared.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/Release/SPIRV-Tools-shared.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/MinSizeRel/SPIRV-Tools-shared.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/RelWithDebInfo/SPIRV-Tools-shared.dll")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake/SPIRV-ToolsTarget.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake/SPIRV-ToolsTarget.cmake"
         "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake/SPIRV-ToolsTarget-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake/SPIRV-ToolsTarget.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/third_party/spirv-tools/source/CMakeFiles/Export/7c927e37733d95dcbdcc190c05ffd3eb/SPIRV-ToolsTarget-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/SPIRV-Tools/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/SPIRV-ToolsConfig.cmake")
endif()

