namespace Eidaris.Infrastructure.Helpers;

internal static class Constants
{
    public static readonly string[] RequiredDeviceExtensions =
    [
        "VK_KHR_swapchain",
        "VK_EXT_memory_budget"
    ];
    
    public const uint MaxFramesInFlight = 2;
}