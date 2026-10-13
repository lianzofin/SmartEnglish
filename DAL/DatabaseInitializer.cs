using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;

namespace DAL
{
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "SmartEnglishDB";

        public static async Task InitializeAsync(string serverConnectionString)
        {
            // שלב א': יצירת בסיס הנתונים אם אינו קיים
            await CreateDatabaseAsync(serverConnectionString);

            // מחרוזת חיבור מלאה הכוללת את שם בסיס הנתונים
            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            // שלב ב': יצירת הטבלאות
            await CreateTablesAsync(dbConnectionString);

            await SeedRolesAsync(dbConnectionString);
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

                -- 1. משתמשים
                CREATE TABLE IF NOT EXISTS Users ( 
                    UserID INT AUTO_INCREMENT PRIMARY KEY, 
                    Username VARCHAR(50) NOT NULL UNIQUE, 
                    PasswordHash VARCHAR(255) NOT NULL, 
                    FullName VARCHAR(100) NOT NULL, 
                    Email VARCHAR(100) NOT NULL UNIQUE, 
                    Role VARCHAR(20) NOT NULL
                );

                -- 2. קורסים 
                CREATE TABLE IF NOT EXISTS Courses ( 
                    CourseID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseName VARCHAR(100) NOT NULL, 
                    TeacherID INT, 
                    Description TEXT, 
                    FOREIGN KEY (TeacherID) REFERENCES Users(UserID) ON DELETE SET NULL 
                );

                -- 3. הרשמת תלמידים לקורסים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS UserCourses (
                    EnrollmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    CourseID INT NOT NULL, 
                    EnrollmentDate DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE 
                );

                -- 4. תרגילים 
                CREATE TABLE IF NOT EXISTS Exercises ( 
                    ExerciseID INT AUTO_INCREMENT PRIMARY KEY, 
                    Title VARCHAR(100) NOT NULL, 
                    Category VARCHAR(50) NOT NULL, 
                    DifficultyLevel INT NOT NULL, 
                    CreatedBy INT, 
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID) ON DELETE SET NULL 
                );

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
                );

                -- 6. משימות (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS Assignments ( 
                    AssignmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    DueDate DATETIME NOT NULL, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                );

                -- 7. התקדמות תלמידים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS StudentProgress ( 
                    ProgressID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    Score INT NOT NULL, 
                    CompletedAt DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                );

                -- 8. מעקב נוכחות בשיעורים (טבלת איחוד) 
                CREATE TABLE IF NOT EXISTS Attendance ( 
                    AttendanceID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseID INT NOT NULL, 
                    StudentID INT NOT NULL, 
                    LessonDate DATETIME NOT NULL, 
                    Status VARCHAR(20) NOT NULL, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE 
                );";

            using var command = new MySqlCommand(query, connection);
            await command.ExecuteNonQueryAsync();
        }


        private static async Task SeedRolesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            // בדיקה אם הטבלה ריקה לפני הכנסת נתוני ברירת המחדל
            string checkSql = "SELECT COUNT(*) FROM Roles;";
            using var checkCmd = new MySqlCommand(checkSql, connection);

            // הפקודה מחזירה ערך בודד, מספר השורות, ומשם ממירים אותו למספר שלם
            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            // מכניסים את התפקידים רק בהפעלה הראשונה.
            // בלי הבדיקה, כל עלייה של השרת הייתה מנסה להכניס אותם שוב
            if (count == 0)
            {
                string insertRolesSql = @"
                    INSERT INTO Roles (RoleName) VALUES
                    ('Admin'),
                    ('Teacher'),
                    ('Student');
                ";
                using var insertCmd = new MySqlCommand(insertRolesSql, connection);
                await insertCmd.ExecuteNonQueryAsync();
            }
        }

    }
}