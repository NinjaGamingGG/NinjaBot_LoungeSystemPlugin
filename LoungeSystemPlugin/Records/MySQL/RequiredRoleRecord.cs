using Dapper.Contrib.Extensions;

namespace LoungeSystemPlugin.Records.MySQL;

[Table("RequiredRoleIndex")]
public record RequiredRoleRecord
{
   [ExplicitKey]
   public int Id { get; init; }
   
   public ulong GuildId { get; init; }
   
   public ulong ChannelId { get; init; }
   
   public ulong RoleId { get; init; }
}