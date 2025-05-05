using System.Net;
using System.Security.Claims;
using System.Text;
using BitzArt.Blazor.Cookies;
using Emne9_Prosjekt.Components;
using Emne9_Prosjekt.Components.Pages.Interfaces;
using Emne9_Prosjekt.Components.Pages.Services;
using Emne9_Prosjekt.Extensions;
using Emne9_Prosjekt.Hubs;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards;
using Emne9_Prosjekt.Features.Leaderboards.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Mappers;
using Emne9_Prosjekt.Features.Leaderboards.Models;
using Emne9_Prosjekt.Features.Members;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Mappers;
using Emne9_Prosjekt.Features.Members.Models;
using Emne9_Prosjekt.GameComponents;
using Emne9_Prosjekt.Hubs.HubServices;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;
using Emne9_Prosjekt.Middleware;
using Emne9_Prosjekt.Validators.Interfaces;
using Emne9_Prosjekt.Validators.MemberValidators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Cookie = System.Net.Cookie;
using HttpVersion = System.Net.HttpVersion;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:44388/api") });

builder.Services.AddScoped(sp =>
{
    var handler = new HttpClientHandler
    {
        UseCookies = true, // Ensures cookies are stored and sent
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = true  
    };

    return new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost:80")
    };
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<ICustomAuthenticationStateProvider, CustomAuthenticationStateProvider>();

builder.Services.AddSignalR();
builder.Services.AddSignalRHubs();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<IForumService, ForumService>();
builder.Services.AddSingleton<IConnectFourGameService, ConnectFourGameService>();
builder.Services.RegisterMappers();
builder.Services.RegisterRepositories();

builder.Services.AddScoped<BattleShipComponents>();
builder.Services.AddScoped<Connect4Components>();

builder.Services
    .AddScoped<IMemberService, MemberService>();
   /* .AddScoped<IMemberRepository, MemberRepository>() */
   /* .AddScoped<IMapper<Member, MemberDTO>, MemberMapper>() */
  /*  .AddScoped<IMapper<Member, MemberRegistrationDTO>, MemberRegistrationMapper>();*/

  builder.Services
      .AddScoped<ILeaderboardService, LeaderboardService>();
   /* .AddScoped<ILeaderboardRepository, LeaderboardRepository>() */
  /*  .AddScoped<IMapper<Leaderboard, LeaderboardDTO>, LeaderboardMapper>()
    .AddScoped<IMapper<Leaderboard, LeaderboardAddOrUpdateDTO>, LeaderboardAddMapper>();*/

builder.Services.AddScoped<IAuthStateService, AuthStateService>();

builder.Services.AddControllers();

builder.Services
    .AddEndpointsApiExplorer();

// EXPERIMENTING A LOT
builder.Services.AddHttpContextAccessor();


builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.PropertyNamingPolicy = null;
    });

builder.AddBlazorCookies();

builder.Services.AddSwaggerGen();

builder.Services
    .AddValidatorsFromAssemblyContaining<Program>(
        lifetime: ServiceLifetime.Scoped,
        filter: filterType => filterType.ValidatorType != typeof(AsyncMemberRegistrationDTOValidator) &&
                              filterType.ValidatorType != typeof(AsyncMemberUpdateDTOValidator)
    )
    .AddFluentValidationAutoValidation(config =>
        config.DisableDataAnnotationsValidation = true);


builder.Services.AddScoped<IAsyncMemberRegistrationValidator, AsyncMemberRegistrationDTOValidator>();
builder.Services.AddScoped<IAsyncMemberUpdateValidator, AsyncMemberUpdateDTOValidator>();


builder.Services.AddDbContext<Emne9EksamenDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 33))));

builder.Services.AddScoped<JwtMiddleware>();


builder.Host.UseSerilog((context, configuration) => 
{
    configuration.ReadFrom.Configuration(context.Configuration);
});


builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; 
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; 
    })
    .AddJwtBearer()
    .AddGoogle(options =>
    {
        options.ClientId = "525416754804-5sjmgl3kc3e2q8s4s8dgvv6dajd53m7s.apps.googleusercontent.com";
        options.ClientSecret = "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";
    });


builder.Services.AddAuthorization();

builder.Services.AddSwaggerWithJwtBearerAuthentication();


var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


app.UseHttpsRedirection();

app.UseRouting();
app.UseMiddleware<JwtMiddleware>()
    .UseMiddleware<ApiExceptionHandling>();
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings = { [".glb"] = "model/gltf-binary" }
    }
});

app.UseStaticFiles();
app.UseRouting();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<ChatHub>("/chatHub"); 
app.MapHub<GameHub>("/gameHub");
app.MapHub<ForumHub>("/forumHub");
app.MapHub<BigChatHub>("/bigchathub");
app.MapHub<ConnectFourGameHub>("/connectgamehub");


app.MapControllers();

app.Run();

public partial class Program { }