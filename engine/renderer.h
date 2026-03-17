#pragma once
#include <cstdint>
#include <string>

void initSkia(int width, int height);
void addDrawRect(float x, float y, float w, float h, uint32_t color);
void addDrawText(const std::string& text, float x, float y, uint32_t color);
void setBackgroundColor(uint32_t color);
void setTextColor(uint32_t color);
void clearCommands();
void flushCommands();
void cleanupSkia();
