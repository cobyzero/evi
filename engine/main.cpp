#include <GLFW/glfw3.h>
#include <iostream>
#include <string>

// Runtime functions
void initJS();
void shutdownJS();
void runScript(const std::string &path);

// Bindings
#include "quickjs/quickjs.h" // Needed for JSContext in registerBindings
#include "renderer.h"
void registerBindings(JSContext *ctx);
extern JSContext *ctx;

int main() {
  if (!glfwInit()) {
    std::cout << "GLFW init failed\n";
    return -1;
  }

  GLFWwindow *window = glfwCreateWindow(800, 600, "TS Skia Engine", NULL, NULL);

  if (!window) {
    glfwTerminate();
    return -1;
  }

  glfwMakeContextCurrent(window);

  // Set clear color (light gray)
  glClearColor(0.9f, 0.9f, 0.9f, 1.0f);

  // Initialize JS
  initJS();
  registerBindings(ctx);

  // Run the app script
  std::cout << "Loading script: dist/scripts/app.js" << std::endl;
  runScript("dist/scripts/app.js");
  std::cout << "Script loaded." << std::endl;

  while (!glfwWindowShouldClose(window)) {
    glClear(GL_COLOR_BUFFER_BIT);

    // Render frame
    flushCommands();

    glfwSwapBuffers(window);
    glfwPollEvents();
  }

  shutdownJS();
  glfwTerminate();
}