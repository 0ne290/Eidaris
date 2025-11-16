using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class VulkanDebugNamer
{
    public VulkanDebugNamer(Device device, ExtDebugUtils? debugUtils)
    {
        _device = device;
        _debugUtils = debugUtils;
    }

    private void SetObjectName(ulong handle, ObjectType objectType, string name)
    {
#if DEBUG
        if (_debugUtils is null)
            return;

        using var nameString = new SilkCString(name);

        var nameInfo = new DebugUtilsObjectNameInfoEXT
        {
            SType = StructureType.DebugUtilsObjectNameInfoExt,
            ObjectType = objectType,
            ObjectHandle = handle,
            PObjectName = nameString
        };

        _debugUtils.SetDebugUtilsObjectName(_device, &nameInfo);
#endif
    }

    public void NameCommandBuffer(CommandBuffer commandBuffer, string name) =>
        SetObjectName((ulong)commandBuffer.Handle, ObjectType.CommandBuffer, name);

    public void NameSemaphore(Semaphore semaphore, string name) =>
        SetObjectName(semaphore.Handle, ObjectType.Semaphore, name);

    public void NameFence(Fence fence, string name) =>
        SetObjectName(fence.Handle, ObjectType.Fence, name);

    public void NameSwapchain(SwapchainKHR swapchain, string name) =>
        SetObjectName(swapchain.Handle, ObjectType.SwapchainKhr, name);

    public void NameImageView(ImageView imageView, string name) =>
        SetObjectName(imageView.Handle, ObjectType.ImageView, name);

    public void NamePipeline(Pipeline pipeline, string name) =>
        SetObjectName(pipeline.Handle, ObjectType.Pipeline, name);

    public void NameCommandPool(CommandPool pool, string name) =>
        SetObjectName(pool.Handle, ObjectType.CommandPool, name);

    private readonly Device _device;
    private readonly ExtDebugUtils? _debugUtils;
}