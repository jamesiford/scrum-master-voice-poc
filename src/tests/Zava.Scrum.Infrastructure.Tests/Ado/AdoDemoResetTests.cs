using System.Net;
using System.Text;
using Zava.Scrum.Infrastructure.Ado;

namespace Zava.Scrum.Infrastructure.Tests.Ado;

public sealed class AdoDemoResetTests
{
    [Fact]
    public async Task RemoveAgentCommentsAsync_DeletesOnlyCommentsWithTheApprovalStamp()
    {
        var handler = new FakeAdo();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://dev.azure.com/org/") };

        var deleted = await new AdoDemoReset(http).RemoveAgentCommentsAsync("Demo", CancellationToken.None);

        Assert.Equal(1, deleted);
        Assert.Equal(["/org/Demo/_apis/wit/workItems/7/comments/11"], handler.Deleted);
    }

    private sealed class FakeAdo : HttpMessageHandler
    {
        public List<string> Deleted { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Delete)
            {
                Deleted.Add(path);
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
            }

            var json = request.Method == HttpMethod.Post
                ? """{"workItems":[{"id":7}]}"""
                : """{"comments":[{"id":11,"text":"Done.<br><i>Approved by Sam in Teams, proposed by Zava Scrum Agent.</i>"},{"id":12,"text":"A teammate's own note"}]}""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }
}
