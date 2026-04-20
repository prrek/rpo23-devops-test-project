using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TodoApp.Web.Controllers;
using Xunit;

namespace TodoApp.Web.Tests;

public class TodoApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public TodoApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenDataIsValid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("test@example.com", "Password123!"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "login" + Guid.NewGuid() + "@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "Password123!"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTodo_ReturnsOk_WhenAuthenticated()
    {
        // Arrange
        var email = "todo" + Guid.NewGuid() + "@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginResult!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/todos", new CreateTodoRequest("Test Todo", "Description"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTodos_ReturnsList_WhenAuthenticated()
    {
        // Arrange
        var email = "get" + Guid.NewGuid() + "@example.com";
        await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(email, "Password123!"));
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Password123!"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginResult!.Token;
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/v1/todos", new CreateTodoRequest("Test Todo", "Description"));

        // Act
        var response = await _client.GetAsync("/api/v1/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<System.Collections.Generic.IEnumerable<TodoApp.Application.DTOs.TodoDto>>();
        todos.Should().NotBeEmpty();
    }

    public record LoginResponse(string Token);
    
    [Fact]
    public async Task Register_ReturnsBadRequest_WhenPasswordIsEmpty()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("test@example.com", ""));
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodos_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        
        var response = await _client.GetAsync("/api/v1/todos");
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
