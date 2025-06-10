include(CMakeFindDependencyMacro)
find_dependency(SPIRV-Tools)
include(${CMAKE_CURRENT_LIST_DIR}/SPIRV-Tools-toolsTargets.cmake)
