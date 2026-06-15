namespace SecureMessaging.Api.Helpers;

public static class ConversationKeyBuilder
{
    public static string BuildDirect(params string[] participantIds)
    {
        return string.Join(":", participantIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal));
    }
}
