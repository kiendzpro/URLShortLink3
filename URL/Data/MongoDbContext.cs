using MongoDB.Driver;
using URl.Models;

namespace URl.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly IConfiguration _configuration;

        public MongoDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
            string? connectionString = configuration["MongoDB:ConnectionString"];
            string? databaseName = configuration["MongoDB:DatabaseName"];

            if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("MongoDB configuration is missing in appsettings.json");
            }

            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<ShortUrl> ShortUrls => _database.GetCollection<ShortUrl>(
            _configuration["MongoDB:CollectionName"] ?? "ShortUrls_v2");
    }
}
