using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MiniTemplateAgile
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            var regex = new Regex(@"\$\{([A-Za-z_][A-Za-z0-9_\.]*)\}");
            var ifRegex = new Regex(@"\$if\s*\(([^)]*)\)\s*([\s\S]*?)\s*\$endif");

            for (int i = 0; i < htmlTemplate.Length; i++)
            {
                if (htmlTemplate[i] == '$')
                {
                    switch (htmlTemplate[i + 1])
                    {
                        case '{':

                            break;

                        case 'i':
                            var ifBlock = ifRegex.Match(htmlTemplate, i - 1);
                            break;
                    }
                }
            }

            return "pivo";

        }

        public static string HandleIfBlock(Match match, object data)
        {
            var codition = match.Groups[1].Value;

            var value = match.Groups[2].Value;
            return "pivo";

        }

        public string RenderFromFile(string filePath, object dataModel)
        {
            throw new NotImplementedException();
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            throw new NotImplementedException();
        }
        public static object? GetValueByPath(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return null;

            var parts = path.Split('.');
            object? current = obj;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                var type = current.GetType();
                var prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop == null)
                    return null;

                current = prop.GetValue(current);
            }

            return current;
        }
    }
}
