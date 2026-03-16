#!/bin/bash

# Colores para la terminal
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Iniciando compilación de TS Skia Engine...${NC}"

# 1. Compilar TypeScript
echo -e "${GREEN}📦 Compilando TypeScript framework...${NC}"
if ! npx tsc; then
    echo -e "${RED}❌ Error compilando TypeScript${NC}"
    exit 1
fi

# 2. Compilar C++ Engine
echo -e "${GREEN}🏗️  Construyendo Engine nativo (C++)...${NC}"
mkdir -p build
cd build

if ! cmake ..; then
    echo -e "${RED}❌ Error en configuración de CMake${NC}"
    exit 1
fi

if ! make -j$(sysctl -n hw.ncpu 2>/dev/null || echo 4); then
    echo -e "${RED}❌ Error compilando el Engine nativo${NC}"
    exit 1
fi

# 3. Ejecutar
echo -e "${BLUE}🛰️  Ejecutando Engine...${NC}"
cd ..
./build/engine
