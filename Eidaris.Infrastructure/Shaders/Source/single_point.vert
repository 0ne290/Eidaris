#version 460

layout(push_constant) uniform VertexPushConstants {
    vec2 position;
    float pointSize;
} vpc;

void main() {
    gl_Position = vec4(vpc.position, 0.0, 1.0);
    gl_PointSize = vpc.pointSize;
}