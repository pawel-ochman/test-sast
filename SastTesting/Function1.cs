using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;

namespace SastTesting
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        [FunctionName("GetUserById")]
        public static async Task<IActionResult> GetUserById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "users/{id}")] HttpRequest req,
            string id,
            ILogger log)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = $"SELECT * FROM Users WHERE Id = '{id}'";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    var user = new User
                                    {
                                        Id = reader["Id"].ToString(),
                                        Name = reader["Name"].ToString(),
                                        Email = reader["Email"].ToString(),
                                        Password = reader["Password"].ToString(),
                                        Phone = reader["Phone"].ToString()
                                    };
                                    return new OkObjectResult(user);
                                }
                            }
                        }
                    }
                }
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error getting user by id");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private static readonly HttpClient client = new HttpClient();

        [FunctionName("GetWeather")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/{city}")] HttpRequest req,
            string city,
            ILogger log)
        {
            var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={Environment.GetEnvironmentVariable("WeatherApiKey")}";
            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var result = await response.Content.ReadAsStringAsync();
            return new OkObjectResult(result);
        }
    }

    public class User
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
    }
}
