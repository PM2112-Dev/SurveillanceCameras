// using System.Net.Http;
// using System.Threading.Tasks;
// using NUnit.Framework;
//
// namespace SurveillanceCameras.Infrastructure.IntegrationTests.Service.WikiDichApiService;
//
// [TestFixture]
// public class WikiDichApiServiceIntegrationTests
// {
//     [Test]
//     public async Task FetchStory_RealPage_ReturnsData()
//     {
//         // Arrange
//         var httpClient = new HttpClient();
//         var service = new Infrastructure.Service.WikiDichService.WikiDichApiService(httpClient);
//
//         // Dùng một link thật, ví dụ một truyện phổ biến trên wikidich.vn
//         var url = "https://wikidich.vn/huyen-huyen-thien-lao-ba-nam-cai-kia-an-choi-trac-tang-ra-tu";
//
//         // Act
//         var result = await service.FetchStory(url);
//         
//         TestContext.Out.WriteLine($"Title: {result.Title}");
//         TestContext.Out.WriteLine($"Author: {result.Author}");
//         TestContext.Out.WriteLine($"TotalChapters: {result.TotalChapters}");
//         TestContext.Out.WriteLine($"Description: {result.Description}");
//         TestContext.Out.WriteLine($"ImageUrl: {result.ImageUrl}");
//         TestContext.Out.WriteLine($"Genres: {string.Join(", ", result.Genres ?? new List<string>())}");
//         // Assert
//         Assert.NotNull(result);
//         Assert.False(string.IsNullOrWhiteSpace(result.Title));
//         Assert.False(string.IsNullOrWhiteSpace(result.Author));
//         Assert.False(string.IsNullOrWhiteSpace(result.Description));
//         Assert.False(string.IsNullOrWhiteSpace(result.ImageUrl));
//         Assert.NotNull(result.Genres);
//     }
// }
