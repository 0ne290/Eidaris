using Eidaris.Infrastructure.Interfaces;

namespace Eidaris.Infrastructure;

public sealed class RendererFactory : IRendererFactory
{
    public IRenderer Create() => new Renderer();
}