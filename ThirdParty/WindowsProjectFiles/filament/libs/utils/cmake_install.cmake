# Install script for directory: C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils

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

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  if(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Dd][Ee][Bb][Uu][Gg])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/x86_64" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/libs/utils/Debug/utils.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ee][Aa][Ss][Ee])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/x86_64" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/libs/utils/Release/utils.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Mm][Ii][Nn][Ss][Ii][Zz][Ee][Rr][Ee][Ll])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/x86_64" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/libs/utils/MinSizeRel/utils.lib")
  elseif(CMAKE_INSTALL_CONFIG_NAME MATCHES "^([Rr][Ee][Ll][Ww][Ii][Tt][Hh][Dd][Ee][Bb][Ii][Nn][Ff][Oo])$")
    file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/lib/x86_64" TYPE STATIC_LIBRARY FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/WindowsProjectFiles/filament/libs/utils/RelWithDebInfo/utils.lib")
  endif()
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/utils" TYPE FILE FILES
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/algorithm.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/bitset.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/CallStack.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/debug.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Allocator.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/BitmaskEnum.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/compiler.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/compressed_pair.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/CString.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Entity.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/EntityInstance.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/EntityManager.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/FixedCapacityVector.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Invocable.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Log.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/memalign.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Mutex.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/NameComponentManager.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/ostream.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Panic.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Path.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/PrivateImplementation.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/PrivateImplementation-impl.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/SingleInstanceComponentManager.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Slice.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/StaticString.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/StructureOfArrays.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/Systrace.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/sstream.h"
    "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/unwindows.h"
    )
endif()

if(CMAKE_INSTALL_COMPONENT STREQUAL "Unspecified" OR NOT CMAKE_INSTALL_COMPONENT)
  file(INSTALL DESTINATION "${CMAKE_INSTALL_PREFIX}/include/utils/generic" TYPE FILE FILES "C:/Users/ben/Documents/Egodystonic/TinyFFR/Repository/ThirdParty/filament/libs/utils/include/utils/generic/Mutex.h")
endif()

