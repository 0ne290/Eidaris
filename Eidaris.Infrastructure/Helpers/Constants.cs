namespace Eidaris.Infrastructure.Helpers;

internal static class Constants
{
    public static readonly string[] RequiredDeviceExtensions =
    [
        "VK_KHR_swapchain",
        "VK_KHR_timeline_semaphore",
        "VK_EXT_descriptor_indexing",
        "VK_KHR_dynamic_rendering",
        "VK_EXT_memory_budget"
    ];
}