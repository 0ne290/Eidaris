using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class RendererInitializer
{
    public RendererInitializer(IWindow window)
    {
        _window = window;
    }
    
    public RendererInitializationContext Initialize()
    {
        if (_window.VkSurface is null)
            throw new InvalidOperationException("Window does not support Vulkan surface.");

        var api = Vk.GetApi();
        var instance = CreateInstance(api);

        if (!api.TryGetInstanceExtension(instance, out KhrSurface khrSurface))
            throw new Exception("VK_KHR_surface extension not available.");

        var surface = CreateSurface(instance);

        var deviceSelector = new PhysicalDeviceSelector(api, instance, khrSurface, surface);
        var (physicalDevice, queueIndices) = deviceSelector.SelectBestDevice();

        return new RendererInitializationContext
        {
            Api = api,
            Instance = instance,
            KhrSurface = khrSurface,
            Surface = surface,
            PhysicalDevice = physicalDevice,
            QueueIndices = queueIndices
        };
    }

    private static Instance CreateInstance(Vk api)
    {
        using var appName = new SilkCString("Eidaris");
        using var engineName = new SilkCString("Eidaris Engine");

        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = appName,
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = engineName,
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version13
        };

        var instanceInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        return api.CreateInstance(&instanceInfo, null, out var ret) != Result.Success
            ? throw new Exception("Failed to create Vulkan instance.")
            : ret;
    }

    private SurfaceKHR CreateSurface(Instance instance) =>
        _window.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();

    private readonly IWindow _window;
}