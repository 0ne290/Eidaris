using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace Eidaris.Infrastructure;

public readonly struct PointRenderData
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct VertexPushConstants
    {
        public VertexPushConstants(Vector2D<float> position, float pointSize)
        {
            Position = position;
            PointSize = pointSize;
        }

        public Vector2D<float> Position { get; }
        public float PointSize { get; }
        
        public readonly float _padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct FragmentPushConstants
    {
        public FragmentPushConstants(Vector4D<float> color)
        {
            Color = color;
        }

        public Vector4D<float> Color { get; }
    }
    
    public PointRenderData(Vector2D<float> position, float pointSize, Vector4D<float> color)
    {
        Vertex = new VertexPushConstants(position, pointSize);
        Fragment = new FragmentPushConstants(color);
    }
    
    internal VertexPushConstants Vertex { get; }
    
    internal FragmentPushConstants Fragment { get; }
}