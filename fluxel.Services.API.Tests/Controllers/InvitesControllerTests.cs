using fluxel.Models.Clubs;
using fluxel.Services.API.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Midori.Networking;

namespace fluxel.Services.API.Tests.Controllers;

public class InvitesControllerTests : DependencyStackTest
{
    private InvitesController controller = null!;

    private string code = string.Empty;

    public override void Setup()
    {
        base.Setup();

        controller = ActivatorUtilities.CreateInstance<InvitesController>(ServiceProvider);

        var u1 = CreateUser(1);
        var u2 = CreateUser(2);
        _ = CreateUser(3);

        var club = Database.Clubs.Add(new Club
        {
            ID = 1,
            Owner = u1,
            Members = [u1],
            Name = "test club",
            Tag = "TEST"
        }).Entity;

        code = Database.ClubInvites.Add(new ClubInvite(club, u2)).Entity.InviteCode;
        Database.SaveChanges();
    }

    [Test]
    public void TestGet()
    {
        var result = controller.Get(code);
        LogResult(result);

        Assert.NotNull(result.Result);
    }

    [Test]
    public void TestAcceptNormal()
    {
        var user = Database.Users.Find(2L)!;
        var result = controller.Accept(user, code);
        LogResult(result);

        Assert.That(Database.Users.Find(2L)?.ClubID, Is.EqualTo(1));
    }

    [Test]
    public void TestAlreadyMember()
    {
        var user = Database.Users.Find(2L)!;
        user.ClubID = 1;
        Database.SaveChanges();

        var result = controller.Accept(user, code);
        LogResult(result);

        Assert.That(result.Status, Is.Not.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void TestInvalidTarget()
    {
        var user = Database.Users.Find(3L)!;
        var result = controller.Accept(user, code);
        LogResult(result);

        Assert.That(result.Status, Is.Not.EqualTo(HttpStatusCode.OK));
    }
}
