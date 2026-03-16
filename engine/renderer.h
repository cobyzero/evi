#pragma once
#include <string>

void addDrawRect(int x, int y, int w, int h);
void addDrawText(const std::string& text, int x, int y);
void flushCommands();
