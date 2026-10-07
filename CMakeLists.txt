cmake_minimum_required(VERSION 3.21)
project(HKTKOAuthWindows VERSION 1.3.13 LANGUAGES C CXX)

if(NOT WIN32)
  message(FATAL_ERROR "HKTK OAuth Windows SDK only supports Windows")
endif()

add_library(HKTKSDK SHARED IMPORTED GLOBAL)
set_target_properties(HKTKSDK PROPERTIES
  IMPORTED_IMPLIB "${CMAKE_CURRENT_LIST_DIR}/native/lib/libHKTKSDK.dll.a"
  IMPORTED_LOCATION "${CMAKE_CURRENT_LIST_DIR}/native/bin/HKTKSDK.dll"
  INTERFACE_INCLUDE_DIRECTORIES "${CMAKE_CURRENT_LIST_DIR}/native/include"
)
add_library(HKTKSDK::HKTKSDK ALIAS HKTKSDK)
