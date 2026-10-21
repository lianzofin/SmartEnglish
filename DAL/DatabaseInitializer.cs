using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    /// <summary>
    /// נוסף בגרסה 2, עודכן בגרסה 7 לגיבוב סיסמאות עם BCrypt.
    /// מחלקה שאחראית להכין את בסיס הנתונים לפני שהמערכת מתחילה לעבוד.
    /// היא נקראת פעם אחת, כשהשרת עולה, ויוצרת את בסיס הנתונים, את הטבלאות ואת נתוני הפתיחה.
    /// אפשר להריץ אותה שוב ושוב בלי נזק: בכל שלב נוצר רק מה שעוד לא קיים.
    /// המחלקה סטטית, כי אין לה מצב משלה ואין צורך ליצור ממנה עצמים.
    /// </summary>
    public static class DatabaseInitializer
    {
        // שם בסיס הנתונים מוגדר במקום אחד בלבד, כדי שכל הפעולות ישתמשו באותו שם
        private const string DatabaseName = "SmartEnglishDB";

        /// <summary>
        /// פעולת האתחול הראשית - נקראת בעת עליית האפליקציה
        /// </summary>
        /// <param name="serverConnectionString">מחרוזת חיבור לשרת (ללא ציון בסיס הנתונים)</param>
        public static async Task InitializeAsync(string serverConnectionString)
        {
            // שלב א': יצירת בסיס הנתונים אם אינו קיים
            await CreateDatabaseAsync(serverConnectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים
            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            // שלב ב': יצירת הטבלאות והתאמת עמודות חסרות
            await CreateTablesAsync(dbConnectionString);

            // שלב ג': הכנסת נתוני ברירת מחדל לטבלת התפקידים ולטבלת המשתמשים
            await SeedRolesAsync(dbConnectionString);
            await SeedUsersAsync(dbConnectionString);

            // שלב ד' (נוסף בגרסה 7): גיבוב סיסמאות ישנות שלא גובבו בעבר
            await HashOldPasswordsAsync(dbConnectionString);
        }

        /// <summary>
        /// יוצרת את בסיס הנתונים עצמו, אם הוא עוד לא קיים.
        /// </summary>
        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            // הקידוד שנבחר תומך בכל השפות, כולל עברית וסמלים מיוחדים,
            // וכלל המיון שנבחר משווה טקסט בלי להבדיל בין אותיות גדולות לקטנות
            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            // מתחברים לשרת בלי לציין בסיס נתונים, כי בשלב הזה הוא אולי עוד לא קיים.
            // החיבור והפקודה נסגרים אוטומטית בסוף הפעולה, גם אם נזרקה חריגה
            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            // הרצת פקודה שלא מחזירה נתונים, אלא רק משנה את מבנה השרת
            await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// יוצרת את שמונת הטבלאות של המערכת, אם הן עוד לא קיימות.
        /// </summary>
        private static async Task CreateTablesAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // יצירת 8 הטבלאות בהתאם למסמך תכנון בסיס הנתונים של SmartEnglish
            string query = @"
                -- תפקידים
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleID INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(30) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

                -- 1. משתמשים (כולל RoleID ו-IsActive)
                CREATE TABLE IF NOT EXISTS Users ( 
                    UserID INT AUTO_INCREMENT PRIMARY KEY, 
                    Username VARCHAR(50) NOT NULL UNIQUE, 
                    PasswordHash VARCHAR(255) NOT NULL, 
                    FullName VARCHAR(100) NOT NULL, 
                    Email VARCHAR(100) NOT NULL UNIQUE, 
                    RoleID INT NOT NULL DEFAULT 3,
                    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (RoleID) REFERENCES Roles(RoleID)
                ) ENGINE=InnoDB;

                -- 2. קורסים 
                CREATE TABLE IF NOT EXISTS Courses ( 
                    CourseID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseName VARCHAR(100) NOT NULL, 
                    TeacherID INT, 
                    Description TEXT, 
                    FOREIGN KEY (TeacherID) REFERENCES Users(UserID) ON DELETE SET NULL 
                ) ENGINE=InnoDB;

                -- 3. הרשמת תלמידים לקורסים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS UserCourses (
                    EnrollmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    CourseID INT NOT NULL, 
                    EnrollmentDate DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                -- 4. תרגילים 
                CREATE TABLE IF NOT EXISTS Exercises ( 
                    ExerciseID INT AUTO_INCREMENT PRIMARY KEY, 
                    Title VARCHAR(100) NOT NULL, 
                    Category VARCHAR(50) NOT NULL, 
                    DifficultyLevel INT NOT NULL, 
                    CreatedBy INT, 
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID) ON DELETE SET NULL 
                ) ENGINE=InnoDB;

                -- 5. שאלות בלומדה 
                CREATE TABLE IF NOT EXISTS Questions ( 
                    QuestionID INT AUTO_INCREMENT PRIMARY KEY, 
                    ExerciseID INT NOT NULL, 
                    QuestionText TEXT NOT NULL, 
                    CorrectAnswer VARCHAR(255) NOT NULL, 
                    WrongAnswer1 VARCHAR(255) NOT NULL, 
                    WrongAnswer2 VARCHAR(255), 
                    WrongAnswer3 VARCHAR(255), 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                -- 6. משימות (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS Assignments ( 
                    AssignmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    DueDate DATETIME NOT NULL, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                -- 7. התקדמות תלמידים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS StudentProgress ( 
                    ProgressID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    Score INT NOT NULL, 
                    CompletedAt DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                -- 8. מעקב נוכחות בשיעורים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS Attendance ( 
                    AttendanceID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseID INT NOT NULL, 
                    StudentID INT NOT NULL, 
                    LessonDate DATETIME NOT NULL, 
                    Status VARCHAR(20) NOT NULL, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;";

            using var command = new MySqlCommand(query, connection);
            await command.ExecuteNonQueryAsync();

            // דרך בטוחה ב-MySQL להוסיף את העמודה IsActive אם היא חסרה בטבלה ישנה
            try
            {
                string addColSql = "ALTER TABLE Users ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT TRUE;";
                using var alterCmd = new MySqlCommand(addColSql, connection);
                await alterCmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException)
            {
                // אם העמודה כבר קיימת, השגיאה תיקלט כאן והתוכנית תמשיך כרגיל
            }
        }

        /// <summary>
        /// מכניסה לטבלת התפקידים את שלושת התפקידים הקבועים של המערכת.
        /// </summary>
        private static async Task SeedRolesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם הטבלה ריקה לפני הכנסת נתוני ברירת המחדל
            string checkSql = "SELECT COUNT(*) FROM Roles;";
            using var checkCmd = new MySqlCommand(checkSql, connection);

            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                string insertRolesSql = @"
                    INSERT INTO Roles (RoleID, RoleName) VALUES
                    (1, 'Admin'),
                    (2, 'Teacher'),
                    (3, 'Student');
                ";
                using var insertCmd = new MySqlCommand(insertRolesSql, connection);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// עודכן בגרסה 7: מכניס לטבלת המשתמשים את המשתמשים ההתחלתיים עם סיסמאות מגובבות (BCrypt).
        /// </summary>
        private static async Task SeedUsersAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם הטבלה ריקה לפני הכנסת נתוני ברירת המחדל
            string checkSql = "SELECT COUNT(*) FROM Users;";
            using var checkCmd = new MySqlCommand(checkSql, connection);

            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
                // הכנת הסיסמאות המגובבות עבור משתמשי הברירת מחדל
                string adminHash = BCrypt.Net.BCrypt.HashPassword("admin");
                string teacherHash = BCrypt.Net.BCrypt.HashPassword("teacher123");
                string userHash = BCrypt.Net.BCrypt.HashPassword("user");

                string insertUsersSql = @"
                    INSERT INTO Users (Username, FullName, Email, PasswordHash, RoleID, IsActive) VALUES 
                    ('admin', 'System Admin', 'admin@smartenglish.com', @AdminHash, 1, TRUE),
                    ('sarah_t', 'Sarah Teacher', 'sarah@school.com', @TeacherHash, 2, TRUE),
                    ('user', 'Test Student', 'user@student.com', @UserHash, 3, TRUE);
                ";

                using var insertCmd = new MySqlCommand(insertUsersSql, connection);
                insertCmd.Parameters.AddWithValue("@AdminHash", adminHash);
                insertCmd.Parameters.AddWithValue("@TeacherHash", teacherHash);
                insertCmd.Parameters.AddWithValue("@UserHash", userHash);

                await insertCmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// נוסף בגרסה 7: מוצא סיסמאות ישנות בטבלה שאינן מגובבות (לא מתחילות ב-$2) ומגבב אותן.
        /// </summary>
        private static async Task HashOldPasswordsAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // שליפת המשתמשים שהסיסמה שלהם עדיין שמורה בטקסט רגיל (לא מתחילה ב-$2)
            string selectSql = "SELECT UserID, PasswordHash FROM Users WHERE PasswordHash NOT LIKE '$2%';";
            var usersToUpdate = new List<User>();

            using (MySqlCommand selectCmd = new MySqlCommand(selectSql, connection))
            using (var reader = await selectCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    User user = new User
                    {
                        Id = reader.GetInt32(0),
                        Password = reader.GetString(1)
                    };
                    usersToUpdate.Add(user);
                }
            }

            // עדכון כל סיסמה ישנה לגיבוב חדש
            foreach (User user in usersToUpdate)
            {
                string updateSql = "UPDATE Users SET PasswordHash = @Hash WHERE UserID = @UserID;";
                using MySqlCommand updateCmd = new MySqlCommand(updateSql, connection);
                updateCmd.Parameters.AddWithValue("@Hash", BCrypt.Net.BCrypt.HashPassword(user.Password));
                updateCmd.Parameters.AddWithValue("@UserID", user.Id);
                await updateCmd.ExecuteNonQueryAsync();
            }
        }
    }
}