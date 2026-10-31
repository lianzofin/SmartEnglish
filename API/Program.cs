namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // רישום הבקרים ותיעוד ה-OpenAPI
            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // טעינת מחרוזת החיבור מ-User Secrets ורישום UserDB כ-Singleton
            string dbConnectionString = (builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
                + "Database=SmartEnglishDB;";
            builder.Services.AddSingleton(new DAL.UserDB(dbConnectionString));

            // הגדרת מדיניות CORS המאפשרת גישה מכל מקור
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
            });

            var app = builder.Build();

            app.UseCors();

            // אתחול בסיס הנתונים והטבלאות לפני הפעלת השרת
            string serverConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

            // חשיפת תיעוד ה-API בסביבת פיתוח
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();
            app.MapControllers();

            // הרצת השרת
            await app.RunAsync();
        }
    }
}