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

  int color = 0xFFFF0000;
  if (argc >= 5) {
    JS_ToInt32(ctx, &color, argv[4]);
  }

  addDrawRect((float)x, (float)y, (float)w, (float)h,
              static_cast<uint32_t>(color));

  return JS_UNDEFINED;
}

static JSValue js_drawText(JSContext *ctx, JSValueConst this_val, int argc,
                           JSValueConst *argv) {
  const char *text = JS_ToCString(ctx, argv[0]);
  int x, y;
  int color = 0xFF000000;
  JS_ToInt32(ctx, &x, argv[1]);
  JS_ToInt32(ctx, &y, argv[2]);
  if (argc >= 4) {
    JS_ToInt32(ctx, &color, argv[3]);
  }

  addDrawText(text, x, y, static_cast<uint32_t>(color));

  JS_FreeCString(ctx, text);
  return JS_UNDEFINED;
}

static JSValue js_setBackgroundColor(JSContext *ctx, JSValueConst this_val,
                                     int argc, JSValueConst *argv) {
  int color = 0xFFFFFFFF;
  if (argc >= 1) {
    JS_ToInt32(ctx, &color, argv[0]);
  }
  setBackgroundColor(static_cast<uint32_t>(color));
  return JS_UNDEFINED;
}

static JSValue js_setTextColor(JSContext *ctx, JSValueConst this_val, int argc,
                               JSValueConst *argv) {
  int color = 0xFF000000;
  if (argc >= 1) {
    JS_ToInt32(ctx, &color, argv[0]);
  }
  setTextColor(static_cast<uint32_t>(color));
  return JS_UNDEFINED;
}

void registerBindings(JSContext *ctx) {

  JSValue global = JS_GetGlobalObject(ctx);

  JS_SetPropertyStr(ctx, global, "drawRect",
                    JS_NewCFunction(ctx, js_drawRect, "drawRect", 5));

  JS_SetPropertyStr(ctx, global, "drawText",
                    JS_NewCFunction(ctx, js_drawText, "drawText", 4));

  JS_SetPropertyStr(ctx, global, "setBackgroundColor",
                    JS_NewCFunction(ctx, js_setBackgroundColor,
                                    "setBackgroundColor", 1));

  JS_SetPropertyStr(ctx, global, "setTextColor",
                    JS_NewCFunction(ctx, js_setTextColor, "setTextColor", 1));

  JS_FreeValue(ctx, global);
}