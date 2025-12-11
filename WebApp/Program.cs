using WebApp.Data;
using Microsoft.EntityFrameworkCore;
using WebApp.Interfaces;
using WebApp.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
builder.Services.AddSession();

builder.Services.AddScoped<IVolunteerRepository, WebApp.Repositories.VolunteerRepository>();
builder.Services.AddScoped<IOrganizationRepository, WebApp.Repositories.OrganizationRepository>();
builder.Services.AddScoped<IProjectRepository, WebApp.Repositories.ProjectRepository>();
builder.Services.AddScoped<IApplicationRepository, WebApp.Repositories.ApplicationRepository>();
builder.Services.AddScoped<IAdminRepository, WebApp.Repositories.AdminRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
