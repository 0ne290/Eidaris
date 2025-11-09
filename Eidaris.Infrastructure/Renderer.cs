using Eidaris.Infrastructure.Helpers;
using Eidaris.Infrastructure.Interfaces;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;

namespace Eidaris.Infrastructure;

public sealed class Renderer : IRenderer
{
    private struct SwapchainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;

        public bool FormatsAvailable;

        public bool PresentModesAvailable;

        public bool IsAdequate => FormatsAvailable && PresentModesAvailable;
    }

    private struct QueueFamilyIndices
    {
        public uint? GraphicsFamily;

        public uint? PresentFamily;

        public bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
    }

    public unsafe void Initialize(IWindow window)
    {
        var api = Vk.GetApi();

        var instance = CreateInstance();

        if (!api.TryGetInstanceExtension(instance, out KhrSurface khrSurface))
            throw new Exception("VK_KHR_surface extension not available.");

        var surface = CreateSurface();

        return;

        Instance CreateInstance()
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

        SurfaceKHR CreateSurface() =>
            window.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();

        (PhysicalDevice device, QueueFamilyIndices indices) SelectBestPhysicalDevice()
        {
            uint deviceCount = 0;
            api.EnumeratePhysicalDevices(instance, &deviceCount, null);
            if (deviceCount == 0)
                throw new Exception("No Vulkan-compatible GPUs found.");

            var devices = stackalloc PhysicalDevice[(int)deviceCount];
            api.EnumeratePhysicalDevices(instance, &deviceCount, devices);

            PhysicalDevice bestDevice = default;
            QueueFamilyIndices bestIndices = default;
            int bestScore = -1;

            for (int i = 0; i < deviceCount; i++)
            {
                var device = devices[i];
                var (score, indices) = EvaluateDevice(device);

                if (score > bestScore)
                {
                    bestDevice = device;
                    bestIndices = indices;
                    bestScore = score;
                }
            }

            if (bestDevice.Handle == 0)
                throw new Exception("No suitable GPU found for Eidaris Renderer.");

            return (bestDevice, bestIndices);

            (int score, QueueFamilyIndices indices) EvaluateDevice(PhysicalDevice device)
            {
                PhysicalDeviceProperties props;
                api.GetPhysicalDeviceProperties(device, &props);
                PhysicalDeviceFeatures features;
                api.GetPhysicalDeviceFeatures(device, &features);

                var indices = FindQueueFamilies();
                if (!indices.IsComplete)
                    return (0, indices);

                if (!CheckDeviceExtensionSupport())
                    return (0, indices);

                var swapchain = QuerySwapchainSupport();
                if (!swapchain.IsAdequate)
                    return (0, indices);

                // Начальный рейтинг устройства
                int score = 0;

                // Дискретная GPU получает огромный приоритет
                if (props.DeviceType == PhysicalDeviceType.DiscreteGpu) score += 1000;

                // Добавляем вес по размеру видеопамяти (если доступно через memory heaps)
                PhysicalDeviceMemoryProperties memProps;
                api.GetPhysicalDeviceMemoryProperties(device, &memProps);
                for (int i = 0; i < memProps.MemoryHeapCount; i++)
                {
                    if ((memProps.MemoryHeaps[i].Flags & MemoryHeapFlags.MemoryHeapDeviceLocalBit) != 0)
                        score += (int)(memProps.MemoryHeaps[i].Size / (1024 * 1024 * 1024)); // +1 балл за каждый ГБ
                }

                // Проверяем важные фичи
                if (features.SamplerAnisotropy) score += 100;
                if (features.GeometryShader) score += 50;
                if (features.TessellationShader) score += 50;
                if (features.WideLines) score += 20;
                if (features.FillModeNonSolid) score += 20;
                if (features.MultiDrawIndirect) score += 30;
                if (features.ShaderInt64) score += 40;

                // Можно добавить вес за поддержку timeline семафоров
                if (SupportsTimelineSemaphores()) score += 150;

                return (score, indices);

                QueueFamilyIndices FindQueueFamilies()
                {
                    uint count = 0;
                    api.GetPhysicalDeviceQueueFamilyProperties(device, &count, null);

                    var families = stackalloc QueueFamilyProperties[(int)count];
                    api.GetPhysicalDeviceQueueFamilyProperties(device, &count, families);

                    QueueFamilyIndices indices = new();

                    for (uint i = 0; i < count; i++)
                    {
                        if ((families[i].QueueFlags & QueueFlags.QueueGraphicsBit) != 0)
                            indices.GraphicsFamily = i;

                        Bool32 presentSupport = false;
                        khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, surface, &presentSupport);
                        if (presentSupport)
                            indices.PresentFamily = i;

                        if (indices.IsComplete)
                            break;
                    }

                    return indices;
                }

                bool CheckDeviceExtensionSupport()
                {
                    uint count = 0;
                    api.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, null);

                    var extensions = stackalloc ExtensionProperties[(int)count];
                    api.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, extensions);

                    var available = new HashSet<string>();
                    for (int i = 0; i < count; i++)
                        available.Add(SilkMarshal.PtrToString((nint)extensions[i].ExtensionName));

                    var required = new[]
                    {
                        "VK_KHR_swapchain",
                        "VK_KHR_timeline_semaphore",
                        "VK_EXT_descriptor_indexing",
                        "VK_KHR_dynamic_rendering",
                        "VK_EXT_memory_budget"
                    };

                    return required.All(available.Contains);
                }

                SwapchainSupportDetails QuerySwapchainSupport()
                {
                    SurfaceCapabilitiesKHR capabilities;
                    khrSurface.GetPhysicalDeviceSurfaceCapabilities(device, surface, &capabilities);

                    uint formatCount = 0;
                    khrSurface.GetPhysicalDeviceSurfaceFormats(device, surface, &formatCount, null);
                    uint presentModeCount = 0;
                    khrSurface.GetPhysicalDeviceSurfacePresentModes(device, surface, &presentModeCount, null);

                    return new SwapchainSupportDetails
                    {
                        Capabilities = capabilities,
                        FormatsAvailable = formatCount > 0,
                        PresentModesAvailable = presentModeCount > 0
                    };
                }

                bool SupportsTimelineSemaphores()
                {
                    PhysicalDeviceTimelineSemaphoreFeatures timelineFeatures = new()
                        { SType = StructureType.PhysicalDeviceTimelineSemaphoreFeatures };
                    PhysicalDeviceFeatures2 features2 = new()
                        { SType = StructureType.PhysicalDeviceFeatures2, PNext = &timelineFeatures };
                    api.GetPhysicalDeviceFeatures2(device, &features2);
                    return timelineFeatures.TimelineSemaphore;
                }
            }
        }
    }
/*public void DrawFrame()
{
    var instance = Vk.CreateInstance(...);
    var surface = Vk.CreateSurfaceKHR(instance, ...);
    var physicalDevice = PickPhysicalDevice(vk, instance);
    var device = Vk.CreateDevice(physicalDevice, ...);
    var queue = GetGraphicsQueue(vk, device);
    var swapchain = CreateSwapchain(vk, device, surface);

    var commandBuffer = AllocateCommandBuffer(vk, device);

    Vk.BeginCommandBuffer(commandBuffer, new CommandBufferBeginInfo());
    Vk.CmdBeginRenderPass(commandBuffer, new RenderPassBeginInfo
    {
        RenderPass = renderPass,
        Framebuffer = framebuffer,
        ClearValueCount = 1,
        PClearValues = new[] { new ClearValue(0f, 0f, 0f, 1f) }
    }, SubpassContents.Inline);

    Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);
    Vk.CmdDraw(commandBuffer, 1, 1, 0, 0);

    Vk.CmdEndRenderPass(commandBuffer);
    Vk.EndCommandBuffer(commandBuffer);

    Submit(queue, commandBuffer);
    Present(queue, swapchain);
}*/
}