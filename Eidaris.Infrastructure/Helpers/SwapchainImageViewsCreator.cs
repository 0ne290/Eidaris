using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Eidaris.Infrastructure.Helpers;

internal sealed unsafe class SwapchainImageViewsCreator
{
    public SwapchainImageViewsCreator(
        Vk api,
        Device device,
        KhrSwapchain khrSwapchain,
        SwapchainKHR swapchain,
        Format imageFormat)
    {
        _api = api;
        _device = device;
        _khrSwapchain = khrSwapchain;
        _swapchain = swapchain;
        _imageFormat = imageFormat;
    }

    public (Image[] images, ImageView[] imageViews) Create()
    {
        var images = GetSwapchainImages();
        var imageViews = new ImageView[images.Length];

        for (var i = 0; i < images.Length; i++)
            imageViews[i] = CreateImageView(images[i]);

        return (images, imageViews);
    }

    private Image[] GetSwapchainImages()
    {
        uint count = 0;
        _khrSwapchain.GetSwapchainImages(_device, _swapchain, &count, null);

        var images = new Image[count];
        fixed (Image* pImages = images)
            _khrSwapchain.GetSwapchainImages(_device, _swapchain, &count, pImages);

        return images;
    }

    private ImageView CreateImageView(Image image)
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = _imageFormat,
            Components = new ComponentMapping
            {
                R = ComponentSwizzle.Identity,
                G = ComponentSwizzle.Identity,
                B = ComponentSwizzle.Identity,
                A = ComponentSwizzle.Identity
            },
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        return _api.CreateImageView(_device, &createInfo, null, out var imageView) != Result.Success
            ? throw new Exception("Failed to create image view.")
            : imageView;
    }

    private readonly Vk _api;
    private readonly Device _device;
    private readonly KhrSwapchain _khrSwapchain;
    private readonly SwapchainKHR _swapchain;
    private readonly Format _imageFormat;
}