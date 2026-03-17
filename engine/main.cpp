#include "../quickjs/quickjs.h"
#include "renderer.h"
#include <GLFW/glfw3.h>
#include <chrono>
#include <filesystem>
#include <iostream>
#include <string>

// Runtime functions
void initJS();
void shutdownJS();
void runScript(const std::string &path);

// Bindings
void registerBindings(JSContext *ctx);
extern JSContext *ctx;

namespace fs = std::filesystem;

static fs::file_time_type latestDistJsWriteTime() {
  fs::file_time_type latest = fs::file_time_type::min();

  try {
    if (!fs::exists("dist")) {
      return latest;
    }

    for (const auto &entry :
         fs::recursive_directory_iterator("dist", fs::directory_options::skip_permission_denied)) {
      if (!entry.is_regular_file()) {
        continue;
      }

      if (entry.path().extension() == ".js") {
        const auto writeTime = entry.last_write_time();
        if (writeTime > latest) {
          latest = writeTime;
        }
      }
    }
  } catch (const std::exception &e) {
    std::cerr << "Hot reload scan error: " << e.what() << std::endl;
  }

  return latest;
}

int main() {
  if (!glfwInit()) {
    std::cerr << "GLFW init failed\n";
    return -1;
  }

  glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
  glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
  glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
  glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);

  GLFWwindow *window = glfwCreateWindow(800, 600, "TS Skia Engine", NULL, NULL);

  if (!window) {
    glfwTerminate();
    return -1;
  }

  glfwMakeContextCurrent(window);

  int width, height;
  glfwGetFramebufferSize(window, &width, &height);

  // Initialize Skia
  initSkia(width, height);

  // Initialize JS
  initJS();
  registerBindings(ctx);

  // Run the app script
  const std::string appScriptPath = "dist/scripts/app.js";
  std::cout << "Loading script: " << appScriptPath << std::endl;
  runScript(appScriptPath);
  std::cout << "Script loaded." << std::endl;

  auto lastReloadCheck = std::chrono::steady_clock::now();
  auto lastDistWriteTime = latestDistJsWriteTime();
  auto pendingWriteTime = fs::file_time_type::min();
  auto pendingSince = std::chrono::steady_clock::now();
  constexpr auto kReloadPollInterval = std::chrono::milliseconds(250);
  constexpr auto kReloadDebounce = std::chrono::milliseconds(500);

  while (!glfwWindowShouldClose(window)) {
    const auto now = std::chrono::steady_clock::now();
    if (now - lastReloadCheck >= kReloadPollInterval) {
      const auto currentWriteTime = latestDistJsWriteTime();
      if (currentWriteTime != fs::file_time_type::min() &&
          currentWriteTime > lastDistWriteTime) {
        // Debounce burst writes from tsc --watch before reloading.
        if (currentWriteTime != pendingWriteTime) {
          pendingWriteTime = currentWriteTime;
          pendingSince = now;
        } else if (now - pendingSince >= kReloadDebounce) {
          std::cout << "Changes detected. Hot reloading..." << std::endl;

          lastDistWriteTime = pendingWriteTime;
          pendingWriteTime = fs::file_time_type::min();

          clearCommands();
          shutdownJS();

          initJS();
          registerBindings(ctx);
          runScript(appScriptPath);

          std::cout << "Hot reload complete." << std::endl;
        }
      }

      lastReloadCheck = now;
    }

    // Render frame
    flushCommands();

    glfwSwapBuffers(window);
    glfwPollEvents();
  }

  shutdownJS();
  cleanupSkia();
  glfwTerminate();
  return 0;
}