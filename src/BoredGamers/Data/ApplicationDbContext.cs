using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Data;

/*
 * ApplicationDbContext
 * --------------------
 * Inherits from IdentityDbContext so ASP.NET Core Identity can create/manage
 * auth tables (AspNetUsers, AspNetRoles, etc.).
 *
 * Add your domain DbSets (Games, Groups, Events, etc.) here in future sprints.
 */
public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}