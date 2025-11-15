using Silk.NET.Vulkan;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class LogicalDeviceCreator
{
    public LogicalDeviceCreator(Vk api, PhysicalDevice physicalDevice, QueueFamilyIndices queueIndices)
    {
        _api = api;
        _physicalDevice = physicalDevice;
        _queueIndices = queueIndices;
    }

    public (Device device, Queue graphicsQueue, Queue presentQueue) Create()
    {
        var device = CreateLogicalDevice();
        var graphicsQueue = GetQueue(device, _queueIndices.GraphicsFamily, 0);
        var presentQueue = GetQueue(device, _queueIndices.PresentFamily, 0);

        return (device, graphicsQueue, presentQueue);
    }

    private Device CreateLogicalDevice()
    {
        var uniqueQueueFamilies = GetUniqueQueueFamilies();
        var queueCreateInfos = CreateQueueCreateInfos(uniqueQueueFamilies);
        var enabledFeatures = GetEnabledFeatures();

        var extensionCount = Constants.RequiredDeviceExtensions.Length;
        Span<SilkCString> extensionStrings = stackalloc SilkCString[extensionCount];
        var extensionPointers = stackalloc byte*[extensionCount];

        try
        {
            for (var i = 0; i < extensionCount; i++)
            {
                extensionStrings[i] = new SilkCString(Constants.RequiredDeviceExtensions[i]);
                extensionPointers[i] = extensionStrings[i];
            }

            fixed (DeviceQueueCreateInfo* pQueueCreateInfos = queueCreateInfos)
            {
                var deviceCreateInfo = new DeviceCreateInfo
                {
                    SType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = (uint)queueCreateInfos.Length,
                    PQueueCreateInfos = pQueueCreateInfos,
                    PEnabledFeatures = &enabledFeatures,
                    EnabledExtensionCount = (uint)extensionCount,
                    PpEnabledExtensionNames = extensionPointers
                };

                return _api.CreateDevice(_physicalDevice, &deviceCreateInfo, null, out var device) != Result.Success
                    ? throw new Exception("Failed to create logical device.")
                    : device;
            }
        }
        finally
        {
            // Cleanup всегда выполняется
            for (var i = 0; i < extensionCount; i++)
                extensionStrings[i].Dispose();
        }
    }

    private uint[] GetUniqueQueueFamilies()
    {
        return _queueIndices.AreSame
            ? [_queueIndices.GraphicsFamily]
            : [_queueIndices.GraphicsFamily, _queueIndices.PresentFamily];
    }

    private static DeviceQueueCreateInfo[] CreateQueueCreateInfos(uint[] uniqueQueueFamilies)
    {
        var queuePriority = 1.0f;
        var infos = new DeviceQueueCreateInfo[uniqueQueueFamilies.Length];

        for (var i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            infos[i] = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        return infos;
    }

    private PhysicalDeviceFeatures GetEnabledFeatures()
    {
        PhysicalDeviceFeatures availableFeatures;
        _api.GetPhysicalDeviceFeatures(_physicalDevice, &availableFeatures);

        return new PhysicalDeviceFeatures
        {
            SamplerAnisotropy = availableFeatures.SamplerAnisotropy,
            FillModeNonSolid = availableFeatures.FillModeNonSolid,
            WideLines = availableFeatures.WideLines,
            GeometryShader = availableFeatures.GeometryShader,
            TessellationShader = availableFeatures.TessellationShader,
            MultiDrawIndirect = availableFeatures.MultiDrawIndirect,
            ShaderInt64 = availableFeatures.ShaderInt64
        };
    }

    private Queue GetQueue(Device device, uint queueFamilyIndex, uint queueIndex)
    {
        _api.GetDeviceQueue(device, queueFamilyIndex, queueIndex, out var queue);
        return queue;
    }

    private readonly Vk _api;
    private readonly PhysicalDevice _physicalDevice;
    private readonly QueueFamilyIndices _queueIndices;
}