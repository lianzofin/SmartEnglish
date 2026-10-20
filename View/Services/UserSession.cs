using Model;

namespace View.Services
{
    public class UserSession
    {
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string FullName { get; private set; } = string.Empty; // ◄ תכונה חדשה
        public string Email { get; private set; } = string.Empty;
        public int RoleId { get; private set; }
        public string RoleName { get; private set; } = string.Empty;

        public bool IsLoggedIn => UserId > 0;

        public event Action? OnChange;

        public void Login(User user)
        {
            UserId = user.Id;
            Username = user.Username;
            // אם במודל User שלך יש FullName השתמשי בו, או שרשרי FirstName ו-LastName
            FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Username;
            Email = user.Email;
            RoleId = user.RoleId;
            RoleName = user.RoleName;

            OnChange?.Invoke();
        }

        public void Logout()
        {
            UserId = 0;
            Username = string.Empty;
            FullName = string.Empty;
            Email = string.Empty;
            RoleId = 0;
            RoleName = string.Empty;

            OnChange?.Invoke();
        }
    }
}