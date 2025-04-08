using System.Text;
using Emne9_Prosjekt.Components;
using Emne9_Prosjekt.Extensions;
using Emne9_Prosjekt.Hubs;
using Emne9_Prosjekt.Services;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Mappers;
using Emne9_Prosjekt.Features.Members.Models;
using Emne9_Prosjekt.GameComponents;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:80/api") });

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSignalRHubConnection("/gameHub");
builder.Services.AddSingleton<ChatService>();
builder.Services.AddSingleton<GameService>();

builder.Services.AddScoped<BattleShipComponents>();
// CHANGES
builder.Services.AddSingleton<Connect4Components>();

builder.Services.AddControllers();

builder.Services
    .AddEndpointsApiExplorer()
    .AddHttpContextAccessor();

builder.Services.AddSwaggerGen();

builder.Services
    .AddScoped<IMemberService, MemberService>()
    .AddScoped<IMemberRepository, MemberRepository>()
    .AddScoped<IMapper<Member, MemberDTO>, MemberMapper>()
    .AddScoped<IMapper<Member, MemberRegistrationDTO>, MemberRegistrationMapper>();

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
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new
                SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"]))
        };
        
    })
    .AddGoogle(options =>
    {
        options.ClientId = "525416754804-5sjmgl3kc3e2q8s4s8dgvv6dajd53m7s.apps.googleusercontent.com";
        options.ClientSecret = "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";
    });


builder.Services.AddAuthorization();

builder.Services.AddSwaggerWithJwtBearerAuthentication();


var app = builder.Build();


// Configure the HTTP request pipeline.
//app.UseHttpsRedirection();

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
//app.UseHttpsRedirection();

app.UseStaticFiles();
//app.UseRouting();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<ChatHub>("/chatHub"); 
app.MapHub<GameHub>("/gameHub");



app.MapControllers();

app.Run();

public partial class Program { }