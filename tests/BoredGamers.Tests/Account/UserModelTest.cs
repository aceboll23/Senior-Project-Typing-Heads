using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;

namespace BoredGamers.Tests.Account
{
    [TestFixture]
    public class UserModelTests
    {
        [Test]
        public void User_ShouldInheritFrom_IdentityUser()
        {
            // Arrange & Act
            var user = new User();
            
            // Assert
            Assert.That(user, Is.InstanceOf<IdentityUser>());
        }
        
        [Test]
        public void User_ShouldHave_CustomProperties()
        {
            // Arrange
            var user = new User
            {
                FirstName = "Bob",
                LastName = "Marley",
                Birthday = new DateOnly(1945, 2, 6),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(user.FirstName, Is.EqualTo("Bob"));
                Assert.That(user.LastName, Is.EqualTo("Marley"));
                Assert.That(user.Birthday, Is.EqualTo(new DateOnly(1945, 2, 6)));
                Assert.That(user.CreatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
                Assert.That(user.UpdatedAt, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(1)));
            });
        }
        
        [Test]
        public void User_ShouldHave_InheritedIdentityProperties()
        {
            // Arrange
            var user = new User
            {
                UserName = "bobmarley",
                Email = "bob@mail.com",
                EmailConfirmed = true
            };
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(user.UserName, Is.EqualTo("bobmarley"));
                Assert.That(user.Email, Is.EqualTo("bob@mail.com"));
                Assert.That(user.EmailConfirmed, Is.True);
            });
        }
        
        [Test]
        public void User_FirstName_ShouldBeNullable()
        {
            // Arrange
            var user = new User
            {
                FirstName = null
            };
            
            // Assert
            Assert.That(user.FirstName, Is.Null);
        }
        
        [Test]
        public void User_LastName_ShouldBeNullable()
        {
            // Arrange
            var user = new User
            {
                LastName = null
            };
            
            // Assert
            Assert.That(user.LastName, Is.Null);
        }
        
        [Test]
        public void User_Birthday_ShouldBeNullable()
        {
            // Arrange
            var user = new User
            {
                Birthday = null
            };
            
            // Assert
            Assert.That(user.Birthday, Is.Null);
        }
        
        [Test]
        public void User_CanSet_AllPropertiesTogether()
        {
            // Arrange & Act
            var user = new User
            {
                UserName = "bobmarley",
                Email = "bob@mail.com",
                FirstName = "Bob",
                LastName = "Marley",
                Birthday = new DateOnly(1945, 2, 6),
                CreatedAt = new DateTime(2026, 2, 14),
                UpdatedAt = new DateTime(2026, 2, 14)
            };
            
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(user.UserName, Is.EqualTo("bobmarley"));
                Assert.That(user.Email, Is.EqualTo("bob@mail.com"));
                Assert.That(user.FirstName, Is.EqualTo("Bob"));
                Assert.That(user.LastName, Is.EqualTo("Marley"));
                Assert.That(user.Birthday.HasValue, Is.True);
                Assert.That(user.Birthday.Value, Is.EqualTo(new DateOnly(1945, 2, 6)));
                Assert.That(user.CreatedAt.Year, Is.EqualTo(2026));
                Assert.That(user.UpdatedAt.Year, Is.EqualTo(2026));
            });
        }
    }
}