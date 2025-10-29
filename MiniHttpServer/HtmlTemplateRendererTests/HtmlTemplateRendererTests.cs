using MiniTemplateAgile;
namespace HtmlTemplateRendererTests
{
    [TestClass]
    public sealed class HtmlTemplateRendererTests
    {
        [TestMethod]
        public void RenderFromString_When_Return()
        {
            //Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1> Привет, Тимерхан</h1>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);


            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenDoubleReplace_ReturnCorrectString()
        {
            //Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> Привет, ${Name}</p>";
            var model = new { Name = "Тимерхан" };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> Привет, Тимерхан</p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenTwoProperties_ReturnCorrectString()
        {
            //Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> Привет, ${Email}</p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru" };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> Привет, test@test.ru</p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_WhenSubProperties_ReturnCorrectString()
        {
            //Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1> Привет, ${Name}</h1><p> группа: ${Group.Name}</p>";
            var model = new { Name = "Тимерхан", Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                }
            };
            string expectedString = "<h1> Привет, Тимерхан</h1><p> группа: 11-409</p>";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

        [TestMethod]
        public void RenderFromString_When_ReturnCorrectString()
        {
            //Arrange
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

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }


        [TestMethod]
        public void RenderFromString_WhenIf_ReturnCorrectString()
        {
            //Arrange
            var testee = new HtmlTemplateRenderer();
            string templateHtml = "<h1>$If(isAuth == true) <p>Привет, Тимерхан</p> $endIf</h1>";
            var model = new
            {
                isAuth = true,
                Name = "Тимерхан",
                Email = "test@test.ru",
                Group = new
                {
                    Id = 1,
                    Name = "11-409",
                }
            };
            string expectedString = "";

            //Act
            var result = testee.RenderFromString(templateHtml, model);

            //Assert
            Assert.AreEqual(expectedString, result);
        }

    }
}
