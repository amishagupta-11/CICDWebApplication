using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class webAuthenticationController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public webAuthenticationController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Displays the form with a textbox for user input.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Sends data entered by the user to the external API and displays the result.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Synchronize(string inputData)
    {
        // First API URL
        string firstApiUrl = "https://devcaamobileapp1app1.azurewebsites.net/WebAuthentication/synchronize";

        // Second API URL (GET endpoint)
        string secondApiUrl = "https://devcaamobileapp1app1.azurewebsites.net/FBO/FBODetailsUI";

        // Create raw plain text content for the first API
        var jsonContent = new StringContent(inputData, Encoding.UTF8, "application/json");

        var httpClient = _httpClientFactory.CreateClient();

        try
        {
            // Call the first API
            var firstResponse = await httpClient.PostAsync(firstApiUrl, jsonContent);

            // Check the response status code
            if (firstResponse.IsSuccessStatusCode)
            {
                // Read the response from the first API
                string firstResponseContent = await firstResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"First API Response Content: {firstResponseContent}");

                // Extract the token from the first API's response
                string token = ExtractTokenFromResponse(firstResponseContent);

                if (string.IsNullOrEmpty(token))
                {
                    ViewBag.ErrorMessage = "Failed to extract token from the first API response.";
                    return View("Error");
                }

                // **Set the `wptoken` header for the second API call**
                httpClient.DefaultRequestHeaders.Add("wptoken", token);

                // Call the second API (GET request)
                var secondResponse = await httpClient.GetAsync(secondApiUrl);

                // Check the response status of the second API
                if (secondResponse.IsSuccessStatusCode)
                {
                    // Read the response from the second API
                    string secondResponseContent = await secondResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Second API Response Content: {secondResponseContent}");

                    // Pass the second API response to the view
                    ViewBag.Response = secondResponseContent;
                    return View("Result");
                }
                else
                {
                    // Handle error from the second API
                    string secondErrorContent = await secondResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Second API Error: {secondResponse.StatusCode}, Details: {secondErrorContent}");
                    ViewBag.ErrorMessage = $"Second API Error: {secondResponse.StatusCode}, Details: {secondErrorContent}";
                    return View("Error");
                }
            }
            else
            {
                // Handle error from the first API
                string firstErrorContent = await firstResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"First API Error: {firstResponse.StatusCode}, Details: {firstErrorContent}");
                ViewBag.ErrorMessage = $"First API Error: {firstResponse.StatusCode}, Details: {firstErrorContent}";
                return View("Error");
            }
        }
        catch (HttpRequestException ex)
        {
            // Handle exceptions during the request
            Console.WriteLine($"Request failed: {ex.Message}");
            ViewBag.ErrorMessage = "Request failed: " + ex.Message;
            return View("Error");
        }
    }



    private string ExtractTokenFromResponse(string responseContent)
    {
        try
        {
            using (var jsonDoc = JsonDocument.Parse(responseContent))
            {
                JsonElement root = jsonDoc.RootElement;

                // Get the token property
                if (root.TryGetProperty("token", out JsonElement tokenElement))
                {
                    return tokenElement.GetString();
                }
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Failed to parse token from response: {ex.Message}");
        }

        return null; // Return null if token extraction fails
    }

}
