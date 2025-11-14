using Eidaris.Infrastructure;
using Eidaris.Infrastructure.Interfaces;
using Eidaris.Presentation.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Eidaris.Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static void Compose(this IServiceCollection services)
    {
        services.AddSingleton<IWindow>(_ => Window.Create(WindowOptions.Default with
        {
            Title = "Eidaris",
            Size = new Vector2D<int>(2560, 1440)
        }));
        services.AddSingleton<IRendererFactory, RendererFactory>();
        services.AddSingleton<IEidarisApplication, EidarisApplication>();
    }
}