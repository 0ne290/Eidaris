using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

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
    
    public required Image[] SwapchainImages { get; init; }
    
    public required ImageView[] SwapchainImageViews { get; init; }
    
    public required CommandPool CommandPool { get; init; }
    
    public required CommandBuffer[] CommandBuffers { get; init; }
    
    public required Semaphore[] ImageAvailableSemaphores { get; init; }
    
    public required Semaphore[] RenderFinishedSemaphores { get; init; }
    
    public required Fence[] InFlightFences { get; init; }
    
    public required ShaderModule VertShaderModule { get; init; }
    
    public required ShaderModule FragShaderModule { get; init; }
    
    public required Pipeline Pipeline { get; init; }
    
    public required PipelineLayout PipelineLayout { get; init; }
}