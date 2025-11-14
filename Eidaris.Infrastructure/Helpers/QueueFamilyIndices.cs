namespace Eidaris.Infrastructure.Helpers;

internal record QueueFamilyIndices
{
    public QueueFamilyIndices(uint graphicsFamily, uint presentFamily)
    {
        GraphicsFamily = graphicsFamily;
        PresentFamily = presentFamily;
    }

    public uint GraphicsFamily { get; }
    
    public uint PresentFamily { get; }
    
    public bool AreSame => GraphicsFamily == PresentFamily;
}