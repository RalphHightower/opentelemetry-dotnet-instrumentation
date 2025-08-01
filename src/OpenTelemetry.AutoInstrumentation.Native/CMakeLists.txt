# Copyright The OpenTelemetry Authors
# SPDX-License-Identifier: Apache-2.0

cmake_minimum_required (VERSION 3.10..3.19)
cmake_policy(SET CMP0015 NEW)

# ******************************************************
# Project definition
# ******************************************************

project("OpenTelemetry.AutoInstrumentation.Native" VERSION ${OTEL_AUTO_VERSION})

# ******************************************************
# Environment detection
# ******************************************************

# Detect operating system
if (CMAKE_SYSTEM_NAME MATCHES "Windows")
    message(FATAL_ERROR "Windows builds are not supported using CMAKE. Please use Visual Studio")
    SET(ISWINDOWS true)
elseif (CMAKE_SYSTEM_NAME MATCHES "Linux")
    message(STATUS "Preparing Linux build")
    SET(ISLINUX true)
elseif (CMAKE_SYSTEM_NAME MATCHES "Darwin")
    message(STATUS "Preparing macOS build")
    SET(ISMACOS true)
endif()

# Detect bitness of the build
if (CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(STATUS "Setting compilation for 64bits processor")
    SET(BIT64 true)
endif()

# Detect architecture
if (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
    message(STATUS "Architecture is x64/AMD64")
    SET(ISAMD64 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL x86 OR CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
    message(STATUS "Architecture is x86")
    SET(ISX86 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm64)
    message(STATUS "Architecture is ARM64")
    SET(ISARM64 true)
elseif (CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l OR CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
    message(STATUS "Architecture is ARM")
    SET(ISARM true)
endif()

# ******************************************************
# Detect prerequisites
# ******************************************************
find_program(FOUND_GIT git)
if (NOT FOUND_GIT)
    message(FATAL_ERROR "GIT is required to build the project")
else()
    message(STATUS "GIT found: ${FOUND_GIT}")
endif()

find_program(FOUND_GCC gcc)
if(NOT FOUND_GCC)
    message(FATAL_ERROR "GCC is required to build the project's dependencies")
else()
    message(STATUS "GCC found: ${FOUND_GCC}")
endif()

find_program(FOUND_CLANG clang)
if(NOT FOUND_CLANG)
    message(FATAL_ERROR "CLANG is required to build the project")
else()
    message(STATUS "CLANG found: ${FOUND_CLANG}")
endif()

find_program(FOUND_CLANGPP clang++)
if(NOT FOUND_CLANGPP)
    message(FATAL_ERROR "CLANG++ is required to build the project")
else()
    message(STATUS "CLANG++ found: ${FOUND_CLANGPP}")
endif()

# ******************************************************
# Output folders
# ******************************************************

# Set output folders
SET(OUTPUT_BIN_DIR ${CMAKE_BINARY_DIR}/bin)
SET(OUTPUT_DEPS_DIR ${CMAKE_BINARY_DIR}/deps)
FILE(MAKE_DIRECTORY ${OUTPUT_BIN_DIR})
FILE(MAKE_DIRECTORY ${OUTPUT_DEPS_DIR})

SET(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})
SET(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})
SET(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_BIN_DIR})

# ******************************************************
# Dependencies
# ******************************************************

# Add custom commands to setup dependencies via git

add_custom_command(
    OUTPUT ${OUTPUT_DEPS_DIR}/re2/obj/libre2.a
    COMMAND git clone -c advice.detachedHead=false --quiet --depth 1 --branch 2023-03-01 https://github.com/google/re2.git && cd re2 && env ARFLAGS=\"-r -s -c\" CXXFLAGS=\"-std=c++11 -O3 -g -fPIC\" make
    WORKING_DIRECTORY ${OUTPUT_DEPS_DIR}
)

add_custom_command(
    OUTPUT ${OUTPUT_DEPS_DIR}/fmt/libfmt.a
    COMMAND cp -R ${CMAKE_BINARY_DIR}/../lib/fmt/. ./fmt && cd fmt && cmake -DCMAKE_POSITION_INDEPENDENT_CODE=TRUE -DFMT_TEST=0 -DFMT_DOC=0 . && make
    WORKING_DIRECTORY ${OUTPUT_DEPS_DIR}
)



# ******************************************************
# Compiler options
# ******************************************************

# Sets the compiler
if(ISLINUX)
    SET (CMAKE_C_COMPILER   ${FOUND_CLANG})
    SET (CMAKE_CXX_COMPILER ${FOUND_CLANGPP})
endif()

# Sets compiler options
add_compile_options(-std=c++17 -fPIC -fms-extensions)
add_compile_options(-DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE)
add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined)
if (ISMACOS)
    add_compile_options(-stdlib=libc++ -DMACOS -Wno-pragma-pack)
elseif(ISLINUX)
    add_compile_options(-stdlib=libstdc++ -DLINUX -Wno-pragmas)
