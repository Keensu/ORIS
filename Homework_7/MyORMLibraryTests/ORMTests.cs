using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniHttpServer.Models;
using Npgsql;
using System.Collections.Generic;
using System.Linq;

namespace MyORMLibraryTests
{
    [TestClass]
    [DoNotParallelize]
    public class ORMContextTests
    {
        private const string ConnectionString = "Host=localhost;Port=5432;Database=users;Username=postgres;Password=g7-gh-c5hc;";
        private ORMContext? _context;

        [TestInitialize]
        public void Setup()
        {
            _context = new ORMContext(ConnectionString);

            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS ""users"" (
                    ""id"" SERIAL PRIMARY KEY,
                    ""username"" VARCHAR(50),
                    ""email"" VARCHAR(50),
                    ""password"" VARCHAR(50)
                );
                TRUNCATE TABLE ""users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"TRUNCATE TABLE ""users"" RESTART IDENTITY;", conn);
            cmd.ExecuteNonQuery();
        }

        [TestMethod]
        public void WhenCreatingNewUser_ItShouldBeStoredInDatabase()
        {
            var user = new User { username = "adam01", email = "adam@example.com", password = "passA1" };
            _context.Create(user);

            var users = _context.ReadByAll<User>().ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("adam01", users[0].username);
            Assert.AreEqual("adam@example.com", users[0].email);
            Assert.AreEqual("passA1", users[0].password);
        }

        [TestMethod]
        public void WhenReadingUserById_ItShouldReturnCorrectRecord()
        {
            var user = new User { username = "emma_l", email = "emma@example.com", password = "ePass!" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().id;
            var result = _context.ReadById<User>(id);

            Assert.IsNotNull(result);
            Assert.AreEqual("emma_l", result.username);
            Assert.AreEqual("emma@example.com", result.email);
            Assert.AreEqual("ePass!", result.password);
        }

        [TestMethod]
        public void WhenUpdatingExistingUser_ItShouldApplyChanges()
        {
            var user = new User { username = "liamX", email = "liam@mail.com", password = "liam123" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().id;

            user.username = "liamUpdated";
            user.email = "liam.updated@mail.com";
            user.password = "newLiamPass";

            _context.Update(id, user);

            var updated = _context.ReadById<User>(id);
            Assert.AreEqual("liamUpdated", updated.username);
            Assert.AreEqual("liam.updated@mail.com", updated.email);
            Assert.AreEqual("newLiamPass", updated.password);
        }

        [TestMethod]
        public void WhenDeletingUser_ItShouldBeRemovedFromDatabase()
        {
            var user = new User { username = "sof", email = "sofia@mail.com", password = "sofPass" };
            _context.Create(user);

            int id = _context.ReadByAll<User>().First().id;
            _context.Delete<User>(id);

            Assert.AreEqual(0, _context.ReadByAll<User>().Count);
        }

        [TestMethod]
        public void WhenReadingAllUsers_ItShouldReturnAllRecords()
        {
            _context.Create(new User { username = "noahN", email = "noah@mail.com", password = "n1" });
            _context.Create(new User { username = "oliv", email = "olivia@mail.com", password = "o2" });

            var users = _context.ReadByAll<User>().ToList();
            Assert.AreEqual(2, users.Count);
        }

        [TestMethod]
        public void WhenFilteringByUserName_ShouldReturnCorrectUsers()
        {
            _context.Create(new User { username = "avaA", email = "ava@mail.com", password = "a1" });
            _context.Create(new User { username = "lucL", email = "lucas@mail.com", password = "l1" });
            _context.Create(new User { username = "ava2", email = "ava2@mail.com", password = "a2" });

            var users = _context.Where<User>(u => u.username == "avaA").ToList();
            Assert.AreEqual(1, users.Count);
            Assert.AreEqual("avaA", users[0].username);
        }

        [TestMethod]
        public void WhenGettingFirstMatchingRecordByUserName_ShouldReturnCorrectUser()
        {
            _context.Create(new User { username = "han1", email = "h1@mail.com", password = "h1" });
            _context.Create(new User { username = "han2", email = "h2@mail.com", password = "h2" });

            var user = _context.FirstOrDefault<User>(u => u.username == "han1");
            Assert.IsNotNull(user);
            Assert.AreEqual("han1", user.username);
        }
    }
}
