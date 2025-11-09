using Eidaris.Infrastructure.Interfaces;
using Eidaris.Presentation.Interfaces;
using Silk.NET.Windowing;

namespace Eidaris.Presentation;

internal sealed class EidarisApplication : IEidarisApplication
{
    public EidarisApplication(IWindow window, IRenderer renderer)
    {
        _window = window;
        _renderer = renderer;
    }

    public void Run()
    {
        _window.Load += OnLoad;
        _window.Render += dt => _renderer.DrawFrame(dt);
        _window.Closing += _renderer.Dispose;

        _window.Run();
    }

    private void OnLoad() => _renderer.Initialize(_window);

    private readonly IWindow _window;
    
    private readonly IRenderer _renderer;
}