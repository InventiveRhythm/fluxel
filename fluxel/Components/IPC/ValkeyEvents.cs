using MessagePack;

namespace fluxel.Components.IPC;

[MessagePackObject(true)]
public record ValkeyIDEvent(long ID);

[MessagePackObject(true)]
public record ValkeyStringIDEvent(string ID);

[MessagePackObject(true)]
public record ValkeyChannelMemberEvent(string ChannelID, long UserID);
