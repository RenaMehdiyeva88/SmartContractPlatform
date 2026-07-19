using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SmartContractPlatform.BLL.Mapping;
using SmartContractPlatform.BLL.Services;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Data;
using SmartContractPlatform.DAL.Entities;
using SmartContractPlatform.DAL.Enums;
using SmartContractPlatform.DAL.Repositories;
using SmartContractPlatform.DAL.Repositories.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// MVC üçün Cookie-based authentication (JWT yox — brauzerdə session kifayətdir)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddAutoMapper(typeof(SmartContractProfile));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IContractService, ContractService>();
builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IPdfService, PdfService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await SeedDefaultTemplatesAsync(dbContext);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "root",
    pattern: "",
    defaults: new { controller = "Account", action = "Login" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();

static async Task SeedDefaultTemplatesAsync(ApplicationDbContext dbContext)
{
    if (await dbContext.Templates.AnyAsync())
    {
        return;
    }

    dbContext.Templates.AddRange(
        new Template { Name = "Rental Contract", Category = TemplateCategory.Rental, SchemaJson = "{\"type\":\"object\",\"properties\":{\"propertyAddress\":{\"type\":\"string\"},\"monthlyRent\":{\"type\":\"number\"},\"startDate\":{\"type\":\"string\",\"format\":\"date\"},\"endDate\":{\"type\":\"string\",\"format\":\"date\"}},\"required\":[\"propertyAddress\",\"monthlyRent\",\"startDate\"]}" },
        new Template { Name = "Service Contract", Category = TemplateCategory.Service, SchemaJson = "{\"type\":\"object\",\"properties\":{\"serviceDescription\":{\"type\":\"string\"},\"fee\":{\"type\":\"number\"},\"deadline\":{\"type\":\"string\",\"format\":\"date\"}},\"required\":[\"serviceDescription\",\"fee\"]}" },
        new Template { Name = "Sales Contract", Category = TemplateCategory.Sale, SchemaJson = "{\"type\":\"object\",\"properties\":{\"itemDescription\":{\"type\":\"string\"},\"price\":{\"type\":\"number\"},\"deliveryDate\":{\"type\":\"string\",\"format\":\"date\"}},\"required\":[\"itemDescription\",\"price\"]}" },
        new Template { Name = "Loan Contract", Category = TemplateCategory.Debt, SchemaJson = "{\"type\":\"object\",\"properties\":{\"principal\":{\"type\":\"number\"},\"interestRate\":{\"type\":\"number\"},\"repaymentDate\":{\"type\":\"string\",\"format\":\"date\"}},\"required\":[\"principal\",\"repaymentDate\"]}" });

    await dbContext.SaveChangesAsync();
}