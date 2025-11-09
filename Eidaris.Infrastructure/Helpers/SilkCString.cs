using Silk.NET.Core.Native;

namespace Eidaris.Infrastructure.Helpers;

public readonly ref struct SilkCString : IDisposable
{
    public SilkCString(string text) => _ptr = SilkMarshal.StringToPtr(text);
    
    public void Dispose() => SilkMarshal.Free(_ptr);
    
    public static unsafe implicit operator byte*(SilkCString str) => (byte*)str._ptr;

    private readonly nint _ptr;
}