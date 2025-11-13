using Silk.NET.Vulkan;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class PipelineCreator
{
    public PipelineCreator(Vk api, Device device, Extent2D extent, Format colorFormat)
    {
        _api = api;
        _device = device;
        _extent = extent;
        _colorFormat = colorFormat;
    }

    public (Pipeline pipeline, PipelineLayout layout) Create(ShaderModule vertShader, ShaderModule fragShader)
    {
        var layout = CreatePipelineLayout();
        var pipeline = CreateGraphicsPipeline(vertShader, fragShader, layout);

        return (pipeline, layout);
    }

    private PipelineLayout CreatePipelineLayout()
    {
        var pushConstantRanges = stackalloc PushConstantRange[2];

        // Vertex shader push constants
        pushConstantRanges[0] = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = (uint)sizeof(PointRenderData.VertexPushConstants)
        };

        // Fragment shader push constants
        pushConstantRanges[1] = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.FragmentBit,
            Offset = (uint)sizeof(PointRenderData.VertexPushConstants),
            Size = (uint)sizeof(PointRenderData.FragmentPushConstants)
        };

        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PSetLayouts = null,
            PushConstantRangeCount = 2,
            PPushConstantRanges = pushConstantRanges
        };

        return _api.CreatePipelineLayout(_device, &layoutInfo, null, out var layout) != Result.Success
            ? throw new Exception("Failed to create pipeline layout.")
            : layout;
    }

    private Pipeline CreateGraphicsPipeline(ShaderModule vertShader, ShaderModule fragShader, PipelineLayout layout)
    {
        using var mainName = new SilkCString("main");

        var vertStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShader,
            PName = mainName
        };

        var fragStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShader,
            PName = mainName
        };

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[] { vertStageInfo, fragStageInfo };

        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0
        };

        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.PointList,
            PrimitiveRestartEnable = false
        };

        var viewport = new Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = _extent.Width,
            Height = _extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = _extent
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false
        };

        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };

        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit |
                             ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        var colorFormat = _colorFormat;
        var renderingInfo = new PipelineRenderingCreateInfo
        {
            SType = StructureType.PipelineRenderingCreateInfo,
            ColorAttachmentCount = 1,
            PColorAttachmentFormats = &colorFormat
        };

        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = layout,
            Subpass = 0,
            BasePipelineHandle = default,
            PNext = &renderingInfo
        };

        return _api.CreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, out var pipeline) !=
               Result.Success
            ? throw new Exception("Failed to create graphics pipeline.")
            : pipeline;
    }

    private readonly Vk _api;
    private readonly Device _device;
    private readonly Extent2D _extent;
    private readonly Format _colorFormat;
}