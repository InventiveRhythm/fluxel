using fluxel.Models.Chat;
using fluxel.Models.Users;
using fluxel.Services.API.Controllers;
using fluXis.Online.API.Models.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.API.Tests.Controllers;

[TestFixture, TestOf(typeof(ChatController))]
public class ChatControllerTests : DependencyStackTest
{
    private ChatController controller = null!;

    public override void Setup()
    {
        base.Setup();

        controller = ActivatorUtilities.CreateInstance<ChatController>(ServiceProvider);

        var u1 = Database.Users.Add(new User { ID = 1, Username = "test user 1" }).Entity;
        var u2 = Database.Users.Add(new User { ID = 2, Username = "test user 2" }).Entity;

        var general = Database.ChatChannels.Add(new ChatChannel("general", APIChannelType.Public)).Entity;
        var pm = Database.ChatChannels.Add(new ChatChannel("private_1-2", APIChannelType.Private)
        {
            TargetUser1 = u1,
            TargetUser2 = u2
        }).Entity;

        Database.ChatChannelMembers.Add(new ChatChannelMember(general, u1));
        Database.ChatChannelMembers.Add(new ChatChannelMember(pm, u1));
        Database.ChatChannelMembers.Add(new ChatChannelMember(pm, u2));

        Database.SaveChanges();
    }

    [Test]
    public void TestListChannels()
    {
        var result = controller.ListChannels();
        Assert.That(result.Result?.Length, Is.EqualTo(1));
    }

    [Test]
    public void TestJoinedChannels()
    {
        var user = Database.Users.Find(1L);
        Assert.NotNull(user);

        var result = controller.ListJoined(user);
        Assert.That(result.Result?.Length, Is.EqualTo(2));
    }
}
