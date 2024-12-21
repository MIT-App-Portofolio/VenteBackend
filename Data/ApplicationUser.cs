using Microsoft.AspNetCore.Identity;

namespace Server.Data;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; }
    public AccountStatus Status { get; set; }
}

public enum AccountStatus
{
    WaitingSetup,
    Active
}