using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class PhysicalDeviceSelector
{
    private readonly ref struct SwapchainSupportDetails
    {
        public SwapchainSupportDetails(bool formatsAvailable, bool presentModesAvailable)
        {
            _formatsAvailable = formatsAvailable;
            _presentModesAvailable = presentModesAvailable;
        }

        private readonly bool _formatsAvailable;

        private readonly bool _presentModesAvailable;

        public bool IsAdequate => _formatsAvailable && _presentModesAvailable;
    }

    public PhysicalDeviceSelector(Vk api, Instance instance, KhrSurface khrSurface, SurfaceKHR surface)
    {
        _api = api;
        _instance = instance;
        _khrSurface = khrSurface;
        _surface = surface;
    }

    public (PhysicalDevice device, QueueFamilyIndices indices) SelectBestDevice()
    {
        uint deviceCount = 0;
        _api.EnumeratePhysicalDevices(_instance, &deviceCount, null);

        if (deviceCount is 0)
            throw new Exception("No Vulkan-compatible GPUs found on this system.");

        Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)deviceCount];
        _api.EnumeratePhysicalDevices(_instance, &deviceCount, devices);

        PhysicalDevice bestDevice = default;
        QueueFamilyIndices bestIndices = default;
        var bestScore = -1;

        foreach (var device in devices)
        {
            if (!TryEvaluateDevice(device, out var score, out var indices))
                continue;

            if (score <= bestScore)
                continue;

            bestDevice = device;
            bestIndices = indices;
            bestScore = score;
        }

        return bestDevice.Handle is 0
            ? throw new Exception("No suitable GPU found for Eidaris Renderer.")
            : (bestDevice, bestIndices);
    }

    private bool TryEvaluateDevice(PhysicalDevice device, out int score, out QueueFamilyIndices indices)
    {
        score = 0;
        indices = default;

        PhysicalDeviceProperties props;
        _api.GetPhysicalDeviceProperties(device, &props);

        PhysicalDeviceFeatures features;
        _api.GetPhysicalDeviceFeatures(device, &features);

        var indicesBuilder = FindQueueFamilies(device);
        if (!indicesBuilder.IsComplete || !CheckDeviceExtensionSupport(device))
            return false;

        var swapchain = QuerySwapchainSupport(device);
        if (!swapchain.IsAdequate)
            return false;

        indices = indicesBuilder.Build();

        if (props.DeviceType == PhysicalDeviceType.DiscreteGpu)
            score += DiscreteGpuBonus;

        PhysicalDeviceMemoryProperties memProps;
        _api.GetPhysicalDeviceMemoryProperties(device, &memProps);

        for (var i = 0; i < memProps.MemoryHeapCount; i++)
            if ((memProps.MemoryHeaps[i].Flags & MemoryHeapFlags.DeviceLocalBit) != 0)
                score += (int)(memProps.MemoryHeaps[i].Size / (1024 * 1024 * 1024)) * VramGbWeight;

        if (features.SamplerAnisotropy)
            score += AnisotropyBonus;
        if (features.GeometryShader)
            score += GeometryShaderBonus;
        if (features.TessellationShader)
            score += TessellationBonus;
        if (features.WideLines)
            score += WideLinesBonus;
        if (features.FillModeNonSolid)
            score += FillModeBonus;
        if (features.MultiDrawIndirect)
            score += MultiDrawBonus;
        if (features.ShaderInt64)
            score += ShaderInt64Bonus;

        if (SupportsTimelineSemaphores(device))
            score += TimelineSemaphoresBonus;

        return true;
    }

    private QueueFamilyIndicesBuilder FindQueueFamilies(PhysicalDevice device)
    {
        uint count = 0;
        _api.GetPhysicalDeviceQueueFamilyProperties(device, &count, null);

        Span<QueueFamilyProperties> families = stackalloc QueueFamilyProperties[(int)count];
        _api.GetPhysicalDeviceQueueFamilyProperties(device, &count, families);

        QueueFamilyIndicesBuilder indices = new();

        for (uint i = 0; i < count; i++)
        {
            if ((families[(int)i].QueueFlags & QueueFlags.GraphicsBit) != 0)
                indices.GraphicsFamily = i;

            Bool32 presentSupport = false;
            _khrSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, &presentSupport);
            if (presentSupport)
                indices.PresentFamily = i;

            if (indices.IsComplete)
                break;
        }

        return indices;
    }

    private bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint count = 0;
        _api.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, null);

        Span<ExtensionProperties> extensions = stackalloc ExtensionProperties[(int)count];
        _api.EnumerateDeviceExtensionProperties(device, (byte*)null, &count, extensions);

        foreach (var required in Constants.RequiredDeviceExtensions)
        {
            var found = false;

            foreach (var ext in extensions)
            {
                var extName = SilkMarshal.PtrToString((nint)ext.ExtensionName);
                if (extName != required)
                    continue;

                found = true;

                break;
            }

            if (!found)
                return false;
        }

        return true;
    }

    private SwapchainSupportDetails QuerySwapchainSupport(PhysicalDevice device)
    {
        uint formatCount = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(device, _surface, &formatCount, null);

        uint presentModeCount = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(device, _surface, &presentModeCount, null);

        return new SwapchainSupportDetails(formatCount > 0, presentModeCount > 0);
    }

    private bool SupportsTimelineSemaphores(PhysicalDevice device)
    {
        PhysicalDeviceTimelineSemaphoreFeatures timelineFeatures = new()
            { SType = StructureType.PhysicalDeviceTimelineSemaphoreFeatures };

        PhysicalDeviceFeatures2 features2 = new()
            { SType = StructureType.PhysicalDeviceFeatures2, PNext = &timelineFeatures };

        _api.GetPhysicalDeviceFeatures2(device, &features2);

        return timelineFeatures.TimelineSemaphore;
    }

    private readonly Vk _api;

    private readonly Instance _instance;

    private readonly KhrSurface _khrSurface;

    private readonly SurfaceKHR _surface;

    private const int DiscreteGpuBonus = 1000;

    private const int VramGbWeight = 1;

    private const int AnisotropyBonus = 100;

    private const int GeometryShaderBonus = 50;

    private const int TessellationBonus = 50;

    private const int WideLinesBonus = 20;

    private const int FillModeBonus = 20;

    private const int MultiDrawBonus = 30;

    private const int ShaderInt64Bonus = 40;

    private const int TimelineSemaphoresBonus = 150;
}