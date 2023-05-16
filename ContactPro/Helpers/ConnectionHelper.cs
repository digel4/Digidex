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

              Console.WriteLine($"inside GetConnectionString");
              Console.WriteLine($"databaseUrl is: {databaseUrl}");
              Console.WriteLine($"IsNullOrEmpty: {string.IsNullOrEmpty(databaseUrl)}");

              if (string.IsNullOrEmpty(databaseUrl))
              {

                     Console.WriteLine($"connectionString");
                     return connectionString;
              }
              else
              {
                     Console.WriteLine($"returning newConnectionString");
                     var newConnectionString = BuildConnectionString(databaseUrl);
                     
                     return newConnectionString;
              }
              
              //return string.IsNullOrEmpty(databaseUrl) ? connectionString : BuildConnectionString(databaseUrl);
       }

       // Build a connection string from the env. i.e. Heroku
       private static string BuildConnectionString(string databaseUrl)
       {
              Console.WriteLine($"initializing  databasUri");
              var databaseUri = new Uri(databaseUrl);
              Console.WriteLine($"databasUri is: {databaseUri}");
              
              Console.WriteLine($"initializing userInfo");
              var userInfo = databaseUri.UserInfo.Split(":");
              Console.WriteLine($"databaseUri.UserInfo is: {databaseUri.UserInfo}");
              Console.WriteLine($"userInfo is: {userInfo}");
              
              Console.WriteLine($"initializing builder");
              var builder = new NpgsqlConnectionStringBuilder
              {
                     Host = databaseUri.Host,
                     Port = databaseUri.Port,
                     Username = userInfo[0],
                     Password = userInfo[1],
                     Database = databaseUri.LocalPath.TrimStart('/'),
                     SslMode = SslMode.Require,
                     TrustServerCertificate = true

              };
              Console.WriteLine($"inside BuildConnectionString");


              Console.WriteLine($"builder is: {builder}");
              return builder.ToString();
       }
}