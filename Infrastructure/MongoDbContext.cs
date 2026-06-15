using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SecureMessaging.Api.Configuration;
using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Infrastructure;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        var value = options.Value;
        var client = new MongoClient(value.ConnectionString);
        _database = client.GetDatabase(value.DatabaseName);
    }

    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    public IMongoCollection<Conversation> Conversations =>
        _database.GetCollection<Conversation>("conversations");

    public IMongoCollection<EncryptedMessage> Messages =>
        _database.GetCollection<EncryptedMessage>("messages");

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await ReplaceLegacySparseUserIndexesAsync(cancellationToken);

        var userIndexes = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.UserName),
                new CreateIndexOptions { Unique = true, Name = "ux_users_username" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.Email),
                new CreateIndexOptions<User>
                {
                    Unique = true,
                    Name = "ux_users_email",
                    PartialFilterExpression =
                        new BsonDocumentFilterDefinition<User>(
                            new BsonDocument("email", new BsonDocument("$type", "string")))
                }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.GoogleSubject),
                new CreateIndexOptions<User>
                {
                    Unique = true,
                    Name = "ux_users_google_subject",
                    PartialFilterExpression =
                        new BsonDocumentFilterDefinition<User>(
                            new BsonDocument(
                                "googleSubject",
                                new BsonDocument("$type", "string")))
                })
        };

        var conversationIndexes = new[]
        {
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(conversation => conversation.ConversationKey),
                new CreateIndexOptions { Unique = true, Name = "ux_conversations_key" }),
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Ascending(conversation => conversation.ParticipantIds),
                new CreateIndexOptions { Name = "ix_conversations_participants" }),
            new CreateIndexModel<Conversation>(
                Builders<Conversation>.IndexKeys.Descending(conversation => conversation.LastMessageAtUtc),
                new CreateIndexOptions { Name = "ix_conversations_last_message" })
        };

        var messageIndexes = new[]
        {
            new CreateIndexModel<EncryptedMessage>(
                Builders<EncryptedMessage>.IndexKeys
                    .Ascending(message => message.ConversationId)
                    .Descending(message => message.SentAtUtc),
                new CreateIndexOptions { Name = "ix_messages_conversation_sent_at" }),
            new CreateIndexModel<EncryptedMessage>(
                Builders<EncryptedMessage>.IndexKeys
                    .Ascending(message => message.ConversationId)
                    .Ascending(message => message.SenderUserId)
                    .Ascending(message => message.ClientMessageId),
                new CreateIndexOptions<EncryptedMessage>
                {
                    Unique = true,
                    Name = "ux_messages_client_id",
                    PartialFilterExpression =
                        new BsonDocumentFilterDefinition<EncryptedMessage>(
                            new BsonDocument(
                                "clientMessageId",
                                new BsonDocument("$type", "string")))
                })
        };

        await Users.Indexes.CreateManyAsync(userIndexes, cancellationToken);
        await Conversations.Indexes.CreateManyAsync(conversationIndexes, cancellationToken);
        await Messages.Indexes.CreateManyAsync(messageIndexes, cancellationToken);
    }

    private async Task ReplaceLegacySparseUserIndexesAsync(
        CancellationToken cancellationToken)
    {
        using var cursor = await Users.Indexes.ListAsync(cancellationToken);
        var indexes = await cursor.ToListAsync(cancellationToken);

        foreach (var indexName in new[] { "ux_users_email", "ux_users_google_subject" })
        {
            var existing = indexes.FirstOrDefault(index =>
                index.TryGetValue("name", out var name) &&
                name.IsString &&
                string.Equals(name.AsString, indexName, StringComparison.Ordinal));

            if (existing is not null &&
                !existing.Contains("partialFilterExpression"))
            {
                await Users.Indexes.DropOneAsync(indexName, cancellationToken);
            }
        }
    }
}
