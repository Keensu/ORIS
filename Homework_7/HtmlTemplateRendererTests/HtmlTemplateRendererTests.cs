using MiniTemplateEngine;
namespace HtmlTemplateRendererTests
{
    [TestClass]
    public sealed class HtmlTemplateRendererTests
    {
        [TestMethod]
        public void RenderFromString_When_Return()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1> Привет, Тимерхан</h1>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenDoubleReplace_ReturnCorrectString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> Привет, ${Name}</p>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> Привет, Тимерхан</p>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenTwoProperties_ReturnCorrectString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> Привет, ${Email}</p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru" };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> Привет, test@test.ru</p>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenSubProperties_ReturnCorrectString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> группа: ${Group.Name}</p>";
            var model = new
            {
                Name = "Тимерхан",
                Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                }
            };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> группа: 11-409</p>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_When_ReturnCorrectString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> группа: ${Group.Name}</p>";
            var model = new
            {
                Name = "Тимерхан",
                Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                }
            };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> группа: 11-409</p>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenIf_ReturnCorrectString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1>$if(Name == \"Тимерхан\")<p>Привет, Тимерхан</p>$endif</h1>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1><p>Привет, Тимерхан</p></h1>";

            // Act
            var result = testee.RenderFromString(templateHtml, model);

            // Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenIfElseTrue_ReturnsTrueBlock()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(IsActive)<p>Active</p>$else<p>Inactive</p>$endif";
            var model = new { IsActive = true };
            string expected = "<p>Active</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenIfElseFalse_ReturnsElseBlock()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(IsActive)<p>Active</p>$else<p>Inactive</p>$endif";
            var model = new { IsActive = false };
            string expected = "<p>Inactive</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenForeach_ReturnsListItems()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<ul>$foreach(var u in Users)<li>${u.Name}</li>$endfor</ul>";
            var model = new { Users = new[] { new { Name = "Kate" }, new { Name = "Alex" } } };
            string expected = "<ul><li>Kate</li><li>Alex</li></ul>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenEmptyCollection_ReturnsEmpty()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<ul>$foreach(var u in Users)<li>${u.Name}</li>$endfor</ul>";
            var model = new { Users = Array.Empty<object>() };
            string expected = "<ul></ul>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenNestedForeach_ReturnsNestedItems()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$foreach(var g in Groups)<h2>${g.Name}</h2>$foreach(var u in g.Users)<p>${u.Name}</p>$endfor$endfor";
            var model = new
            {
                Groups = new[]
                {
            new { Name = "Group1", Users = new[] { new { Name = "Kate" }, new { Name = "Alex" } } },
            new { Name = "Group2", Users = new[] { new { Name = "Jenny" } } }
        }
            };
            string expected = "<h2>Group1</h2><p>Kate</p><p>Alex</p><h2>Group2</h2><p>Jenny</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenVariableNotExists_ReturnsEmpty()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<p>${Missing}</p>";
            var model = new { Name = "Test" };
            string expected = "<p></p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenNestedPropertyNotExists_ReturnsEmpty()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<p>${User.Name}</p>";
            var model = new { };
            string expected = "<p></p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenIfElseWithoutElse_ReturnsEmptyOnFalse()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(IsActive)<p>Active</p>$endif";
            var model = new { IsActive = false };
            string expected = "";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromFile_WhenFileExists_ReturnsRenderedString()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string path = "test.html";
            File.WriteAllText(path, "<h1>${Name}</h1>");
            var model = new { Name = "Kate" };
            string expected = "<h1>Kate</h1>";

            // Act
            var result = testee.RenderFromFile(path, model);
            File.Delete(path);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderToFile_CreatesFileWithRenderedContent()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string inputPath = "input.html";
            string outputPath = "output.html";
            File.WriteAllText(inputPath, "<h1>${Name}</h1>");
            var model = new { Name = "Alex" };

            // Act
            testee.RenderToFile(inputPath, outputPath, model);
            string content = File.ReadAllText(outputPath);
            File.Delete(inputPath);
            File.Delete(outputPath);

            // Assert
            Assert.AreEqual("<h1>Alex</h1>", content);
        }

        [TestMethod]
        public void RenderFromString_WhenForeachWithIfElse_ReturnsCorrectResult()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$foreach(var u in Users)$if(u.IsActive)<p>${u.Name}</p>$endif$endfor";
            var model = new
            {
                Users = new[]
                {
            new { Name = "Kate", IsActive = true },
            new { Name = "Alex", IsActive = false }
        }
            };
            string expected = "<p>Kate</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenMultipleVariablesAndForeach_ReturnsCorrect()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<h1>${Title}</h1>$foreach(var u in Users)<p>${u.Name} - ${u.Email}</p>$endfor";
            var model = new
            {
                Title = "Users",
                Users = new[]
                {
            new { Name = "Kate", Email = "kate@test.com" },
            new { Name = "Alex", Email = "alex@test.com" }
        }
            };
            string expected = "<h1>Users</h1><p>Kate - kate@test.com</p><p>Alex - alex@test.com</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenForeachDictionary_ReturnsCorrect()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$foreach(var item in Items)<p>${item.Value}</p>$endfor";
            var model = new
            {
                Items = new[]
                {
            new Dictionary<string, object> { { "Value", "One" } },
            new Dictionary<string, object> { { "Value", "Two" } }
        }
            };
            string expected = "<p>One</p><p>Two</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        // --- ДОПОЛНИТЕЛЬНЫЕ ТЕСТЫ ---

        [TestMethod]
        public void RenderFromString_WhenNestedIfInsideIf_ReturnsInnerBlock()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(IsAdmin)$if(IsSuper)<p>Super Admin</p>$else<p>Admin</p>$endif$else<p>User</p>$endif";
            var model = new { IsAdmin = true, IsSuper = true };
            string expected = "<p>Super Admin</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenNestedIfInsideForeach_ReturnsConditionalBlocks()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$foreach(var u in Users)$if(u.IsAdmin)<p>${u.Name} (Admin)</p>$else<p>${u.Name}</p>$endif$endfor";
            var model = new
            {
                Users = new[]
                {
            new { Name = "Kate", IsAdmin = true },
            new { Name = "Alex", IsAdmin = false }
        }
            };
            string expected = "<p>Kate (Admin)</p><p>Alex</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenIfInsideNestedForeach_ReturnsExpected()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$foreach(var g in Groups)$foreach(var u in g.Users)$if(u.Active)<p>${g.Name}-${u.Name}</p>$endif$endfor$endfor";
            var model = new
            {
                Groups = new[]
                {
            new { Name = "Group1", Users = new[] { new { Name = "Kate", Active = true }, new { Name = "Alex", Active = false } } },
            new { Name = "Group2", Users = new[] { new { Name = "Jenny", Active = true } } }
        }
            };
            string expected = "<p>Group1-Kate</p><p>Group2-Jenny</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenConditionUsesBoolProperty_ReturnsTrueBlock()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(Enabled)<p>ON</p>$else<p>OFF</p>$endif";
            var model = new { Enabled = true };
            string expected = "<p>ON</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenConditionValueNotEqual_ReturnsElseBlock()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "$if(Role==\"Admin\")<p>Admin</p>$else<p>User</p>$endif";
            var model = new { Role = "User" };
            string expected = "<p>User</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void RenderFromString_WhenWhitespaceAndExtraLines_RemovedProperly()
        {
            // Arrange
            var testee = new HtmlTemplateRenderer();
            string template = "<p>${Name}</p>\n\n<p>${Email}</p>";
            var model = new { Name = "Test", Email = "mail@test.ru" };
            string expected = "<p>Test</p>\n<p>mail@test.ru</p>";

            // Act
            var result = testee.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(expected, result);
        }

        /// <summary>
        /// Убираем разрывы строк и пробелы
        /// </summary>
        private static string Normalize(string s)
        {
            return s.Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace(" ", string.Empty);
        }

        /// <summary>
        /// Проверяет корректность обработки конструкций $if/$else и вставки переменных.
        /// </summary>
        /// <param name="template">Шаблон </param>
        /// <param name="expected">Ожидаемый результат </param>
        [DataTestMethod]
        [DataRow("$if(IsTrue) <p>User is active</p>$endif", " <p>User is active</p>")]
        [DataRow("$if(IsTrue) <p>User is active</p>$else <p>Not active</p>$endif", " <p>User is active</p>")]
        [DataRow("$if(IsTrue) <p>${Name}</p>$endif", " <p>Kriik</p>")]
        [DataRow("${Email} $if(IsTrue) <p>${Name}</p>$endif", "test@test.ru  <p>Kriik</p>")]
        [DataRow("$if(IsTrue) <p>${Name}</p>$endif ${Email}", " <p>Kriik</p> test@test.ru")]
        [DataRow("$if(IsFalse) <p>User is active</p>$endif ", " ")]
        [DataRow("$if(IsFalse) <p>User is active</p>$else <p>Not active</p>$endif", " <p>Not active</p>")]
        [DataRow("$if(IsFalse) <p>User is active</p>$else <p>${Name}</p>$endif", " <p>Kriik</p>")]
        [DataRow("$if(IsTrue)$if(IsTrue)${Name}$endif $endif", "Kriik ")]
        [DataRow("$if(IsTrue)$if(IsTrue)${Name}$else Net $endif $endif", "Kriik ")]
        [DataRow("$if(IsTrue)$if(IsFalse)${Name}$else Net $endif $endif", " Net  ")]
        [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $endif", "")]
        [DataRow("$if(IsFalse)$if(IsFalse)${Name}$else Net $endif $else a $endif", " a ")]
        [DataRow("$if(IsTrue)${Email} $if(IsFalse)${Name}$else Net $endif $else a $endif", "test@test.ru  Net  ")]

        public void TestIfAndVariableBlocks(string template, string expected)
        {
            // Arrange
            HtmlTemplateRenderer renderer = new HtmlTemplateRenderer();
            var model = new
            {
                Name = "Kriik",
                Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                },
                IsTrue = true,
                IsFalse = false,
            };

            // Act
            string result = renderer.RenderFromString(template, model);

            // Assert
            Assert.AreEqual(Normalize(expected), Normalize(result));
        }





    }
}
