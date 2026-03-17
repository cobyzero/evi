#include "app.h"

int main() {
  EngineApp app;
  if (!app.initialize()) {
    return -1;
  }
  return app.run();
}