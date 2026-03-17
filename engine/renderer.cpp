#include "renderer.h"
#include <vector>
#include <iostream>
#include <string>

#define GL_SILENCE_DEPRECATION
#define GLFW_INCLUDE_GLCOREARB
#include <GLFW/glfw3.h>

#include "core/SkCanvas.h"
#include "core/SkColor.h"
#include "core/SkFont.h"
#include "core/SkFontMgr.h"
#include "core/SkImageInfo.h"
#include "core/SkPaint.h"
#include "core/SkPixmap.h"
#include "core/SkSurface.h"

#ifdef __APPLE__
#include <CoreText/CoreText.h>
#include "ports/SkFontMgr_mac_ct.h"
#endif

// Command structure
enum class CommandType {
    DrawRect,
    DrawText
};

struct RenderCommand {
    CommandType type;
    float x, y, w, h;
    uint32_t color;
    std::string text;
};

static std::vector<RenderCommand> commandQueue;
static sk_sp<SkSurface> sSurface = nullptr;
static uint32_t sBackgroundColor = SK_ColorWHITE;
static uint32_t sDefaultTextColor = SK_ColorBLACK;
static int sSurfaceWidth = 0;
static int sSurfaceHeight = 0;
static sk_sp<SkTypeface> sTextTypeface = nullptr;

static unsigned int sProgram = 0;
static unsigned int sVao = 0;
static unsigned int sVbo = 0;
static unsigned int sTexture = 0;

static unsigned int compileShader(unsigned int type, const char* source) {
    unsigned int shader = glCreateShader(type);
    glShaderSource(shader, 1, &source, nullptr);
    glCompileShader(shader);

    int success = 0;
    glGetShaderiv(shader, GL_COMPILE_STATUS, &success);
    if (!success) {
        char log[1024] = {0};
        glGetShaderInfoLog(shader, sizeof(log), nullptr, log);
        std::cerr << "Shader compile error: " << log << std::endl;
    }

    return shader;
}

static bool initCompositor(int width, int height) {
    const char* vertexSrc =
        "#version 330 core\n"
        "layout(location = 0) in vec2 aPos;\n"
        "layout(location = 1) in vec2 aUV;\n"
        "out vec2 vUV;\n"
        "void main() {\n"
        "  vUV = aUV;\n"
        "  gl_Position = vec4(aPos, 0.0, 1.0);\n"
        "}\n";

    const char* fragmentSrc =
        "#version 330 core\n"
        "in vec2 vUV;\n"
        "out vec4 FragColor;\n"
        "uniform sampler2D uTex;\n"
        "void main() {\n"
        "  FragColor = texture(uTex, vUV);\n"
        "}\n";

    unsigned int vs = compileShader(GL_VERTEX_SHADER, vertexSrc);
    unsigned int fs = compileShader(GL_FRAGMENT_SHADER, fragmentSrc);

    sProgram = glCreateProgram();
    glAttachShader(sProgram, vs);
    glAttachShader(sProgram, fs);
    glLinkProgram(sProgram);

    int linked = 0;
    glGetProgramiv(sProgram, GL_LINK_STATUS, &linked);
    if (!linked) {
        char log[1024] = {0};
        glGetProgramInfoLog(sProgram, sizeof(log), nullptr, log);
        std::cerr << "Shader link error: " << log << std::endl;
    }

    glDeleteShader(vs);
    glDeleteShader(fs);

    const float quad[] = {
        -1.0f, -1.0f, 0.0f, 1.0f,
         1.0f, -1.0f, 1.0f, 1.0f,
        -1.0f,  1.0f, 0.0f, 0.0f,
         1.0f,  1.0f, 1.0f, 0.0f,
    };

    glGenVertexArrays(1, &sVao);
    glGenBuffers(1, &sVbo);
    glBindVertexArray(sVao);
    glBindBuffer(GL_ARRAY_BUFFER, sVbo);
    glBufferData(GL_ARRAY_BUFFER, sizeof(quad), quad, GL_STATIC_DRAW);

    glEnableVertexAttribArray(0);
    glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(float),
                          reinterpret_cast<void*>(0));

    glEnableVertexAttribArray(1);
    glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 4 * sizeof(float),
                          reinterpret_cast<void*>(2 * sizeof(float)));

    glBindBuffer(GL_ARRAY_BUFFER, 0);
    glBindVertexArray(0);

    glGenTextures(1, &sTexture);
    glBindTexture(GL_TEXTURE_2D, sTexture);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
    glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, width, height, 0, GL_BGRA,
                 GL_UNSIGNED_BYTE, nullptr);
    glBindTexture(GL_TEXTURE_2D, 0);

    sSurfaceWidth = width;
    sSurfaceHeight = height;

    return linked == GL_TRUE;
}

