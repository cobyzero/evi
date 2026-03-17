#include "include/core/SkCanvas.h"
#include "include/core/SkPaint.h"
#include "include/core/SkSurface.h"
#include "include/gpu/ganesh/GrDirectContext.h"

class SkiaRenderer {
public:
  sk_sp<SkSurface> surface;
  sk_sp<GrDirectContext> context;

  void render() {
    SkCanvas *canvas = surface->getCanvas();

    canvas->clear(SK_ColorBLACK);

    SkPaint paint;
    paint.setColor(SK_ColorRED);

    canvas->drawRect(SkRect::MakeXYWH(100, 100, 200, 200), paint);
  }
};