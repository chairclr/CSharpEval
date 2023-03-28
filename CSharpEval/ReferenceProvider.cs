using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace CSharpEval;

public static class ReferenceProvider
{
    public static PortableExecutableReference GetBestPEReference(Assembly assembly)
    {
        if (assembly.Location is not null && File.Exists(assembly.Location))
        {
            return MetadataReference.CreateFromFile(assembly.Location);
        }

        return GetInMemoryReference(assembly)!;
    }

    public static unsafe PortableExecutableReference? GetInMemoryReference(Assembly assembly)
    {
        if (assembly.TryGetRawMetadata(out byte* blob, out int length))
        {
            ModuleMetadata moduleMetadata = ModuleMetadata.CreateFromMetadata((nint)blob, length);
            return AssemblyMetadata.Create(moduleMetadata).GetReference();
        }

        return null;
    }

    private static unsafe Stream? GetInMemoryStream(Assembly assembly)
    {
        if (assembly.TryGetRawMetadata(out byte* blob, out int length))
        {
            byte[] arr = new byte[length];
            Marshal.Copy((IntPtr)blob, arr, 0, length);
            MemoryStream stream = new MemoryStream(arr);

            return stream;
        }

        return null;
    }
}
