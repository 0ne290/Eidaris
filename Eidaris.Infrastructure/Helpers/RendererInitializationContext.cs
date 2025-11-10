using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Eidaris.Infrastructure.Helpers;

internal readonly ref struct RendererInitializationContext
{
    public required Vk Api { get; init; }
    
    public required Instance Instance { get; init; }
    
    public required KhrSurface KhrSurface { get; init; }
    
    public required SurfaceKHR Surface { get; init; }
    
    public required PhysicalDevice PhysicalDevice { get; init; }
    
    public required QueueFamilyIndices QueueIndices { get; init; }

    public required Device LogicalDevice { get; init; }
    
    public required Queue GraphicsQueue { get; init; }
    
    public required Queue PresentQueue { get; init; }
    
    public required KhrSwapchain KhrSwapchain { get; init; }
    
    public required SwapchainKHR Swapchain { get; init; }
    
    public required SurfaceFormatKHR SwapchainFormat { get; init; }
    
    public required Extent2D SwapchainExtent { get; init; }
}