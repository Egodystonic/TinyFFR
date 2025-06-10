# Install script for directory: C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl

# Set the install prefix
if(NOT DEFINED CMAKE_INSTALL_PREFIX)
  set(CMAKE_INSTALL_PREFIX "C:/Program Files (x86)/SDL2")
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

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Debug/SDL2d.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Release/SDL2.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/MinSizeRel/SDL2.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY OPTIONAL FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/RelWithDebInfo/SDL2.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Debug/SDL2d.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Release/SDL2.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/MinSizeRel/SDL2.dll")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/bin" TYPE SHARED_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/RelWithDebInfo/SDL2.dll")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Debug/SDL2maind.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Release/SDL2main.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/MinSizeRel/SDL2main.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/RelWithDebInfo/SDL2main.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Debug/SDL2-staticd.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Release/SDL2-static.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/MinSizeRel/SDL2-static.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/RelWithDebInfo/SDL2-static.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Debug/SDL2_test.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/Release/SDL2_test.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/MinSizeRel/SDL2_test.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/RelWithDebInfo/SDL2_test.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake"
         "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2Targets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2Targets-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake"
         "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2mainTargets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2mainTargets-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2staticTargets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2staticTargets.cmake"
         "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2staticTargets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2staticTargets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2staticTargets-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(EXISTS "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2testTargets.cmake")
    file(DIFFERENT _cmake_export_file_changed FILES
         "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2testTargets.cmake"
         "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets.cmake")
    if(_cmake_export_file_changed)
      file(GLOB _cmake_old_config_files "$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2testTargets-*.cmake")
      if(_cmake_old_config_files)
        string(REPLACE ";" ", " _cmake_old_config_files_text "${_cmake_old_config_files}")
        message(STATUS "Old export file \"$ENV{DESTDIR}${CMAKE_INSTALL_PREFIX}/cmake/SDL2testTargets.cmake\" will be replaced.  Removing files [${_cmake_old_config_files_text}].")
        unset(_cmake_old_config_files_text)
        file(REMOVE ${_cmake_old_config_files})
      endif()
      unset(_cmake_old_config_files)
    endif()
    unset(_cmake_export_file_changed)
  endif()
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets.cmake")
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets-debug.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets-minsizerel.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets-relwithdebinfo.cmake")
  endif()
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/CMakeFiles/Export/272ceadb8458515b2ae4b5630a6029cc/SDL2testTargets-release.cmake")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Devel" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/cmake" TYPE FILE FILES
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/SDL2Config.cmake"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/SDL2ConfigVersion.cmake"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/cmake/sdlfind.cmake"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/SDL2" TYPE FILE FILES
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_atomic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_audio.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_bits.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_blendmode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_clipboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_copying.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_cpuinfo.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_egl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_endian.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_error.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_events.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_filesystem.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gamecontroller.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gesture.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_guid.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_haptic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hidapi.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hints.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_joystick.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keyboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keycode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_loadso.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_locale.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_main.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_messagebox.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_metal.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_misc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mouse.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mutex.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_name.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl_glext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2ext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_khrplatform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_pixels.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_power.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_quit.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rect.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_render.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rwops.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_scancode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_sensor.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_shape.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_stdinc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_surface.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_system.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_syswm.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_common.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_compare.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_crc32.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_font.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_fuzzer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_harness.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_images.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_md5.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_memory.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_random.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_thread.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_timer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_touch.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_types.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_version.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_video.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_vulkan.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/begin_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/close_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include/SDL2/SDL_revision.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include-config-debug/SDL2/SDL_config.h"
      )
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/SDL2" TYPE FILE FILES
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_atomic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_audio.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_bits.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_blendmode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_clipboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_copying.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_cpuinfo.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_egl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_endian.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_error.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_events.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_filesystem.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gamecontroller.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gesture.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_guid.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_haptic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hidapi.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hints.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_joystick.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keyboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keycode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_loadso.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_locale.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_main.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_messagebox.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_metal.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_misc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mouse.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mutex.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_name.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl_glext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2ext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_khrplatform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_pixels.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_power.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_quit.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rect.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_render.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rwops.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_scancode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_sensor.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_shape.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_stdinc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_surface.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_system.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_syswm.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_common.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_compare.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_crc32.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_font.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_fuzzer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_harness.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_images.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_md5.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_memory.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_random.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_thread.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_timer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_touch.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_types.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_version.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_video.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_vulkan.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/begin_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/close_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include/SDL2/SDL_revision.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include-config-release/SDL2/SDL_config.h"
      )
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/SDL2" TYPE FILE FILES
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_atomic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_audio.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_bits.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_blendmode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_clipboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_copying.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_cpuinfo.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_egl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_endian.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_error.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_events.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_filesystem.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gamecontroller.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gesture.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_guid.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_haptic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hidapi.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hints.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_joystick.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keyboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keycode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_loadso.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_locale.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_main.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_messagebox.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_metal.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_misc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mouse.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mutex.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_name.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl_glext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2ext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_khrplatform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_pixels.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_power.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_quit.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rect.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_render.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rwops.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_scancode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_sensor.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_shape.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_stdinc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_surface.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_system.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_syswm.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_common.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_compare.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_crc32.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_font.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_fuzzer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_harness.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_images.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_md5.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_memory.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_random.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_thread.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_timer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_touch.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_types.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_version.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_video.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_vulkan.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/begin_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/close_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include/SDL2/SDL_revision.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include-config-minsizerel/SDL2/SDL_config.h"
      )
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/SDL2" TYPE FILE FILES
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_atomic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_audio.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_bits.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_blendmode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_clipboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_copying.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_cpuinfo.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_egl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_endian.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_error.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_events.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_filesystem.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gamecontroller.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_gesture.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_guid.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_haptic.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hidapi.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_hints.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_joystick.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keyboard.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_keycode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_loadso.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_locale.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_main.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_messagebox.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_metal.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_misc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mouse.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_mutex.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_name.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengl_glext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2ext.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_gl2platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_opengles2_khrplatform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_pixels.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_platform.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_power.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_quit.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rect.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_render.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_rwops.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_scancode.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_sensor.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_shape.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_stdinc.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_surface.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_system.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_syswm.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_assert.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_common.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_compare.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_crc32.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_font.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_fuzzer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_harness.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_images.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_log.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_md5.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_memory.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_test_random.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_thread.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_timer.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_touch.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_types.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_version.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_video.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/SDL_vulkan.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/begin_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/include/close_code.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include/SDL2/SDL_revision.h"
      "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/include-config-relwithdebinfo/SDL2/SDL_config.h"
      )
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/licenses/SDL2" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/sdl/LICENSE.txt")
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/pkgconfig" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/sdl2.pc")
endif()

if(CMAKE_INSTALL_COMPONENT)
  set(CMAKE_INSTALL_MANIFEST "install_manifest_${CMAKE_INSTALL_COMPONENT}.txt")
else()
  set(CMAKE_INSTALL_MANIFEST "install_manifest.txt")
endif()

string(REPLACE ";" "\n" CMAKE_INSTALL_MANIFEST_CONTENT
       "${CMAKE_INSTALL_MANIFEST_FILES}")
file(WRITE "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/sdl/${CMAKE_INSTALL_MANIFEST}"
     "${CMAKE_INSTALL_MANIFEST_CONTENT}")
