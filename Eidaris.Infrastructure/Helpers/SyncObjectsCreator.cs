using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class SyncObjectsCreator
{
    public SyncObjectsCreator(Vk api, Device device, uint framesInFlight)
    {
        _api = api;
        _device = device;
        _framesInFlight = framesInFlight;
    }

    public (Semaphore[] imageAvailable, Semaphore[] renderFinished, Fence[] inFlight) Create()
    {
        var imageAvailableSemaphores = new Semaphore[_framesInFlight];
        var renderFinishedSemaphores = new Semaphore[_framesInFlight];
        var inFlightFences = new Fence[_framesInFlight];

        var semaphoreInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        var fenceInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit
        };

        for (var i = 0; i < _framesInFlight; i++)
        {
            if (_api.CreateSemaphore(_device, &semaphoreInfo, null, out imageAvailableSemaphores[i]) != Result.Success)
                throw new Exception("Failed to create image available semaphore.");

            if (_api.CreateSemaphore(_device, &semaphoreInfo, null, out renderFinishedSemaphores[i]) != Result.Success)
                throw new Exception("Failed to create render finished semaphore.");

            if (_api.CreateFence(_device, &fenceInfo, null, out inFlightFences[i]) != Result.Success)
                throw new Exception("Failed to create in-flight fence.");
        }

        return (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences);
    }

    private readonly Vk _api;
    private readonly Device _device;
    private readonly uint _framesInFlight;
}