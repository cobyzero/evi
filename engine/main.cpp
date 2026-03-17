#include "../quickjs/quickjs.h"
#include "renderer.h"
#include <GLFW/glfw3.h>
#include <iostream>
#include <string>

// Runtime functions
void initJS();
void shutdownJS();
void runScript(const std::string &path);

// Bindings
void registerBindings(JSContext *ctx);
extern JSContext *ctx;

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
  std::cout << "Loading script: dist/scripts/app.js" << std::endl;
  runScript("dist/scripts/app.js");
  std::cout << "Script loaded." << std::endl;

  while (!glfwWindowShouldClose(window)) {
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