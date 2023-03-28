using Microsoft.CodeAnalysis;

namespace CSharpEval.Tests.Unit;

public class ReferenceProviderTests
{
    [Test]
    public void GetInMemoryReferenceToStaticAssembly()
    {
        PortableExecutableReference? reference = ReferenceProvider.GetInMemoryReference(AssemblyProvider.StaticAssembly);

        Assert.That(reference, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(reference.FilePath, Is.Null);

            Assert.That(reference.Display, Is.EqualTo("<in-memory assembly>"));

            Assert.That(reference.Properties.Kind, Is.EqualTo(MetadataImageKind.Assembly));
        });
    }

    [Test]
    public void GetBestReferenceToStaticAssembly()
    {
        PortableExecutableReference? reference = ReferenceProvider.GetBestPEReference(AssemblyProvider.StaticAssembly);

        Assert.That(reference, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(reference.FilePath, Is.EqualTo(AssemblyProvider.StaticAssembly.Location));

            Assert.That(reference.Display, Is.EqualTo(AssemblyProvider.StaticAssembly.Location));

            Assert.That(reference.Properties.Kind, Is.EqualTo(MetadataImageKind.Assembly));
        });
    }

    [Test]
    public void GetInMemoryReferenceToDynamicAssembly()
    {
        PortableExecutableReference? reference = ReferenceProvider.GetInMemoryReference(AssemblyProvider.DynamicAssembly);

        Assert.That(reference, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(reference.FilePath, Is.Null);

            Assert.That(reference.Display, Is.EqualTo("<in-memory assembly>"));

            Assert.That(reference.Properties.Kind, Is.EqualTo(MetadataImageKind.Assembly));
        });
    }

    [Test]
    public void GetBestReferenceToDynamicAssembly()
    {
        PortableExecutableReference? reference = ReferenceProvider.GetBestPEReference(AssemblyProvider.DynamicAssembly);

        Assert.That(reference, Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(reference.FilePath, Is.Null);

            Assert.That(reference.Display, Is.EqualTo("<in-memory assembly>"));

            Assert.That(reference.Properties.Kind, Is.EqualTo(MetadataImageKind.Assembly));
        });
    }
}
