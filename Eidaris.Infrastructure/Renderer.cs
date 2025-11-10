using Eidaris.Infrastructure.Helpers;
using Eidaris.Infrastructure.Interfaces;
using Silk.NET.Windowing;

namespace Eidaris.Infrastructure;

public sealed class Renderer : IRenderer
{
    public void Initialize(IWindow window)
    {
        var initializer = new RendererInitializer(window);
        var initializationContext = initializer.Initialize();
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