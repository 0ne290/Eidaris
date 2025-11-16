using Silk.NET.Core;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class RendererInitializer
{
    public RendererInitializer(IWindow window)
    {
        _window = window;
    }

    public RendererInitializationContext Initialize()
    {
        try
        {
            if (_window.VkSurface is null)
                throw new InvalidOperationException("Window does not support Vulkan surface.");

            _api = Vk.GetApi();
            _instance = CreateInstance();
            
#if DEBUG
            if (_api.TryGetInstanceExtension(_instance, out _debugUtils))
                _debugMessenger = DebugMessengerCreator.Create(_instance, _debugUtils!);
#endif

            if (!_api.TryGetInstanceExtension(_instance, out _khrSurface))
                throw new Exception("VK_KHR_surface extension not available.");

            _surface = CreateSurface();

            var physicalDeviceSelector = new PhysicalDeviceSelector(_api, _instance, _khrSurface!, _surface);
            var (physicalDevice, queueIndices) = physicalDeviceSelector.SelectBestDevice();

            var logicalDeviceCreator = new LogicalDeviceCreator(_api, physicalDevice, queueIndices);
            var (logicalDevice, graphicsQueue, presentQueue) = logicalDeviceCreator.Create();
            _logicalDevice = logicalDevice;

            if (!_api.TryGetDeviceExtension(_instance, _logicalDevice, out _khrSwapchain))
                throw new Exception("VK_KHR_swapchain extension not available.");

            var swapchainCreator = new SwapchainCreator(
                physicalDevice,
                _logicalDevice,
                _khrSurface!,
                _surface,
                _khrSwapchain!,
                queueIndices,
                (uint)_window.Size.X,
                (uint)_window.Size.Y);
            var (swapchain, swapchainFormat, swapchainExtent) = swapchainCreator.Create();
            _swapchain = swapchain;

            var imageViewsCreator = new SwapchainImageViewsCreator(
                _api,
                _logicalDevice,
                _khrSwapchain!,
                _swapchain,
                swapchainFormat.Format);
            var (swapchainImages, swapchainImageViews) = imageViewsCreator.Create();
            _swapchainImageViews = swapchainImageViews;

            var commandPoolCreator = new CommandPoolCreator(_api, _logicalDevice, queueIndices.GraphicsFamily);
            _commandPool = commandPoolCreator.CreateCommandPool();
            var commandBuffers = commandPoolCreator.AllocateCommandBuffers(_commandPool, Constants.MaxFramesInFlight);
            
            var syncObjectsCreator = new SyncObjectsCreator(_api, _logicalDevice, (uint)swapchainImages.Length);
            var (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences) = syncObjectsCreator.Create();
            _imageAvailableSemaphores = imageAvailableSemaphores;
            _renderFinishedSemaphores = renderFinishedSemaphores;
            _inFlightFences = inFlightFences;
            
#if DEBUG
            var debugNamer = new VulkanDebugNamer(_logicalDevice, _debugUtils);
            
            debugNamer.NameSwapchain(_swapchain, "MainSwapchain");
            debugNamer.NameCommandPool(_commandPool, "GraphicsCommandPool");
            
            for (var i = 0; i < commandBuffers.Length; i++)
                debugNamer.NameCommandBuffer(commandBuffers[i], $"CommandBuffer_Frame{i}");
            
            for (var i = 0; i < imageAvailableSemaphores.Length; i++)
                debugNamer.NameSemaphore(imageAvailableSemaphores[i], $"ImageAvailable_Frame{i}");
            
            for (var i = 0; i < renderFinishedSemaphores.Length; i++)
                debugNamer.NameSemaphore(renderFinishedSemaphores[i], $"RenderFinished_Frame{i}");
            
            for (var i = 0; i < inFlightFences.Length; i++)
                debugNamer.NameFence(inFlightFences[i], $"InFlightFence_Frame{i}");
            
            for (var i = 0; i < swapchainImageViews.Length; i++)
                debugNamer.NameImageView(swapchainImageViews[i], $"SwapchainImageView_{i}");
#endif

            var shaderModuleCreator = new ShaderModuleCreator(_api, _logicalDevice);
            _vertexShaderModule = LoadShaderModule("Shaders/Compiled/single_point.vert.spv", shaderModuleCreator);
            _fragmentShaderModule = LoadShaderModule("Shaders/Compiled/single_point.frag.spv", shaderModuleCreator);

            var pipelineCreator = new PipelineCreator(_api, _logicalDevice, swapchainExtent, swapchainFormat.Format);
            var (pipeline, pipelineLayout) = pipelineCreator.Create(_vertexShaderModule, _fragmentShaderModule);
            _pipeline = pipeline;
            _pipelineLayout = pipelineLayout;
            
#if DEBUG
            debugNamer.NamePipeline(_pipeline, "PointRenderingPipeline");
#endif

            var context = new RendererInitializationContext
            {
                Api = _api,
                Instance = _instance,
#if DEBUG
                DebugUtils = _debugUtils,
                DebugMessenger = _debugMessenger,
                DebugNamer = debugNamer,
#endif
                KhrSurface = _khrSurface!,
                Surface = _surface,
                PhysicalDevice = physicalDevice,
                QueueIndices = queueIndices,
                LogicalDevice = _logicalDevice,
                GraphicsQueue = graphicsQueue,
                PresentQueue = presentQueue,
                KhrSwapchain = _khrSwapchain!,
                Swapchain = _swapchain,
                SwapchainFormat = swapchainFormat,
                SwapchainExtent = swapchainExtent,
                SwapchainImages = swapchainImages,
                SwapchainImageViews = _swapchainImageViews,
                CommandPool = _commandPool,
                CommandBuffers = commandBuffers,
                ImageAvailableSemaphores = _imageAvailableSemaphores,
                RenderFinishedSemaphores = _renderFinishedSemaphores,
                InFlightFences = _inFlightFences,
                VertexShaderModule = _vertexShaderModule,
                FragmentShaderModule = _fragmentShaderModule,
                Pipeline = _pipeline,
                PipelineLayout = _pipelineLayout
            };

            // Ownership передан в context, обнуляем поля
            _api = null;
            _debugUtils = null;
            _khrSurface = null;
            _khrSwapchain = null;
            _swapchainImageViews = null;
            _imageAvailableSemaphores = null;
            _renderFinishedSemaphores = null;
            _inFlightFences = null;

            return context;
        }
        catch
        {
            Cleanup();
            throw;
        }
    }

    private Instance CreateInstance()
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

        var windowExtensions = _window.VkSurface!.GetRequiredExtensions(out var windowExtCount);

#if DEBUG
        using var debugExt = new SilkCString("VK_EXT_debug_utils");
        var extCount = windowExtCount + 1;
        var extensions = stackalloc byte*[(int)extCount];
        
        for (var i = 0; i < windowExtCount; i++)
            extensions[i] = windowExtensions[i];
        extensions[windowExtCount] = debugExt;
        
        using var validationLayer = new SilkCString("VK_LAYER_KHRONOS_validation");
        var layerPtr = (byte*)validationLayer;
        
        // Включаем все validation features для максимальных логов
        var validationFeatures = stackalloc ValidationFeatureEnableEXT[]
        {
            ValidationFeatureEnableEXT.BestPracticesExt,
            ValidationFeatureEnableEXT.SynchronizationValidationExt,
            ValidationFeatureEnableEXT.GpuAssistedExt,
            ValidationFeatureEnableEXT.DebugPrintfExt
        };
        
        var validationFeaturesInfo = new ValidationFeaturesEXT
        {
            SType = StructureType.ValidationFeaturesExt,
            EnabledValidationFeatureCount = 4,
            PEnabledValidationFeatures = validationFeatures
        };
        
        var instanceInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PNext = &validationFeaturesInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = extCount,
            PpEnabledExtensionNames = extensions,
            EnabledLayerCount = 1,
            PpEnabledLayerNames = &layerPtr
        };
