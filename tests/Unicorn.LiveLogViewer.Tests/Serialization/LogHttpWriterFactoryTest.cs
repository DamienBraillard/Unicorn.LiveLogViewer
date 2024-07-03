using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Unicorn.LiveLogViewer.Serialization;
using Unicorn.LiveLogViewer.Sources;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Serialization;

public class LogHttpWriterFactoryTest
{
    private readonly LogHttpWriterFactory _target = new();

    [Fact]
    public void Create_ValidArgument_ReturnsALogHttpWriter()
    {
        // Arrange

        // Act
        var writer = _target.Create(Stream.Null);

        // Assert
        writer.Should().BeOfType<LogHttpWriter>();
    }

    [Fact]
    public async Task Create_ValidArgument_ReturnsAWriterThatWritesToTheSpecifiedStream()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var writer = _target.Create(stream);

        // Assert
        await writer.WriteAsync([new LogEvent()], default);
        stream.Should().NotHaveLength(0);
    }
}