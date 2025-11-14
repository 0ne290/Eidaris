using Eidaris.Infrastructure;
using Eidaris.Infrastructure.Interfaces;
using Eidaris.Presentation.Interfaces;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Eidaris.Presentation;

internal sealed class EidarisApplication : IEidarisApplication
{
    private readonly IWindow _window;
    private readonly IRendererFactory _rendererFactory;
    private IRenderer? _renderer;

    public EidarisApplication(IWindow window, IRendererFactory rendererFactory)
    {
        _window = window;
        _rendererFactory = rendererFactory;
    }

    public void Run()
    {
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Closing += OnClosing;

        _window.Run();
    }

    private void OnLoad()
    {
        _renderer = _rendererFactory.Create();
        _renderer.Initialize(_window);
    }

    private void OnRender(double deltaTime)
    {
        var point = new PointRenderData(
            position: new Vector2D<float>(0.0f, 0.0f),
            pointSize: 20.0f,
            color: new Vector4D<float>(1.0f, 1.0f, 1.0f, 1.0f)
        );
        
        _renderer!.DrawFrame(in point);
    }

    private void OnClosing()
    {
        _renderer?.Dispose();
        _renderer = null;
    }
}