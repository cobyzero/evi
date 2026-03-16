#include "quickjs/quickjs.h"
#include <iostream>
#include "renderer.h"

static JSValue js_drawRect(JSContext *ctx, JSValueConst this_val, int argc,
                           JSValueConst *argv) {

  int x, y, w, h;

  JS_ToInt32(ctx, &x, argv[0]);
  JS_ToInt32(ctx, &y, argv[1]);
  JS_ToInt32(ctx, &w, argv[2]);
  JS_ToInt32(ctx, &h, argv[3]);

  addDrawRect(x, y, w, h);

  return JS_UNDEFINED;
}

static JSValue js_drawText(JSContext *ctx, JSValueConst this_val, int argc,
                           JSValueConst *argv) {
  const char *text = JS_ToCString(ctx, argv[0]);
  int x, y;
  JS_ToInt32(ctx, &x, argv[1]);
  JS_ToInt32(ctx, &y, argv[2]);

  addDrawText(text, x, y);

  JS_FreeCString(ctx, text);
  return JS_UNDEFINED;
}

void registerBindings(JSContext *ctx) {

  JSValue global = JS_GetGlobalObject(ctx);

  JS_SetPropertyStr(ctx, global, "drawRect",
                    JS_NewCFunction(ctx, js_drawRect, "drawRect", 4));

  JS_SetPropertyStr(ctx, global, "drawText",
                    JS_NewCFunction(ctx, js_drawText, "drawText", 3));

  JS_FreeValue(ctx, global);
}