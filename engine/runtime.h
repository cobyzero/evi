#pragma once

#include <string>

struct JSContext;
struct JSRuntime;

class JsRuntime {
public:
  JsRuntime() = default;
  ~JsRuntime();

  JsRuntime(const JsRuntime &) = delete;
  JsRuntime &operator=(const JsRuntime &) = delete;

  bool init();
  void shutdown();
  bool restart();
  bool runScript(const std::string &path) const;

  JSContext *context() const { return ctx_; }

private:
  JSRuntime *rt_ = nullptr;
  JSContext *ctx_ = nullptr;
};