endif()
if (BIT64)
    add_compile_options(-DBIT64 -DHOST_64BIT)
endif()
if (ISAMD64)
    add_compile_options(-DAMD64)
elseif (ISX86)
    add_compile_options(-DBX86 -DHOST_X86)
elseif (ISARM64)
    add_compile_options(-DARM64)
    # See https://github.com/dotnet/runtime/issues/78286
    add_compile_definitions(HOST_ARM64)
elseif (ISARM)
    add_compile_options(-DARM)
endif()

add_compile_definitions(OTEL_AUTO_VERSION_MAJOR=${OTEL_AUTO_VERSION_MAJOR})
add_compile_definitions(OTEL_AUTO_VERSION_MINOR=${OTEL_AUTO_VERSION_MINOR})
add_compile_definitions(OTEL_AUTO_VERSION_PATCH=${OTEL_AUTO_VERSION_PATCH})

# ******************************************************
# Suppress Warning on MacOS
# ******************************************************

# Only necessary with cmake 3.19.x on macos
# See https://stackoverflow.com/questions/4929255/building-static-libraries-on-mac-using-cmake-and-gcc#answer-4953904

if (ISMACOS)
    SET(CMAKE_C_ARCHIVE_CREATE   "<CMAKE_AR> Scr <TARGET> <LINK_FLAGS> <OBJECTS>")
    SET(CMAKE_CXX_ARCHIVE_CREATE "<CMAKE_AR> Scr <TARGET> <LINK_FLAGS> <OBJECTS>")
    SET(CMAKE_C_ARCHIVE_FINISH   "<CMAKE_RANLIB> -no_warning_for_no_symbols -c <TARGET>")
    SET(CMAKE_CXX_ARCHIVE_FINISH "<CMAKE_RANLIB> -no_warning_for_no_symbols -c <TARGET>")
endif()

# ******************************************************
# Define static target
# ******************************************************
add_library("OpenTelemetry.AutoInstrumentation.Native.static" STATIC
        class_factory.cpp
        clr_helpers.cpp
        continuous_profiler_clr_helpers.cpp
        continuous_profiler.cpp
        cor_profiler_base.cpp
        cor_profiler.cpp
        il_rewriter_wrapper.cpp
        il_rewriter.cpp
        integration.cpp
        member_resolver.cpp
        metadata_builder.cpp
        miniutf.cpp
        string_utils.cpp
        util.cpp
        calltarget_tokens.cpp
        rejit_handler.cpp
        rejit_preprocessor.cpp
        rejit_work_offloader.cpp
        environment_variables_util.cpp
        method_rewriter.cpp
        tracer_tokens.cpp
        lib/coreclr/src/pal/prebuilt/idl/corprof_i.cpp
        # Source dependencies retrievied via additional commands using git
        ${OUTPUT_DEPS_DIR}/fmt/libfmt.a
        ${OUTPUT_DEPS_DIR}/re2/obj/libre2.a
)

set_target_properties("OpenTelemetry.AutoInstrumentation.Native.static" PROPERTIES PREFIX "")

# Define directories includes
target_include_directories("OpenTelemetry.AutoInstrumentation.Native.static"
        PUBLIC lib/coreclr/src/pal/inc/rt
        PUBLIC lib/coreclr/src/pal/prebuilt/inc
        PUBLIC lib/coreclr/src/pal/inc
        PUBLIC lib/coreclr/src/inc
        PUBLIC lib/spdlog/include
        PUBLIC ${OUTPUT_DEPS_DIR}/fmt/include
        PUBLIC ${OUTPUT_DEPS_DIR}/re2
)

# Define linker libraries
if (ISMACOS)
    target_link_libraries("OpenTelemetry.AutoInstrumentation.Native.static"
        ${OUTPUT_DEPS_DIR}/re2/obj/libre2.a
        ${OUTPUT_DEPS_DIR}/fmt/libfmt.a
        ${CMAKE_DL_LIBS}
    )
elseif(ISLINUX)
    target_link_libraries("OpenTelemetry.AutoInstrumentation.Native.static"
        ${OUTPUT_DEPS_DIR}/re2/obj/libre2.a
        ${OUTPUT_DEPS_DIR}/fmt/libfmt.a
        ${CMAKE_DL_LIBS}
        -static-libgcc
        -static-libstdc++
    )
endif()

# ******************************************************
# Define shared target
# ******************************************************
SET(TARGET_NAME "OpenTelemetry.AutoInstrumentation.Native")

if (ISMACOS)
    add_library(${TARGET_NAME} SHARED
        dllmain.cpp
        interop.cpp
    )
else()
    add_library(${TARGET_NAME} SHARED
        dllmain.cpp
        interop.cpp
    )
endif()

set_target_properties(${TARGET_NAME} PROPERTIES PREFIX "")

# Define linker libraries
target_link_libraries(${TARGET_NAME} "OpenTelemetry.AutoInstrumentation.Native.static")
