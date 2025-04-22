namespace Emne9_Prosjekt.Components.Pages.Interfaces;

public interface IAuthStateService
{
    string? UserName { get; }
    bool IsLoggedIn { get; }
    event Action? OnChange;

    void SetUserName(string userName);
    void ClearUserName();

}