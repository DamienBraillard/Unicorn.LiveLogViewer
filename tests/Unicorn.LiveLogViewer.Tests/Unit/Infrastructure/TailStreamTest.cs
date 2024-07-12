using System;
using System.IO;
using FluentAssertions;
using NSubstitute;
using Unicorn.LiveLogViewer.Infrastructure;
using Xunit;

namespace Unicorn.LiveLogViewer.Tests.Unit.Infrastructure;

public class TailStreamTest
{
    [Fact]
    public void Constructor_InnerStreamIsNull_Throws()
    {
        // Arrange

        // Act
        var action = () => new TailStream(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentNullException>().WithParameterName("innerStream");
    }

    [Fact]
    public void Constructor_InnerStreamIsNotReadable_Throws()
    {
        // Arrange
        var innerStream = Substitute.For<Stream>();
        innerStream.CanRead.Returns(false);
        innerStream.CanWrite.Returns(true);
        innerStream.CanSeek.Returns(true);
        innerStream.CanTimeout.Returns(true);

        // Act
        var action = () => new TailStream(null!);

        // Assert
        action.Should().ThrowExactly<ArgumentException>().WithParameterName("innerStream").WithMessage("*readable*");
    }
}