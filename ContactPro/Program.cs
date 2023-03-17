using ContactPro.Data;
using ContactPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ContactPro.Services;
using ContactPro.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;


//using ContactPro.Helpers;
// using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

//var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri"));
//builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// var connectionString = ConnectionHelper.GetConnectionString(builder.Configuration);
var connectionString = builder.Configuration.GetSection("pgSettings")["PgConnection"];
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = true)
        .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

///custom Services
/// 
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAddressBookService, AddressBookService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

var app = builder.Build();
var scope = app.Services.CreateScope();

// // database update with the latest migrations
// await DataHelper.ManageDataAsync(scope.ServiceProvider);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//Custom error handler for the entire app. When an error happens go to route below with error code[0]/ Because this is an actual route /Home goes to the Homecontroller {HandleError}
app.UseStatusCodePagesWithReExecute("/Home/HandleError/{0}");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
