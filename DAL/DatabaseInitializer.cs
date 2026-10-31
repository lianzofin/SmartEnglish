using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Model;
using MySql.Data.MySqlClient;

namespace DAL
{
    // מחלקה לאתחול בסיס הנתונים בעת עליית השרת
    public static class DatabaseInitializer
    {
        private const string DatabaseName = "SmartEnglishDB";

        // הפעולה הראשית לאתחול הנתונים והטבלאות
        public static async Task InitializeAsync(string serverConnectionString)
        {
            await CreateDatabaseAsync(serverConnectionString);

            string dbConnectionString = $"{serverConnectionString}Database={DatabaseName};";

            await CreateTablesAsync(dbConnectionString);
            await SeedRolesAsync(dbConnectionString);
            await SeedUsersAsync(dbConnectionString);
            await HashOldPasswordsAsync(dbConnectionString);
        }

        // יצירת בסיס הנתונים במידה ואינו קיים
        private static async Task CreateDatabaseAsync(string serverConnectionString)
        {
            string sql = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";

            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync();
            using var command = new MySqlCommand(sql, connection);

            await command.ExecuteNonQueryAsync();
        }

        // יצירת הטבלאות הנדרשות לפרויקט
        private static async Task CreateTablesAsync(string connectionString)
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            string query = @"
                CREATE TABLE IF NOT EXISTS Roles (
                    RoleID INT AUTO_INCREMENT PRIMARY KEY,
                    RoleName VARCHAR(30) NOT NULL UNIQUE
                ) ENGINE=InnoDB;

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

                CREATE TABLE IF NOT EXISTS Courses ( 
                    CourseID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseName VARCHAR(100) NOT NULL, 
                    TeacherID INT, 
                    Description TEXT, 
                    FOREIGN KEY (TeacherID) REFERENCES Users(UserID) ON DELETE SET NULL 
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS UserCourses (
                    EnrollmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    CourseID INT NOT NULL, 
                    EnrollmentDate DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS Exercises ( 
                    ExerciseID INT AUTO_INCREMENT PRIMARY KEY, 
                    Title VARCHAR(100) NOT NULL, 
                    Category VARCHAR(50) NOT NULL, 
                    DifficultyLevel INT NOT NULL, 
                    CreatedBy INT, 
                    FOREIGN KEY (CreatedBy) REFERENCES Users(UserID) ON DELETE SET NULL 
                ) ENGINE=InnoDB;

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

                CREATE TABLE IF NOT EXISTS Assignments ( 
                    AssignmentID INT AUTO_INCREMENT PRIMARY KEY, 
                    CourseID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    DueDate DATETIME NOT NULL, 
                    FOREIGN KEY (CourseID) REFERENCES Courses(CourseID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

                CREATE TABLE IF NOT EXISTS StudentProgress ( 
                    ProgressID INT AUTO_INCREMENT PRIMARY KEY, 
                    StudentID INT NOT NULL, 
                    ExerciseID INT NOT NULL, 
                    Score INT NOT NULL, 
                    CompletedAt DATETIME NOT NULL, 
                    FOREIGN KEY (StudentID) REFERENCES Users(UserID) ON DELETE CASCADE, 
                    FOREIGN KEY (ExerciseID) REFERENCES Exercises(ExerciseID) ON DELETE CASCADE 
                ) ENGINE=InnoDB;

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

            try
            {
                string addColSql = "ALTER TABLE Users ADD COLUMN IsActive BOOLEAN NOT NULL DEFAULT TRUE;";
                using var alterCmd = new MySqlCommand(addColSql, connection);
                await alterCmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException)
            {
                // עמודת IsActive כבר קיימת
            }
        }

        // הכנסת תפקידי ברירת מחדל
        private static async Task SeedRolesAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

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

        // הכנסת משתמשים ראשוניים עם סיסמאות מגובבות
        private static async Task SeedUsersAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

            string checkSql = "SELECT COUNT(*) FROM Users;";
            using var checkCmd = new MySqlCommand(checkSql, connection);

            long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());

            if (count == 0)
            {
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

        // גיבוב סיסמאות ישנות במידה ואינן מגובבות
        private static async Task HashOldPasswordsAsync(string dbConnectionString)
        {
            using var connection = new MySqlConnection(dbConnectionString);
            await connection.OpenAsync();

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