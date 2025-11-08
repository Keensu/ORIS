using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Models;
using MyORMLibrary;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace MyORMLibraryTests
{
    [TestClass]
    [DoNotParallelize]
    public class ORMContextTests
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=users;Username=postgres;Password=g7-gh-c5hc;";
        private ORMContext _context;

        [TestInitialize]
        public void Setup()
        {
            _context = new ORMContext(ConnectionString);
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"
                CREATE TABLE IF NOT EXISTS ""Users"" (
                    ""id"" SERIAL PRIMARY KEY,
                    ""name"" VARCHAR(100),
                    ""password"" VARCHAR(100),
                    ""email"" VARCHAR(100),
                    ""username"" VARCHAR(50),
                    ""age"" INT
                ); TRUNCATE TABLE ""Users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand(@"TRUNCATE TABLE ""Users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public void WhenCreatingNewUser_ItShouldBeStoredInDatabase()
        {
            var user = new MiniHttpServer.Models.User
            {
                Name = "Adam",
                Password = "passA1",
                Email = "adam@example.com",
                UserName = "adam01",
                Age = 26
            };

            _context.Create<MiniHttpServer.Models.User>(user);

            var users = _context.ReadByAll<MiniHttpServer.Models.User>();

            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("Adam", users[0].Name);
            Assert.AreEqual("passA1", users[0].Password);
            Assert.AreEqual("adam@example.com", users[0].Email);
            Assert.AreEqual("adam01", users[0].UserName);
            Assert.AreEqual(26, users[0].Age);
        }

        [TestMethod]
        public void WhenReadingUserById_ItShouldReturnCorrectRecord()
        {
            var user = new MiniHttpServer.Models.User
            {
                Name = "Emma",
                Password = "ePass!",
                Email = "emma@example.com",
                UserName = "emma_l",
                Age = 29
            };

            _context.Create<MiniHttpServer.Models.User>(user);
            var users = _context.ReadByAll<MiniHttpServer.Models.User>();
            int id = users[0].Id;

            var result = _context.ReadById<MiniHttpServer.Models.User>(id);

            Assert.IsNotNull(result);
            Assert.AreEqual("Emma", result.Name);
            Assert.AreEqual("ePass!", result.Password);
            Assert.AreEqual("emma@example.com", result.Email);
            Assert.AreEqual("emma_l", result.UserName);
            Assert.AreEqual(29, result.Age);
        }

        [TestMethod]
        public void WhenUpdatingExistingUser_ItShouldApplyChanges()
        {
            var user = new MiniHttpServer.Models.User
            {
                Name = "Liam",
                Password = "liam123",
                Email = "liam@mail.com",
                UserName = "liamX",
                Age = 22
            };

            _context.Create<MiniHttpServer.Models.User>(user);

            var users = _context.ReadByAll<MiniHttpServer.Models.User>();
            int id = users[0].Id;

            user.Name = "Liam Updated";
            user.Password = "newLiamPass";
            user.Email = "liam.updated@mail.com";
            user.UserName = "liamUpdated";
            user.Age = 24;

            _context.Update<MiniHttpServer.Models.User>(id, user);

            var updated = _context.ReadById<MiniHttpServer.Models.User>(id);
            Assert.AreEqual("Liam Updated", updated.Name);
            Assert.AreEqual("newLiamPass", updated.Password);
            Assert.AreEqual("liam.updated@mail.com", updated.Email);
            Assert.AreEqual("liamUpdated", updated.UserName);
            Assert.AreEqual(24, updated.Age);
        }

        [TestMethod]
        public void WhenDeletingUser_ItShouldBeRemovedFromDatabase()
        {
            var user = new MiniHttpServer.Models.User
            {
                Name = "Sofia",
                Password = "sofPass",
                Email = "sofia@mail.com",
                UserName = "sof",
                Age = 31
            };

            _context.Create<MiniHttpServer.Models.User>(user);

            var users = _context.ReadByAll<MiniHttpServer.Models.User>();
            int id = users[0].Id;

            _context.Delete(id);

            var remaining = _context.ReadByAll<MiniHttpServer.Models.User>();
            Assert.AreEqual(0, remaining.Count);
        }

        [TestMethod]
        public void WhenReadingAllUsers_ItShouldReturnAllRecords()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Noah", Password = "n1", Email = "noah@mail.com", UserName = "noahN", Age = 20 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Olivia", Password = "o2", Email = "olivia@mail.com", UserName = "oliv", Age = 25 });

            var users = _context.ReadByAll<MiniHttpServer.Models.User>();

            Assert.AreEqual(2, users.Count);
        }

        [TestMethod]
        public void WhenFilteringByNameAlice_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ava", Password = "a1", Email = "ava@mail.com", UserName = "avaA", Age = 23 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Lucas", Password = "l1", Email = "lucas@mail.com", UserName = "lucL", Age = 30 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ava", Password = "a2", Email = "ava2@mail.com", UserName = "ava2", Age = 28 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name == "Ava").ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Name == "Ava"));
        }

        [TestMethod]
        public void WhenFilteringByNameAvaAndAgeAbove26_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ava", Password = "pass", Email = "ava@mail.com", UserName = "ava1", Age = 24 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ava", Password = "pass2", Email = "av2@mail.com", UserName = "ava2", Age = 29 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Mason", Password = "pass3", Email = "m@mail.com", UserName = "mason", Age = 33 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name == "Ava" && u.Age > 26).ToList();

            Assert.AreEqual(1, users.Count);
            Assert.IsTrue(users.All(u => u.Name == "Ava" && u.Age > 26));
        }

        [TestMethod]
        public void WhenFilteringByNameAvaOrMason_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ava", Password = "a1", Email = "a1@mail.com", UserName = "ava", Age = 24 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Oliver", Password = "o1", Email = "o@mail.com", UserName = "oliv", Age = 32 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Mason", Password = "m1", Email = "m@mail.com", UserName = "mas", Age = 35 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name == "Ava" || u.Name == "Mason").ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Name == "Ava" || u.Name == "Mason"));
        }

        [TestMethod]
        public void WhenFilteringByAgeGreaterThan28_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ella", Password = "e1", Email = "e@mail.com", UserName = "ella", Age = 27 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ethan", Password = "eth", Email = "eth@mail.com", UserName = "eth", Age = 31 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Mia", Password = "mia", Email = "mia@mail.com", UserName = "mia", Age = 34 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Age > 28).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Age > 28));
        }

        [TestMethod]
        public void WhenFilteringByAgeLessOrEqual30_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Henry", Password = "h1", Email = "h@mail.com", UserName = "henry", Age = 29 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Emily", Password = "em", Email = "em@mail.com", UserName = "em", Age = 30 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "James", Password = "j", Email = "j@mail.com", UserName = "jam", Age = 34 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Age <= 30).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Age <= 30));
        }

        [TestMethod]
        public void WhenFilteringByNameContainingSubstring_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Isabella", Password = "i1", Email = "i@mail.com", UserName = "isa", Age = 23 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Bella", Password = "b1", Email = "b@mail.com", UserName = "bell", Age = 30 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Isabel", Password = "i2", Email = "i2@mail.com", UserName = "is2", Age = 28 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name.Contains("bel")).ToList();

            Assert.AreEqual(3, users.Count);
            Assert.IsTrue(users.All(u => u.Name.Contains("bel")));
        }

        [TestMethod]
        public void WhenFilteringByNameStartingWithPrefix_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Charlotte", Password = "c1", Email = "c@mail.com", UserName = "char", Age = 22 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Charles", Password = "c2", Email = "ch@mail.com", UserName = "char2", Age = 26 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "George", Password = "g1", Email = "g@mail.com", UserName = "geo", Age = 31 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name.StartsWith("Char")).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Name.StartsWith("Char")));
        }

        [TestMethod]
        public void WhenFilteringByNameEndingWithSuffix_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Patrick", Password = "p1", Email = "p@mail.com", UserName = "pat", Age = 28 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Nick", Password = "n1", Email = "n@mail.com", UserName = "nick", Age = 27 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Dominic", Password = "d1", Email = "d@mail.com", UserName = "dom", Age = 34 });

            var users = _context.Where<MiniHttpServer.Models.User>(u => u.Name.EndsWith("ick")).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => u.Name.EndsWith("ick")));
        }

        [TestMethod]
        public void WhenFilteringByNameInCollection_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Leo", Password = "l1", Email = "leo@mail.com", UserName = "leo", Age = 21 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Daniel", Password = "d1", Email = "dan@mail.com", UserName = "dan", Age = 32 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ella", Password = "e2", Email = "ell@mail.com", UserName = "ell", Age = 29 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Sam", Password = "s1", Email = "sam@mail.com", UserName = "sam", Age = 26 });

            var names = new List<string> { "Leo", "Ella" };
            var users = _context.Where<MiniHttpServer.Models.User>(u => names.Contains(u.Name)).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u => names.Contains(u.Name)));
        }

        [TestMethod]
        public void WhenFilteringByComplexCondition_ShouldReturnCorrectUsers()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Aria", Password = "a1", Email = "aria@mail.com", UserName = "aria", Age = 24 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Aria", Password = "a2", Email = "aria2@mail.com", UserName = "aria2", Age = 29 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Thomas", Password = "t1", Email = "th@mail.com", UserName = "thom", Age = 33 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Zoe", Password = "z1", Email = "z@mail.com", UserName = "zoe", Age = 36 });

            var users = _context.Where<MiniHttpServer.Models.User>(u =>
                (u.Name == "Aria" && u.Age > 26) || u.Age > 30
            ).ToList();

            Assert.AreEqual(2, users.Count);
            Assert.IsTrue(users.All(u =>
                (u.Name == "Aria" && u.Age > 26) || u.Age > 30
            ));
        }

        [TestMethod]
        public void WhenGettingFirstMatchingRecordByName_ShouldReturnCorrectUser()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Hannah", Password = "h1", Email = "h1@mail.com", UserName = "han1", Age = 24 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Hannah", Password = "h2", Email = "h2@mail.com", UserName = "han2", Age = 27 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Jack", Password = "j1", Email = "j@mail.com", UserName = "jack", Age = 31 });

            var user = _context.FirstOrDefault<MiniHttpServer.Models.User>(u => u.Name == "Hannah");

            Assert.IsNotNull(user);
            Assert.AreEqual("Hannah", user.Name);
            Assert.AreEqual(24, user.Age);
        }

        [TestMethod]
        public void WhenNoMatchingRecordFound_ShouldReturnNull()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Logan", Password = "log1", Email = "log@mail.com", UserName = "logan", Age = 28 });

            var user = _context.FirstOrDefault<MiniHttpServer.Models.User>(u => u.Name == "NonExisting");

            Assert.IsNull(user);
        }

        [TestMethod]
        public void WhenUsingComplexConditionWithFirstOrDefault_ShouldReturnCorrectUser()
        {
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Chloe", Password = "c1", Email = "c@mail.com", UserName = "chlo", Age = 24 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Ben", Password = "b1", Email = "b@mail.com", UserName = "ben", Age = 31 });
            _context.Create<MiniHttpServer.Models.User>(new MiniHttpServer.Models.User { Name = "Nathan", Password = "n1", Email = "n@mail.com", UserName = "nate", Age = 35 });

            var user = _context.FirstOrDefault<MiniHttpServer.Models.User>(u => u.Age > 30 && u.Name.Contains("e"));

            Assert.IsNotNull(user);
            Assert.AreEqual("Ben", user.Name);
            Assert.AreEqual(31, user.Age);
        }
    }
}
