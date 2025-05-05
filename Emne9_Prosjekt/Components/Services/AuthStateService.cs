using Emne9_Prosjekt.Components.Pages.Interfaces;

namespace Emne9_Prosjekt.Components.Pages.Services;

public class AuthStateService : IAuthStateService
{
    // State for whether the user is logged in
    private string? _userName; 
    public string? UserName
    {
        get => _userName;
        private set
        {
            _userName = value;
            NotifyStateChanged();
        }
    }

    public bool IsLoggedIn => !string.IsNullOrEmpty(UserName);
    
    // Event to notify when state changes
    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    // Public method to update the username and notify components
    public void SetUserName(string userName)
    {
        UserName = userName;
    }

    // Clear the state, e.g., for logout
    public void ClearUserName()
    {
        UserName = null;
    }

}