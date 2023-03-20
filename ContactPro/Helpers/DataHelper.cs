using ContactPro.Data;
using Microsoft.EntityFrameworkCore;

namespace ContactPro.Helpers;

public static class DataHelper
{
    // This runs the db update when it's ran on a heroku server or if changes need to be made
    public static async Task ManageDataAsync(IServiceProvider svcProvider)
    {
        // Get an instance of the db application context
        var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
        
        // Migration: this is equivalent to dotnet ef database update
        await dbContextSvc.Database.MigrateAsync();
    }
}