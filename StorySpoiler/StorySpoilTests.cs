using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using StorySpoil.Tests.Models;

namespace StorySpoil.Tests
{
    [TestFixture]
    public class StorySpoilTests
    {
        private RestClient client;
        private static string createdStoryId = string.Empty;

        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("kristiandechev93", "kristiandechev93");

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string userName, string password)
        {
            var loginClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post)
                .AddJsonBody(new { userName, password });

            var response = loginClient.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString()!;
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreatedAndId()
        {
            var body = new StoryDTO
            {
                Title = "My New Story",
                Description = "Demo description",
                Url = ""
            };

            var req = new RestRequest("/api/Story/Create", Method.Post)
                .AddJsonBody(body);
            var resp = client.Execute(req);

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var api = JsonSerializer.Deserialize<ApiResponseDTO>(resp.Content);
            Assert.That(api?.StoryId, Is.Not.Null.And.Not.Empty);
            Assert.That(api?.Msg, Is.EqualTo("Successfully created!"));

            createdStoryId = api!.StoryId!;
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnOkAndMessage()
        {
            Assert.That(createdStoryId, Is.Not.Empty);

            var body = new StoryDTO
            {
                Title = "Edited Title",
                Description = "Edited Description",
                Url = ""
            };

            var req = new RestRequest($"/api/Story/Edit/{createdStoryId}", Method.Put)
                .AddJsonBody(body);
            var resp = client.Execute(req);

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var api = JsonSerializer.Deserialize<ApiResponseDTO>(resp.Content);
            Assert.That(api?.Msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnNonEmptyArray()
        {
            var resp = client.Execute(new RestRequest("/api/Story/All", Method.Get));

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(resp.Content);
            Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(json.GetArrayLength(), Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOkAndMessage()
        {
            Assert.That(createdStoryId, Is.Not.Empty);

            var resp = client.Execute(new RestRequest($"/api/Story/Delete/{createdStoryId}", Method.Delete));

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var api = JsonSerializer.Deserialize<ApiResponseDTO>(resp.Content);
            Assert.That(api?.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var body = new StoryDTO { Title = "", Description = "" };

            var resp = client.Execute(new RestRequest("/api/Story/Create", Method.Post)
                .AddJsonBody(body));

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnBadRequest()
        {
            var nonExistingId = Guid.NewGuid().ToString();

            var body = new StoryDTO { Title = "X", Description = "Y", Url = "" };

            var resp = client.Execute(new RestRequest($"/api/Story/Edit/{nonExistingId}", Method.Put)
                .AddJsonBody(body));

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequestAndMsg()
        {
            var resp = client.Execute(new RestRequest("/api/Story/Delete/123", Method.Delete));

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var api = JsonSerializer.Deserialize<ApiResponseDTO>(resp.Content);
            Assert.That(api?.Msg, Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            client?.Dispose();
        }
    }
}
