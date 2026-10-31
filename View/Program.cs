using View.Components;

var builder = WebApplication.CreateBuilder(args);

// רישום רכיבי Razor ורכיבי Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// טעינת כתובת ה-API מ-appsettings.json ורישום HttpClient עבור שכבת התצוגה
string apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl not found in appsettings.json.");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// רישום שירות UserSession לניהול מצב ההתחברות של המשתמש
builder.Services.AddScoped<View.Services.UserSession>();

var app = builder.Build();

// הגדרת טיפול בשגיאות ורשת מאובטחת בסביבת ייצור
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// הפניה לדף שגיאה מותאם במקרה של דף לא נמצא (404)
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();

// חיבור רכיב App כרכיב השורש של האפליקציה
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();