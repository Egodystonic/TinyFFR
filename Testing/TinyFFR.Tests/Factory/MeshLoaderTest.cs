using Egodystonic.TinyFFR.Assets.Meshes;
using Egodystonic.TinyFFR.Factory.Local;

namespace Egodystonic.TinyFFR.Factory;

[TestFixture]
public class MeshLoaderTest
{
    private LocalTinyFfrFactory _factory;

    [SetUp]
    public void SetUpTest() { 
        _factory = new LocalTinyFfrFactory();
    }

    [TearDown]
    public void TearDownTest() {
        _factory.Dispose();
    }

    [Test]
    public void ShouldLoadModelFileFromByteArray()
    {
        using var fileStream = File.OpenRead(Path.Combine("..", "..","..","..","..","test_assets", "ELCrate.obj"));
        using var ms = new MemoryStream();
        fileStream.CopyTo(ms);
        var span = new ReadOnlySpan<byte>(ms.ToArray());
        
        var mesh = _factory.AssetLoader.LoadMesh(span, "obj", new MeshCreationConfig
        {
            Name = "crate"
        }, new MeshReadConfig());

        Assert.Multiple(() =>
        {
            Assert.That(mesh.BoundingBox.Width, Is.EqualTo(22.720339f).Within(0.001f));
            Assert.That(mesh.BoundingBox.Height, Is.EqualTo(22.720345f).Within(0.001f));
            Assert.That(mesh.BoundingBox.Depth, Is.EqualTo(22.720343f).Within(0.001f));
        });
    }
}