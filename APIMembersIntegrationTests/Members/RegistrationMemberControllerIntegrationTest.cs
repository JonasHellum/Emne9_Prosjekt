using System.Net;
using System.Net.Http.Json;
using Emne9_Prosjekt.Features.Members.Models;
using Moq;
using Xunit.Abstractions;

namespace APIMembersIntegrationTests.Members;

public class RegistrationMemberControllerIntegrationTest : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _testOutputHelper;

    public RegistrationMemberControllerIntegrationTest(CustomWebApplicationFactory factory, ITestOutputHelper testOutputHelper)
    {
        _factory = factory;
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
    }
    [Fact]
    public async Task RegisterMemberAsync_WithValidInput_ReturnsOkWithMemberDTO()
    {
        // Arrange
        var registrationDto = new MemberRegistrationDTO
        {
            FirstName = "Bjarne",
            LastName = "Betjent",
            UserName = "BjarneBetjent123",
            Email = "test@example.com",
            Password = "StrongPassword1!"
        };
        
        _factory.MemberRepositoryMock
            .Setup(repo => repo.AddAsync(It.IsAny<Member>()))
            .ReturnsAsync((Member m) => m);

        // Act
        var response = await _client.PostAsJsonAsync("/api/Members/Register", registrationDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // 200 OK
        var member = await response.Content.ReadFromJsonAsync<MemberDTO>();
        Assert.NotNull(member);
        Assert.Equal("BjarneBetjent123", member.UserName);
    }
    
    [Fact]
    public async Task RegisterMemberAsync_WithInvalidInput_ReturnsBadRequest_WithValidationErrors()
    {
        // Arrange – vi setter opp en ugyldig MemberRegistrationDTO med en tom e-post og et svakt passord
        var registrationDto = new MemberRegistrationDTO
        {
            FirstName = "IkkeBjarne",
            LastName = "IkkeBetjent",
            UserName = "IkkeBjarneBetjent456",
            Email = "", // tom e-post som da skal feile validering
            Password = "abc" // svakt passord (for kort, mangler tall etc etc)
        };

        // Act – Sender POST-forespørselen til controlleren med en kjip ugyldig DTO
        var response = await _client.PostAsJsonAsync("/api/Members/Register", registrationDto);

        // Assert – først sjekker vi at vi får en 400 Bad Request som svar
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Leser inn svaret som en streng for feilsøking (kan hjelpe med debugging - jeg liker informasjoooon)
        var content = await response.Content.ReadAsStringAsync();
        _testOutputHelper.WriteLine("Validation error response: " + content);

        // Deserialiser svaret til et objekt som inneholder feilmeldingene
        var result = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();

        // Bekreft at feilene er tilstede i svaret (dvs. at de ble returnert fra validering)
        // Bekreft at resultatet ikke er null
        Assert.NotNull(result);

        Assert.True(result.Errors.ContainsKey("Email"));
        Assert.Contains(result.Errors["Email"], error => error == "Email is required");
        Assert.Contains(result.Errors["Email"], error => error == "Email is not valid");

        // Bekreft at det finnes feil for passordfeltet
        Assert.True(result.Errors.ContainsKey("Password"));
        Assert.Contains(result.Errors["Password"], error => error == "Password must be between 8 and 32 characters");
        Assert.Contains(result.Errors["Password"], error => error == "Invalid password, need at least 1 number");
        Assert.Contains(result.Errors["Password"], error => error == "Invalid password, need at least 1 capital letter");
    }

}