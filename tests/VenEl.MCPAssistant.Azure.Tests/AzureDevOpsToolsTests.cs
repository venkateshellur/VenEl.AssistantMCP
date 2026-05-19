using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VenEl.MCPAssistant.Azure.Services;
using VenEl.MCPAssistant.Azure.Tools;
using Xunit;

namespace VenEl.MCPAssistant.Azure.Tests;

public class AzureDevOpsToolsTests
{
    [Fact]
    public async Task AzureListProjectsAsync_CallsHttpClientWithCorrectUrl()
    {
        // Arrange
        var mockHttpClient = new Mock<IAzureHttpClient>();
        var expectedResponse = "{\"count\": 1, \"value\": [{\"name\": \"MockProject\"}]}";
        
        mockHttpClient
            .Setup(c => c.GetAsync(AzureProduct.DevOps, "_apis/projects?$top=50", "7.1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var logger = NullLogger<AzureListProjectsActionHandler>.Instance;
        var handler = new AzureListProjectsActionHandler(mockHttpClient.Object, logger);
        var args = new AzureCommandArgs { Action = "azure_list_projects", Top = 50 };

        // Act
        var result = await handler.HandleAsync(args, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockHttpClient.Verify(c => c.GetAsync(AzureProduct.DevOps, "_apis/projects?$top=50", "7.1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
