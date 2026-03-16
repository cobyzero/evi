#include "quickjs/quickjs.h"
#include <fstream>
#include <string>
#include <iostream>
#include <streambuf>
#include "quickjs/quickjs-libc.h"

JSRuntime *rt;
JSContext *ctx;

void initJS() {
  rt = JS_NewRuntime();
  js_std_init_handlers(rt);
  ctx = JS_NewContext(rt);
  js_std_add_helpers(ctx, 0, NULL);
  JS_SetModuleLoaderFunc2(rt, NULL, js_module_loader, NULL, NULL);
}

void shutdownJS() {
  JS_FreeContext(ctx);
  JS_FreeRuntime(rt);
}

void runScript(const std::string &path) {
  std::ifstream t(path);
  if (!t.is_open()) {
    std::cerr << "Failed to open script: " << path << std::endl;
    return;
  }

  std::string code((std::istreambuf_iterator<char>(t)),
                   std::istreambuf_iterator<char>());

  JSValue result = JS_Eval(ctx, code.c_str(), code.size(), path.c_str(),
                           JS_EVAL_TYPE_MODULE);

  if (JS_IsException(result)) {
    JSValue exception = JS_GetException(ctx);
    const char *str = JS_ToCString(ctx, exception);
    std::cerr << "JS Exception: " << (str ? str : "Unknown error") << std::endl;
    JS_FreeCString(ctx, str);
    JS_FreeValue(ctx, exception);
  }

  js_std_loop(ctx);

  JS_FreeValue(ctx, result);
}