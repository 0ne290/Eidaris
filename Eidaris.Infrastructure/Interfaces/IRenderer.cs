using Silk.NET.Windowing;

namespace Eidaris.Infrastructure.Interfaces;

public interface IRenderer : IDisposable
{
    void Initialize(IWindow window);
    void DrawFrame(in PointRenderData point);
}