void initSkia(int width, int height) {
    auto info = SkImageInfo::MakeN32Premul(width, height);
    sSurface = SkSurfaces::Raster(info);

#ifdef __APPLE__
    auto fontMgr = SkFontMgr_New_CoreText(nullptr);
    if (fontMgr) {
        sTextTypeface = fontMgr->matchFamilyStyle("Helvetica", SkFontStyle());
        if (!sTextTypeface) {
            sTextTypeface = fontMgr->legacyMakeTypeface(nullptr, SkFontStyle());
        }
    }
#endif

    if (!sSurface) {
        std::cerr << "Failed to create Skia surface" << std::endl;
    }

    if (!initCompositor(width, height)) {
        std::cerr << "Failed to initialize GL compositor" << std::endl;
    }
}

void addDrawRect(float x, float y, float w, float h, uint32_t color) {
    commandQueue.push_back({CommandType::DrawRect, x, y, w, h, color, ""});
}

void addDrawText(const std::string& text, float x, float y, uint32_t color) {
    commandQueue.push_back({CommandType::DrawText, x, y, 0, 0, color, text});
}

void setBackgroundColor(uint32_t color) {
    sBackgroundColor = color;
}

void setTextColor(uint32_t color) {
    sDefaultTextColor = color;
}

void flushCommands() {
    if (!sSurface) return;

    SkCanvas* canvas = sSurface->getCanvas();
    canvas->clear(sBackgroundColor);

    SkPaint paint;
    SkFont font;
    if (sTextTypeface) {
        font.setTypeface(sTextTypeface);
    }
    font.setSize(20.0f);
    paint.setAntiAlias(true);

    for (const auto& cmd : commandQueue) {
        if (cmd.type == CommandType::DrawRect) {
            paint.setColor(cmd.color);
            canvas->drawRect(SkRect::MakeXYWH(cmd.x, cmd.y, cmd.w, cmd.h), paint);
        } else if (cmd.type == CommandType::DrawText) {
            const uint32_t color = cmd.color == 0 ? sDefaultTextColor : cmd.color;
            paint.setColor(color);
            canvas->drawString(cmd.text.c_str(), cmd.x, cmd.y + font.getSize(), font, paint);
        }
    }

    SkPixmap pixmap;
    if (sSurface->peekPixels(&pixmap)) {
        glViewport(0, 0, sSurfaceWidth, sSurfaceHeight);
        glClear(GL_COLOR_BUFFER_BIT);

        glActiveTexture(GL_TEXTURE0);
        glBindTexture(GL_TEXTURE_2D, sTexture);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 4);
        glPixelStorei(GL_UNPACK_ROW_LENGTH,
                      static_cast<int>(pixmap.rowBytes() / 4));
        glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, pixmap.width(), pixmap.height(),
                        GL_BGRA, GL_UNSIGNED_BYTE, pixmap.addr());
        glPixelStorei(GL_UNPACK_ROW_LENGTH, 0);

        glUseProgram(sProgram);
        glUniform1i(glGetUniformLocation(sProgram, "uTex"), 0);
        glBindVertexArray(sVao);
        glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

        glBindVertexArray(0);
        glBindTexture(GL_TEXTURE_2D, 0);
        glUseProgram(0);
    }
}

void cleanupSkia() {
    if (sTexture) {
        glDeleteTextures(1, &sTexture);
        sTexture = 0;
    }
    if (sVbo) {
        glDeleteBuffers(1, &sVbo);
        sVbo = 0;
    }
    if (sVao) {
        glDeleteVertexArrays(1, &sVao);
        sVao = 0;
    }
    if (sProgram) {
        glDeleteProgram(sProgram);
        sProgram = 0;
    }

    sSurface = nullptr;
    sTextTypeface = nullptr;
}
