using Silk.NET.Vulkan;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class CommandPoolCreator
{
    public CommandPoolCreator(Vk api, Device device, uint queueFamilyIndex)
    {
        _api = api;
        _device = device;
        _queueFamilyIndex = queueFamilyIndex;
    }

    public CommandPool CreateCommandPool()
    {
        var createInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _queueFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        return _api.CreateCommandPool(_device, &createInfo, null, out var commandPool) != Result.Success
            ? throw new Exception("Failed to create command pool.")
            : commandPool;
    }

    public CommandBuffer[] AllocateCommandBuffers(CommandPool commandPool, uint count)
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = count
        };

        var commandBuffers = new CommandBuffer[count];
        fixed (CommandBuffer* pCommandBuffers = commandBuffers)
        {
            if (_api.AllocateCommandBuffers(_device, &allocInfo, pCommandBuffers) != Result.Success)
                throw new Exception("Failed to allocate command buffers.");
        }

        return commandBuffers;
    }

    private readonly Vk _api;
    private readonly Device _device;
    private readonly uint _queueFamilyIndex;
}