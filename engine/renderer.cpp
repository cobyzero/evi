#include <vector>
#include <string>
#include <iostream>

#ifdef __APPLE__
#include <OpenGL/gl.h>
#else
#include <GL/gl.h>
#endif

enum class CommandType {
    DrawRect,
    DrawText
};

struct RenderCommand {
    CommandType type;
    int x, y, w, h;
    std::string text;
};

std::vector<RenderCommand> commandQueue;

void addDrawRect(int x, int y, int w, int h) {
    commandQueue.push_back({CommandType::DrawRect, x, y, w, h, ""});
}

void addDrawText(const std::string& text, int x, int y) {
    commandQueue.push_back({CommandType::DrawText, x, y, 0, 0, text});
}

void flushCommands() {
    for (const auto& cmd : commandQueue) {
        if (cmd.type == CommandType::DrawRect) {
            std::cout << "Drawing rect at " << cmd.x << "," << cmd.y << std::endl;
            // Basic OpenGL drawing as a fallback
            glColor3f(1.0f, 0.0f, 0.0f); // Red
            float x1 = (cmd.x / 400.0f) - 1.0f;
            float y1 = 1.0f - (cmd.y / 300.0f);
            float x2 = ((cmd.x + cmd.w) / 400.0f) - 1.0f;
            float y2 = 1.0f - ((cmd.y + cmd.h) / 300.0f);

            glBegin(GL_QUADS);
            glVertex2f(x1, y1);
            glVertex2f(x2, y1);
            glVertex2f(x2, y2);
            glVertex2f(x1, y2);
            glEnd();
        } else if (cmd.type == CommandType::DrawText) {
            // Text rendering requires a font engine, skipping for basic GL fallback
        }
    }
    // For now, we don't clear so the drawing stays on screen
    // In a real engine, we would clear and rebuild or use a persistent tree
    // commandQueue.clear(); 
}
