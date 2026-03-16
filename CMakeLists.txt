cmake_minimum_required(VERSION 3.16)

project(ts_skia_engine)

set(CMAKE_CXX_STANDARD 17)

find_package(glfw3 REQUIRED)
find_package(OpenGL REQUIRED)

# QuickJS
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

# Engine
add_executable(engine
    engine/main.cpp
    engine/runtime.cpp
    engine/bindings.cpp
    engine/renderer.cpp
)

# Fix include path to avoid shadowing standard <version> header on macOS
target_include_directories(engine PRIVATE
    ${CMAKE_SOURCE_DIR}
)

target_link_libraries(engine
    glfw
    OpenGL::GL
    quickjs
)