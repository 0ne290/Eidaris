#version 460

layout(push_constant) uniform FragmentPushConstants {
    layout(offset = 16) vec4 color;  // ← Добавить offset
} fpc;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = fpc.color;
}