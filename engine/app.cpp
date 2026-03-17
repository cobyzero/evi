#include "app.h"

#include "bindings.h"
#include "hot_reload.h"
#include "renderer.h"
#include "runtime.h"

#include <GLFW/glfw3.h>

#include <iostream>
#include <memory>

EngineApp::EngineApp()
    : runtime_(std::make_unique<JsRuntime>()),
      hotReloader_(std::make_unique<HotReloader>()) {}

EngineApp::~EngineApp() { shutdown(); }

bool EngineApp::initialize() {
  if (!glfwInit()) {
    std::cerr << "GLFW init failed" << std::endl;
    return false;
  }

  glfwWindowHint(GLFW_CONTEXT_VERSION_MAJOR, 3);
  glfwWindowHint(GLFW_CONTEXT_VERSION_MINOR, 3);
  glfwWindowHint(GLFW_OPENGL_PROFILE, GLFW_OPENGL_CORE_PROFILE);
  glfwWindowHint(GLFW_OPENGL_FORWARD_COMPAT, GL_TRUE);

  window_ = glfwCreateWindow(800, 600, "TS Skia Engine", nullptr, nullptr);
  if (!window_) {
    std::cerr << "Failed to create GLFW window" << std::endl;
    glfwTerminate();
    return false;
  }

  glfwMakeContextCurrent(window_);

  int width = 0;
  int height = 0;
  glfwGetFramebufferSize(window_, &width, &height);
  initSkia(width, height);

  if (!runtime_->init()) {
    return false;
  }

  registerBindings(runtime_->context());

  std::cout << "Loading script: " << appScriptPath_ << std::endl;
  loadScript();
  std::cout << "Script loaded." << std::endl;

  return true;
}

int EngineApp::run() {
  if (!window_) {
    return -1;
  }

  while (!glfwWindowShouldClose(window_)) {
    if (hotReloader_->shouldReload()) {
      std::cout << "Changes detected. Hot reloading..." << std::endl;
      if (reloadScript()) {
        std::cout << "Hot reload complete." << std::endl;
      }
    }

    flushCommands();
    glfwSwapBuffers(window_);
    glfwPollEvents();
  }

  return 0;
}

bool EngineApp::loadScript() { return runtime_->runScript(appScriptPath_); }

bool EngineApp::reloadScript() {
  clearCommands();

  if (!runtime_->restart()) {
    std::cerr << "Hot reload failed: unable to restart runtime" << std::endl;
    return false;
  }

  registerBindings(runtime_->context());
  return loadScript();
}

void EngineApp::shutdown() {
  if (runtime_) {
    runtime_->shutdown();
  }

  cleanupSkia();

  if (window_) {
    glfwDestroyWindow(window_);
    window_ = nullptr;
  }

  glfwTerminate();
}