#else
        var instanceInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = windowExtCount,
            PpEnabledExtensionNames = windowExtensions
        };
#endif

        return _api!.CreateInstance(&instanceInfo, null, out var ret) != Result.Success
            ? throw new Exception("Failed to create Vulkan instance.")
            : ret;
    }

    private SurfaceKHR CreateSurface() =>
        _window.VkSurface!.Create<AllocationCallbacks>(_instance.ToHandle(), null).ToSurface();

    private static ShaderModule LoadShaderModule(string path, ShaderModuleCreator creator)
    {
        var code = File.ReadAllBytes(path);
        return creator.CreateShaderModule(code);
    }

    private void Cleanup()
    {
        if (_api is null)
            return;

        if (_pipeline.Handle != 0)
            _api.DestroyPipeline(_logicalDevice, _pipeline, null);

        if (_pipelineLayout.Handle != 0)
            _api.DestroyPipelineLayout(_logicalDevice, _pipelineLayout, null);

        if (_fragmentShaderModule.Handle != 0)
            _api.DestroyShaderModule(_logicalDevice, _fragmentShaderModule, null);

        if (_vertexShaderModule.Handle != 0)
            _api.DestroyShaderModule(_logicalDevice, _vertexShaderModule, null);

        if (_inFlightFences is not null)
            foreach (var fence in _inFlightFences)
                if (fence.Handle != 0)
                    _api.DestroyFence(_logicalDevice, fence, null);

        if (_renderFinishedSemaphores is not null)
            foreach (var semaphore in _renderFinishedSemaphores)
                if (semaphore.Handle != 0)
                    _api.DestroySemaphore(_logicalDevice, semaphore, null);

        if (_imageAvailableSemaphores is not null)
            foreach (var semaphore in _imageAvailableSemaphores)
                if (semaphore.Handle != 0)
                    _api.DestroySemaphore(_logicalDevice, semaphore, null);

        if (_commandPool.Handle != 0)
            _api.DestroyCommandPool(_logicalDevice, _commandPool, null);

        if (_swapchainImageViews is not null)
            foreach (var imageView in _swapchainImageViews)
                if (imageView.Handle != 0)
                    _api.DestroyImageView(_logicalDevice, imageView, null);

        if (_khrSwapchain is not null && _swapchain.Handle != 0)
            _khrSwapchain.DestroySwapchain(_logicalDevice, _swapchain, null);

        if (_logicalDevice.Handle != 0)
            _api.DestroyDevice(_logicalDevice, null);

        if (_khrSurface is not null && _surface.Handle != 0)
            _khrSurface.DestroySurface(_instance, _surface, null);
        
#if DEBUG
        if (_debugUtils is not null && _debugMessenger.Handle != 0)
            _debugUtils.DestroyDebugUtilsMessenger(_instance, _debugMessenger, null);
#endif

        if (_instance.Handle != 0)
            _api.DestroyInstance(_instance, null);

        _api.Dispose();
        _api = null;
    }

    private readonly IWindow _window;

    private Vk? _api;

    private Instance _instance;
    
#if DEBUG
    private ExtDebugUtils? _debugUtils;

    private DebugUtilsMessengerEXT _debugMessenger;
#endif

    private KhrSurface? _khrSurface;

    private SurfaceKHR _surface;

    private Device _logicalDevice;

    private KhrSwapchain? _khrSwapchain;

    private SwapchainKHR _swapchain;

    private ImageView[]? _swapchainImageViews;

    private CommandPool _commandPool;

    private Semaphore[]? _imageAvailableSemaphores;

    private Semaphore[]? _renderFinishedSemaphores;

    private Fence[]? _inFlightFences;

    private ShaderModule _vertexShaderModule;

    private ShaderModule _fragmentShaderModule;

    private Pipeline _pipeline;

    private PipelineLayout _pipelineLayout;
}