using Emne9_Prosjekt.Features.Members.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace APIMembersIntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public CustomWebApplicationFactory()
    {
        MemberRepositoryMock = new Mock<IMemberRepository>();
    }
    public Mock<IMemberRepository> MemberRepositoryMock { get; set; }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(MemberRepositoryMock.Object);
        });
    }
}