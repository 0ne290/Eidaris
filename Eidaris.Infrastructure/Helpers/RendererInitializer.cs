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

        var physicalDeviceSelector = new PhysicalDeviceSelector(api, instance, khrSurface, surface);
        var (physicalDevice, queueIndices) = physicalDeviceSelector.SelectBestDevice();

        var logicalDeviceCreator = new LogicalDeviceCreator(api, physicalDevice, queueIndices);
        var (logicalDevice, graphicsQueue, presentQueue) = logicalDeviceCreator.Create();

        if (!api.TryGetDeviceExtension(instance, logicalDevice, out KhrSwapchain khrSwapchain))
            throw new Exception("VK_KHR_swapchain extension not available.");

        var swapchainCreator = new SwapchainCreator(
            physicalDevice,
            logicalDevice,
            khrSurface,
            surface,
            khrSwapchain,
            queueIndices,
            (uint)_window.Size.X,
            (uint)_window.Size.Y);
        var (swapchain, swapchainFormat, swapchainExtent) = swapchainCreator.Create();

        var imageViewsCreator = new SwapchainImageViewsCreator(
            api,
            logicalDevice,
            khrSwapchain,
            swapchain,
            swapchainFormat.Format);
        var (swapchainImages, swapchainImageViews) = imageViewsCreator.Create();

        var commandPoolCreator = new CommandPoolCreator(api, logicalDevice, queueIndices.GraphicsFamily);
        var commandPool = commandPoolCreator.CreateCommandPool();
        var commandBuffers = commandPoolCreator.AllocateCommandBuffers(commandPool, Constants.MaxFramesInFlight);

        var syncObjectsCreator = new SyncObjectsCreator(api, logicalDevice, Constants.MaxFramesInFlight);
        var (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences) = syncObjectsCreator.Create();

        var shaderModuleCreator = new ShaderModuleCreator(api, logicalDevice);
        var vertexShaderModule = LoadShaderModule("Shaders/Compiled/single_point.vert.spv", shaderModuleCreator);
        var fragmentShaderModule = LoadShaderModule("Shaders/Compiled/single_point.frag.spv", shaderModuleCreator);

        var pipelineCreator = new PipelineCreator(api, logicalDevice, swapchainExtent, swapchainFormat.Format);
        var (pipeline, pipelineLayout) = pipelineCreator.Create(vertexShaderModule, fragmentShaderModule);


        return new RendererInitializationContext
        {
            Api = api,
            Instance = instance,
            KhrSurface = khrSurface,
            Surface = surface,
            PhysicalDevice = physicalDevice,
            QueueIndices = queueIndices,
            LogicalDevice = logicalDevice,
            GraphicsQueue = graphicsQueue,
            PresentQueue = presentQueue,
            KhrSwapchain = khrSwapchain,
            Swapchain = swapchain,
            SwapchainFormat = swapchainFormat,
            SwapchainExtent = swapchainExtent,
            SwapchainImages = swapchainImages,
            SwapchainImageViews = swapchainImageViews,
            CommandPool = commandPool,
            CommandBuffers = commandBuffers,
            ImageAvailableSemaphores = imageAvailableSemaphores,
            RenderFinishedSemaphores = renderFinishedSemaphores,
            InFlightFences = inFlightFences,
            VertexShaderModule = vertexShaderModule,
            FragmentShaderModule = fragmentShaderModule,
            Pipeline = pipeline,
            PipelineLayout = pipelineLayout
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

    private static ShaderModule LoadShaderModule(string path, ShaderModuleCreator creator)
    {
        var code = File.ReadAllBytes(path);
        return creator.CreateShaderModule(code);
    }

    private readonly IWindow _window;
}