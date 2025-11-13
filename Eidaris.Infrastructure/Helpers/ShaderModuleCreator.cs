using Silk.NET.Vulkan;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class ShaderModuleCreator
{
    public ShaderModuleCreator(Vk api, Device device)
    {
        _api = api;
        _device = device;
    }

    public ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)pCode
            };

            return _api.CreateShaderModule(_device, &createInfo, null, out var shaderModule) != Result.Success
                ? throw new Exception("Failed to create shader module.")
                : shaderModule;
        }
    }

    private readonly Vk _api;
    private readonly Device _device;
}