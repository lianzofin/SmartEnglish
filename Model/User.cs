using System;

// נוסף בגרסה 3.
// מחלקה שמייצגת משתמש במערכת: שורה אחת בטבלת המשתמשים.
// העצמים של המחלקה עוברים בין כל השכבות: שכבת הגישה לנתונים יוצרת אותם,
// שכבת השירות שולחת אותם ללקוח, והתצוגה מציגה אותם.
namespace Model
{
    // המשתמש יורש ממחלקת הבסיס, וכך מקבל ממנה את המזהה
    public class User : BaseEntity
    {
        // שם המשתמש, ייחודי בכל המערכת
        public string Username { get; set; } = string.Empty;

        // הדואר האלקטרוני, גם הוא ייחודי
        public string Email { get; set; } = string.Empty;

        // הסיסמה. בשלב הזה של הפרויקט היא נשמרת כמו שהיא, בלי הצפנה
        public string Password { get; set; } = string.Empty;

        // השם המלא של המשתמש
        public string FullName { get; set; } = string.Empty;

        // המזהה של התפקיד, מפתח זר לטבלת התפקידים
        public int RoleId { get; set; }

        // שם התפקיד, נשלף מטבלת התפקידים בעזרת צירוף טבלאות
        public string RoleName { get; set; } = string.Empty;

        // סימון אם המשתמש פעיל במערכת
        public bool IsActive { get; set; } = true;

        // מועד ההרשמה. בסיס הנתונים ממלא אותו לבד בזמן ההוספה
        public DateTime CreatedAt { get; set; }

        // בנאי ריק. השרת משתמש בו כשהוא ממיר אוטומטית את הנתונים שהגיעו בבקשה לעצם משתמש
        public User() { }

        // משתמש שעדיין לא נשמר בבסיס הנתונים (אין לו מזהה)
        public User(string username, string email, string password, string fullName)
        {
            Username = username;
            Email = email;
            Password = password;
            FullName = fullName;
        }

        // משתמש שנשלף מבסיס הנתונים.
        // המזהה מועבר לבנאי של מחלקת הבסיס, ושאר השדות נשמרים כאן
        public User(int id, string username, string email, string password, string fullName,
                    int roleId, string roleName, bool isActive, DateTime createdAt) : base(id)
        {
            Username = username;
            Email = email;
            Password = password;
            FullName = fullName;
            RoleId = roleId;
            RoleName = roleName;
            IsActive = isActive;
            CreatedAt = createdAt;
        }

        // דריסה של הפעולה שמחזירה תיאור טקסטואלי של העצם: שם המשתמש והתפקיד שלו
        public override string ToString() => $"{Username} ({RoleName})";
    }
}