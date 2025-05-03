using Microsoft.AspNetCore.Authentication.Cookies;

namespace Emne9_Prosjekt.Components.Pages.Interfaces;

public interface ICookieSettingService
{
    Task ProcessApiResponse(HttpResponseMessage response);
}