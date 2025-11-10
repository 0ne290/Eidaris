namespace Eidaris.Infrastructure.Helpers;

internal readonly struct QueueFamilyIndices
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