using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace URl.Models
{
    public class ShortUrl
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("originalUrl")]
        public string OriginalUrl { get; set; } = string.Empty;

        [BsonElement("shortCode")]
        public string ShortCode { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [BsonElement("clickCount")]
        public int ClickCount { get; set; } = 0;
        
        [BsonElement("urlName")]
        public string UrlName { get; set; } = string.Empty;
    }
}
