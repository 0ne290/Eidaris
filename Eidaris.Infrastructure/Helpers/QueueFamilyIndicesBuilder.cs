namespace Eidaris.Infrastructure.Helpers;

internal record QueueFamilyIndicesBuilder
{
    public uint? GraphicsFamily;

    public uint? PresentFamily;

    public bool IsComplete => GraphicsFamily.HasValue && PresentFamily.HasValue;

    public QueueFamilyIndices Build()
    {
        return !IsComplete
            ? throw new InvalidOperationException("Queue family indices are incomplete.")
            : new QueueFamilyIndices(GraphicsFamily!.Value, PresentFamily!.Value);
    }
}