#version 460

layout(push_constant) uniform FragmentPushConstants {
    vec4 color;
} fpc;

layout(location = 0) out vec4 outColor;

void main() {
    outColor = fpc.color;
}