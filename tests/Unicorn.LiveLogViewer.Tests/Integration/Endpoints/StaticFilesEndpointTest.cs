using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Unicorn.LiveLogViewer.StaticContent;
using Unicorn.LiveLogViewer.Tests.Helpers;
using Unicorn.LiveLogViewer.Tests.Helpers.AspNet;
using Xunit;
using Xunit.Abstractions;

namespace Unicorn.LiveLogViewer.Tests.Integration.Endpoints;

public class StaticFilesEndpointTest : IDisposable
{
    private readonly TestServerBuilder _serverBuilder;

    public StaticFilesEndpointTest(ITestOutputHelper outputHelper)
    {
        _serverBuilder = new TestServerBuilder(outputHelper);
    }

    public void Dispose()
    {
        _serverBuilder.Dispose();
    }

    [Theory]
    [CombinatorialData]
    public async Task UseLiveLogViewer_AllDefaultStaticFiles_ServesTheStaticFile(
        bool useCustomBasePath,
        [CombinatorialMemberData(nameof(StaticFileTestData.MemberData), MemberType = typeof(StaticFileTestData))]
        StaticFileTestData file)
    {
        // Arrange
        using var server = _serverBuilder
            .SetupWebApplicationBuilder(builder => builder.Services.AddLiveLogViewer())
            .SetupWebApplication(app => _ = useCustomBasePath ? app.MapLiveLogViewer(basePath: "/my-test-path") : app.MapLiveLogViewer())
            .StartServer(LogLevel.Debug);

        var requestPath = $"{(useCustomBasePath ? "/my-test-path" : "/logViewer")}/{file}";

        var client = server.CreateClient();

        // Act
        var response = await client.GetAsync(requestPath);

        // Assert
        response.Should().Be200Ok();
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Be(file.Content);
    }

    #region Test Data

    public class StaticFileTestData(string name, string content)
    {
        static StaticFileTestData()
        {
            var fileNames = typeof(StaticFileNames).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => f.GetValue(null)).Cast<string>();
            var type = typeof(StaticFileNames);

            var items = new List<StaticFileTestData>();
            foreach (var fileName in fileNames)
            {
                var resourceFullName = $"{type.Namespace}.{fileName}";
                var content = type.Assembly.GetManifestResourceStream(resourceFullName)?.ToUtf8String();
                if (content == null)
                    throw new MissingManifestResourceException($"Resource for file '{fileName}' not found: '{resourceFullName}'");
                items.Add(new StaticFileTestData(fileName, content));
            }

            MemberData = items;
        }

        public static IEnumerable<StaticFileTestData> MemberData { get; }

        public string Name { get; } = name;
        public string Content { get; } = content;

        public override string ToString() => Name;
    }

    #endregion
}