using Eidaris.Infrastructure.Helpers;
using Eidaris.Infrastructure.Interfaces;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Eidaris.Infrastructure;

public sealed unsafe class Renderer : IRenderer
{
    public void Initialize(IWindow window)
    {
        var initializer = new RendererInitializer(window);
        _initializationContext = initializer.Initialize();
    }

    public void DrawFrame(PointRenderData point)
    {
        var fence = _initializationContext.InFlightFences[_currentFrame];

        _initializationContext.Api.WaitForFences(_initializationContext.LogicalDevice, 1, fence, true, ulong.MaxValue);

        uint imageIndex;
        var result = _initializationContext.KhrSwapchain.AcquireNextImage(
            _initializationContext.LogicalDevice,
            _initializationContext.Swapchain,
            ulong.MaxValue,
            _initializationContext.ImageAvailableSemaphores[_currentFrame],
            default,
            &imageIndex);

        if (result != Result.Success)
            throw new Exception($"Failed to acquire swapchain image: {result}");

        _initializationContext.Api.ResetFences(_initializationContext.LogicalDevice, 1, fence);

        var commandBuffer = _initializationContext.CommandBuffers[_currentFrame];
        _initializationContext.Api.ResetCommandBuffer(commandBuffer, 0);

        RecordCommandBuffer(commandBuffer, imageIndex, point);

        SubmitCommandBuffer(commandBuffer);

        PresentFrame(imageIndex);

        _currentFrame = (_currentFrame + 1) % Constants.MaxFramesInFlight;
    }

    private void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, PointRenderData point)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo
        };

        _initializationContext.Api.BeginCommandBuffer(commandBuffer, &beginInfo);

        TransitionImageLayout(commandBuffer, _initializationContext.SwapchainImages[imageIndex],
            ImageLayout.Undefined, ImageLayout.ColorAttachmentOptimal);

        BeginRendering(commandBuffer, imageIndex);

        _initializationContext.Api.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics,
            _initializationContext.Pipeline);

        PushConstants(commandBuffer, point);

        _initializationContext.Api.CmdDraw(commandBuffer, 1, 1, 0, 0);

        _initializationContext.Api.CmdEndRendering(commandBuffer);

        TransitionImageLayout(commandBuffer, _initializationContext.SwapchainImages[imageIndex],
            ImageLayout.ColorAttachmentOptimal, ImageLayout.PresentSrcKhr);

        _initializationContext.Api.EndCommandBuffer(commandBuffer);
    }

    private void TransitionImageLayout(CommandBuffer commandBuffer, Image image,
        ImageLayout oldLayout, ImageLayout newLayout)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        PipelineStageFlags srcStage, dstStage;

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (oldLayout)
        {
            case ImageLayout.Undefined when newLayout == ImageLayout.ColorAttachmentOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;
                srcStage = PipelineStageFlags.TopOfPipeBit;
                dstStage = PipelineStageFlags.ColorAttachmentOutputBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.PresentSrcKhr:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = 0;
                srcStage = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStage = PipelineStageFlags.BottomOfPipeBit;
                break;
            default:
                throw new Exception("Unsupported layout transition.");
        }

        _initializationContext.Api.CmdPipelineBarrier(commandBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1,
            &barrier);
    }

    private void BeginRendering(CommandBuffer commandBuffer, uint imageIndex)
    {
        var clearColor = new ClearColorValue(0.0f, 0.0f, 0.0f, 1.0f);

        var colorAttachment = new RenderingAttachmentInfo
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = _initializationContext.SwapchainImageViews[imageIndex],
            ImageLayout = ImageLayout.ColorAttachmentOptimal,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new ClearValue { Color = clearColor }
        };

        var renderingInfo = new RenderingInfo
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = _initializationContext.SwapchainExtent
            },
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachment
        };

        _initializationContext.Api.CmdBeginRendering(commandBuffer, &renderingInfo);
    }

    private void PushConstants(CommandBuffer commandBuffer, PointRenderData point)
    {
        var vertex = point.Vertex;
        var fragment = point.Fragment;

        // TODO: не дублирование ли это PipelineCreator.CreatePipelineLayout?
        _initializationContext.Api.CmdPushConstants(commandBuffer, _initializationContext.PipelineLayout,
            ShaderStageFlags.VertexBit, 0, (uint)sizeof(PointRenderData.VertexPushConstants), &vertex);
        _initializationContext.Api.CmdPushConstants(commandBuffer, _initializationContext.PipelineLayout,
            ShaderStageFlags.FragmentBit, (uint)sizeof(PointRenderData.VertexPushConstants),
            (uint)sizeof(PointRenderData.FragmentPushConstants), &fragment);
    }

    private void SubmitCommandBuffer(CommandBuffer commandBuffer)
    {
        var waitSemaphore = _initializationContext.ImageAvailableSemaphores[_currentFrame];
        var signalSemaphore = _initializationContext.RenderFinishedSemaphores[_currentFrame];
        var waitStage = PipelineStageFlags.ColorAttachmentOutputBit;

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &waitSemaphore,
            PWaitDstStageMask = &waitStage,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &signalSemaphore
        };

        if (_initializationContext.Api.QueueSubmit(_initializationContext.GraphicsQueue, 1, &submitInfo,
                _initializationContext.InFlightFences[_currentFrame]) != Result.Success)
        {
            throw new Exception("Failed to submit command buffer.");
        }
    }

    private void PresentFrame(uint imageIndex)
    {
        var signalSemaphore = _initializationContext.RenderFinishedSemaphores[_currentFrame];
        var swapchain = _initializationContext.Swapchain;

        var presentInfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &signalSemaphore,
            SwapchainCount = 1,
            PSwapchains = &swapchain,
            PImageIndices = &imageIndex
        };

        _initializationContext.KhrSwapchain.QueuePresent(_initializationContext.PresentQueue, &presentInfo);
    }

    private RendererInitializationContext _initializationContext = null!;

    private uint _currentFrame;
}