using Npgsql;
namespace ContactPro.Helpers;

// Static means that we don't have to create an instance of the object and that there can only be one. Note every method in a static class also has to be static
public static class ConnectionHelper
{
       public static string GetConnectionString(IConfiguration configuration)
       {
              // connectionString will only have a value in a local env
              var connectionString = configuration.GetSection("pgSettings")["PgConnection"];
              // databaseUrl will only have a value in a production env
              var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

              return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);
       }

       // Build a connection string from the env. i.e. Heroku
       private static string BuildConnectionString(string databaseUrl)
       {
              var databasUri = new Uri(databaseUrl);
              var userInfo = databasUri.UserInfo.Split(";");
              var builder = new NpgsqlConnectionStringBuilder
              {
                     Host = databasUri.Host,
                     Port = databasUri.Port,
                     Username = userInfo[0],
                     Password = userInfo[1],
                     Database = databasUri.LocalPath.TrimStart('/'),
                     SslMode = SslMode.Require,
                     TrustServerCertificate = true

              };
              return builder.ToString();
       }
}