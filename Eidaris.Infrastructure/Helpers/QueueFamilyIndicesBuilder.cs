namespace Eidaris.Infrastructure.Helpers;

internal struct QueueFamilyIndicesBuilder
{
    public uint? GraphicsFamily;
    
    public uint? PresentFamily;

    public readonly bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;

    public readonly QueueFamilyIndices Build()
    {
        if (!IsComplete)
            throw new InvalidOperationException("Queue family indices are incomplete.");

        return new QueueFamilyIndices(GraphicsFamily!.Value, PresentFamily!.Value);
    }
}