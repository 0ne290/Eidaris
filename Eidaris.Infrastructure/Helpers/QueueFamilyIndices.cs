namespace Eidaris.Infrastructure.Helpers;

internal struct QueueFamilyIndices
{
    public uint? GraphicsFamily;
    
    public uint? PresentFamily;

    public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;
}