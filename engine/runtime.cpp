#include "runtime.h"

#include "quickjs/quickjs-libc.h"
#include "quickjs/quickjs.h"

#include <fstream>
#include <iostream>
#include <iterator>
#include <string>

JsRuntime::~JsRuntime() { shutdown(); }

bool JsRuntime::init() {
  if (rt_ || ctx_) {
    return true;
  }

  rt_ = JS_NewRuntime();
  if (!rt_) {
    std::cerr << "Failed to create JS runtime" << std::endl;
    return false;
  }

  js_std_init_handlers(rt_);

  ctx_ = JS_NewContext(rt_);
  if (!ctx_) {
    std::cerr << "Failed to create JS context" << std::endl;
    shutdown();
    return false;
  }

  js_std_add_helpers(ctx_, 0, nullptr);
  JS_SetModuleLoaderFunc2(rt_, nullptr, js_module_loader, nullptr, nullptr);
  return true;
}

void JsRuntime::shutdown() {
  if (ctx_) {
    JS_FreeContext(ctx_);
    ctx_ = nullptr;
  }
  if (rt_) {
    js_std_free_handlers(rt_);
    JS_FreeRuntime(rt_);
    rt_ = nullptr;
  }
}

bool JsRuntime::restart() {
  shutdown();
  return init();
}

bool JsRuntime::runScript(const std::string &path) const {
  if (!ctx_) {
    std::cerr << "JS runtime is not initialized" << std::endl;
    return false;
  }

  std::ifstream file(path);
  if (!file.is_open()) {
    std::cerr << "Failed to open script: " << path << std::endl;
    return false;
  }

  std::string code((std::istreambuf_iterator<char>(file)),
                   std::istreambuf_iterator<char>());

  JSValue result =
      JS_Eval(ctx_, code.c_str(), code.size(), path.c_str(), JS_EVAL_TYPE_MODULE);

  bool ok = true;
  if (JS_IsException(result)) {
    JSValue exception = JS_GetException(ctx_);
    const char *str = JS_ToCString(ctx_, exception);
    std::cerr << "JS Exception: " << (str ? str : "Unknown error") << std::endl;
    JS_FreeCString(ctx_, str);
    JS_FreeValue(ctx_, exception);
    ok = false;
  }

  js_std_loop(ctx_);
  JS_FreeValue(ctx_, result);
  return ok;
}