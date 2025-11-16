using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace Eidaris.Infrastructure.Helpers;

internal static unsafe class DebugMessengerCreator
{
    public static DebugUtilsMessengerEXT Create(Instance instance, ExtDebugUtils debugUtils)
    {
        var createInfo = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity =
                DebugUtilsMessageSeverityFlagsEXT.InfoBitExt |
                DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
            MessageType =
                DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback)
        };

        return debugUtils.CreateDebugUtilsMessenger(instance, &createInfo, null, out var messenger) != Result.Success
            ? throw new Exception("Failed to create debug messenger.")
            : messenger;
    }

    private static uint DebugCallback(
        DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT types,
        DebugUtilsMessengerCallbackDataEXT* pData,
        void* pUserData)
    {
        var message = Marshal.PtrToStringAnsi((nint)pData->PMessage);

        var severityPrefix = severity switch
        {
            DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => "[ERROR]",
            DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => "[WARN]",
            DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => "[INFO]",
            _ => "[DEBUG]"
        };

        var typePrefix = types switch
        {
            DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt => "[PERF]",
            DebugUtilsMessageTypeFlagsEXT.ValidationBitExt => "[VALID]",
            _ => "[GENERAL]"
        };

        Console.WriteLine($"[VULKAN]{severityPrefix}{typePrefix} {message}");

        return Vk.False;
    }
}