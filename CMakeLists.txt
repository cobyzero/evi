cmake_minimum_required(VERSION 3.16)
project(evi)

set(CMAKE_CXX_STANDARD 20)

# Explicitly add project root and quickjs to all targets
include_directories(${CMAKE_SOURCE_DIR})

find_package(glfw3 REQUIRED)
find_package(OpenGL REQUIRED)

# -------- QuickJS --------
add_library(quickjs STATIC
    quickjs/quickjs.c
    quickjs/libregexp.c
    quickjs/libunicode.c
    quickjs/cutils.c
    quickjs/dtoa.c
    quickjs/quickjs-libc.c
)

target_compile_definitions(quickjs PRIVATE
    CONFIG_VERSION="2025-09-13"
)

# -------- Engine --------
add_executable(engine
    engine/main.cpp
    engine/runtime.cpp
    engine/bindings.cpp
    engine/renderer.cpp
)

# -------- SKIA --------
set(SKIA_DIR ${CMAKE_SOURCE_DIR}/skia)

target_include_directories(engine PRIVATE
    ${SKIA_DIR}
    ${SKIA_DIR}/include
)

# Link Skia
target_link_libraries(engine
    ${SKIA_DIR}/out/Release/libskia.a
)

# -------- macOS frameworks --------
target_link_libraries(engine
    "-framework Cocoa"
    "-framework Metal"
    "-framework QuartzCore"
    "-framework IOKit"
    "-framework CoreFoundation"
    "-framework CoreGraphics"
    "-framework CoreText"
)

# Link dependencies
target_link_libraries(engine
    glfw
    OpenGL::GL
    quickjs
)