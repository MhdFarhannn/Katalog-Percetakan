using MySql.Data.MySqlClient;

namespace Katalog.Services
{
    public class Database{
      private readonly string _connectionString = Env.Value["Database:connection"]!;
      public MySqlConnection connect(){
        var connection = new MySqlConnection(_connectionString);
        return connection;
      }
    }
}
