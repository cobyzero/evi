#pragma once

#include <memory>

struct GLFWwindow;
class HotReloader;
class JsRuntime;

class EngineApp {
public:
  EngineApp();
  ~EngineApp();

  EngineApp(const EngineApp &) = delete;
  EngineApp &operator=(const EngineApp &) = delete;

  bool initialize();
  int run();

private:
  bool loadScript();
  bool reloadScript();
  void shutdown();

  GLFWwindow *window_ = nullptr;
  const char *appScriptPath_ = "dist/scripts/app.js";

  std::unique_ptr<JsRuntime> runtime_;
  std::unique_ptr<HotReloader> hotReloader_;
};
