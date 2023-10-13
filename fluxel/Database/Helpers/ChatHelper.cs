using fluxel.Components.Chat;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class ChatHelper
{
    private static IMongoCollection<ChatMessage> messages => MongoDatabase.GetCollection<ChatMessage>("chat_messages");

    public static void Add(ChatMessage message) => messages.InsertOne(message);

    public static ChatMessage? Get(Guid id) => messages.Find(x => x.Id == id).FirstOrDefault();

    public static void Delete(ChatMessage message)
    {
        message.Deleted = true;
        messages.ReplaceOne(x => x.Id == message.Id, message);
    }

    public static List<ChatMessage> FromChannel(string channel) => messages.Find(x => x.Channel == channel && !x.Deleted).ToList();
}
