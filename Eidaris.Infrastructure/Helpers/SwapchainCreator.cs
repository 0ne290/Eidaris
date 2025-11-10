using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class SwapchainCreator
{
    public SwapchainCreator(
        PhysicalDevice physicalDevice,
        Device logicalDevice,
        KhrSurface khrSurface,
        SurfaceKHR surface,
        KhrSwapchain khrSwapchain,
        QueueFamilyIndices queueIndices,
        uint windowWidth,
        uint windowHeight)
    {
        _physicalDevice = physicalDevice;
        _logicalDevice = logicalDevice;
        _khrSurface = khrSurface;
        _surface = surface;

        SurfaceCapabilitiesKHR tempCapabilities;
        _khrSurface.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, &tempCapabilities);
        _capabilities = tempCapabilities;

        _khrSwapchain = khrSwapchain;
        _queueIndices = queueIndices;
        _windowWidth = windowWidth;
        _windowHeight = windowHeight;
    }

    public (SwapchainKHR swapchain, SurfaceFormatKHR surfaceFormat, Extent2D extent) Create()
    {
        var surfaceFormat = ChooseSurfaceFormat();
        var presentMode = ChoosePresentMode();
        var extent = ChooseExtent();
        var imageCount = ChooseImageCount();

        var swapchain = CreateSwapchainKhr(surfaceFormat, presentMode, extent, imageCount);

        return (swapchain, surfaceFormat, extent);
    }

    private SurfaceFormatKHR ChooseSurfaceFormat()
    {
        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &count, null);

        var formats = new SurfaceFormatKHR[count];
        fixed (SurfaceFormatKHR* pFormats = formats)
            _khrSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, &count, pFormats);

        foreach (var format in formats)
            if (format is { Format: Format.B8G8R8A8Srgb, ColorSpace: ColorSpaceKHR.SpaceSrgbNonlinearKhr })
                return format;

        return formats[0];
    }

    private PresentModeKHR ChoosePresentMode()
    {
        uint count = 0;
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &count, null);

        var modes = new PresentModeKHR[count];
        fixed (PresentModeKHR* pModes = modes)
            _khrSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, &count, pModes);

        foreach (var mode in modes)
            if (mode == PresentModeKHR.MailboxKhr)
                return mode;

        return PresentModeKHR.FifoKhr;
    }

    private Extent2D ChooseExtent()
    {
        if (_capabilities.CurrentExtent.Width != uint.MaxValue)
            return _capabilities.CurrentExtent;

        return new Extent2D
        {
            Width = Math.Clamp(_windowWidth, _capabilities.MinImageExtent.Width, _capabilities.MaxImageExtent.Width),
            Height = Math.Clamp(_windowHeight, _capabilities.MinImageExtent.Height, _capabilities.MaxImageExtent.Height)
        };
    }

    private uint ChooseImageCount()
    {
        var imageCount = _capabilities.MinImageCount + 1;

        if (_capabilities.MaxImageCount > 0 && imageCount > _capabilities.MaxImageCount)
            imageCount = _capabilities.MaxImageCount;

        return imageCount;
    }

    private SwapchainKHR CreateSwapchainKhr(
        SurfaceFormatKHR surfaceFormat,
        PresentModeKHR presentMode,
        Extent2D extent,
        uint imageCount)
    {
        var queueFamilyIndices = stackalloc uint[] { _queueIndices.GraphicsFamily, _queueIndices.PresentFamily };

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = _surface,
            MinImageCount = imageCount,
            ImageFormat = surfaceFormat.Format,
            ImageColorSpace = surfaceFormat.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = _capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        if (_queueIndices.GraphicsFamily != _queueIndices.PresentFamily)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = queueFamilyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        return _khrSwapchain.CreateSwapchain(_logicalDevice, &createInfo, null, out var swapchain) != Result.Success
            ? throw new Exception("Failed to create swapchain.")
            : swapchain;
    }

    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _logicalDevice;
    private readonly KhrSurface _khrSurface;
    private readonly SurfaceKHR _surface;
    private readonly SurfaceCapabilitiesKHR _capabilities;
    private readonly KhrSwapchain _khrSwapchain;
    private readonly QueueFamilyIndices _queueIndices;
    private readonly uint _windowWidth;
    private readonly uint _windowHeight;
}