using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records.MySQL;

[Table("LoungeIndex")]
public record LoungeDbRecord
{
    [ExplicitKey]
    public ulong ChannelId { get; init; }
    public ulong GuildId { get; init; }
    public ulong OwnerId { get; init; }
    public bool IsPublic { get; init; }
    public ulong OriginChannel { get; init; }

}