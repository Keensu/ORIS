using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MiniTemplateEngine
{
    public class HtmlTemplateRenderer : IHtmlTemplateRenderer
    {
        public string RenderFromFile(string filePath, object dataModel)
        {
            var html = File.ReadAllText(filePath);
            return RenderFromString(html, dataModel);
        }

        public string RenderFromString(string htmlTemplate, object dataModel)
        {
            var result = htmlTemplate;

            result = ProcessForeach(result, dataModel);
            result = ProcessIfElse(result, dataModel);
            result = ProcessVariables(result, dataModel);

            result = Regex.Replace(result, @"\n\s*\n", "\n");

            return result;
        }

        public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
        {
            var result = RenderFromFile(inputFilePath, dataModel);
            File.WriteAllText(outputFilePath, result);
            return result;
        }

        private string ProcessForeach(string template, object dataModel)
        {
            var result = new StringBuilder();
            int pos = 0;

            while (true)
            {
                int startIndex = template.IndexOf("$foreach", pos, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1)
                {
                    result.Append(template.Substring(pos));
                    break;
                }

                result.Append(template.Substring(pos, startIndex - pos));

                int endCondition = template.IndexOf(')', startIndex);
                if (endCondition == -1) break;

                string header = template.Substring(startIndex, endCondition - startIndex + 1);
                var headerMatch = Regex.Match(header,
                    @"\$foreach\s*\(\s*var\s+(\w+)\s+in\s+([^)]+)\s*\)",
                    RegexOptions.IgnoreCase);

                if (!headerMatch.Success) break;

                string itemName = headerMatch.Groups[1].Value.Trim();
                string collectionPath = headerMatch.Groups[2].Value.Trim();

                int bodyStart = endCondition + 1;
                int searchPos = bodyStart;
                int nested = 0;
                int bodyEnd = -1;

                while (searchPos < template.Length)
                {
                    int nextForeach = template.IndexOf("$foreach", searchPos, StringComparison.OrdinalIgnoreCase);
                    int nextEndfor = template.IndexOf("$endfor", searchPos, StringComparison.OrdinalIgnoreCase);

                    if (nextEndfor == -1) break;

                    if (nextForeach != -1 && nextForeach < nextEndfor)
                    {
                        nested++;
                        searchPos = nextForeach + 1;
                    }
                    else
                    {
                        if (nested == 0)
                        {
                            bodyEnd = nextEndfor;
                            break;
                        }

                        nested--;
                        searchPos = nextEndfor + 1;
                    }
                }

                if (bodyEnd == -1) break;

                string loopBody = template.Substring(bodyStart, bodyEnd - bodyStart);

                var collection = GetValueByPath(dataModel, collectionPath) as System.Collections.IEnumerable;
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        var loopContext = new Dictionary<string, object>();

                        if (dataModel is IDictionary<string, object> dictParent)
                        {
                            foreach (var kv in dictParent)
                                loopContext[kv.Key] = kv.Value;
                        }
                        else
                        {
                            var props = dataModel.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                            foreach (var prop in props)
                                loopContext[prop.Name] = prop.GetValue(dataModel);
                        }

                        loopContext[itemName] = item;

                        var itemResult = ProcessForeach(loopBody, loopContext);
                        itemResult = ProcessIfElse(itemResult, loopContext);
                        itemResult = ProcessVariables(itemResult, loopContext);

                        result.Append(itemResult);
                    }
                }

                pos = bodyEnd + "$endfor".Length;
            }

            return result.ToString();
        }

        private string ProcessIfElse(string template, object dataModel)
        {
            var result = new StringBuilder();
            int pos = 0;

            while (true)
            {
                int startIndex = template.IndexOf("$if", pos, StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1)
                {
                    result.Append(template.Substring(pos));
                    break;
                }

                result.Append(template.Substring(pos, startIndex - pos));

                int endCondition = template.IndexOf(')', startIndex);
                if (endCondition == -1) break;

                string header = template.Substring(startIndex, endCondition - startIndex + 1);
                var headerMatch = Regex.Match(header, @"\$if\s*\(([^)]*)\)", RegexOptions.IgnoreCase);

                if (!headerMatch.Success) break;

                string condition = headerMatch.Groups[1].Value.Trim();
                int bodyStart = endCondition + 1;
                int searchPos = bodyStart;
                int nested = 0;
                int elseIndex = -1;
                int bodyEnd = -1;

                while (searchPos < template.Length)
                {
                    int nextIf = template.IndexOf("$if", searchPos, StringComparison.OrdinalIgnoreCase);
                    int nextElse = template.IndexOf("$else", searchPos, StringComparison.OrdinalIgnoreCase);
                    int nextEndif = template.IndexOf("$endif", searchPos, StringComparison.OrdinalIgnoreCase);

                    if (nextEndif == -1) break;

                    if (nextIf != -1 && nextIf < nextEndif && (nextElse == -1 || nextIf < nextElse))
                    {
                        nested++;
                        searchPos = nextIf + 1;
                    }
                    else if (nextElse != -1 && nextElse < nextEndif && nested == 0 && elseIndex == -1)
                    {
                        elseIndex = nextElse;
                        searchPos = nextElse + 1;
                    }
                    else
                    {
                        if (nested == 0)
                        {
                            bodyEnd = nextEndif;
                            break;
                        }

                        nested--;
                        searchPos = nextEndif + 1;
                    }
                }

                if (bodyEnd == -1) break;

                string truePart;
                string falsePart = string.Empty;

                if (elseIndex != -1)
                {
                    truePart = template.Substring(bodyStart, elseIndex - bodyStart);
                    falsePart = template.Substring(elseIndex + "$else".Length, bodyEnd - (elseIndex + "$else".Length));
                }
                else
                {
                    truePart = template.Substring(bodyStart, bodyEnd - bodyStart);
                }

                bool isTrue = EvaluateCondition(dataModel, condition);
                string chosenPart = isTrue ? truePart : falsePart;

                var processed = ProcessIfElse(chosenPart, dataModel);
                processed = ProcessVariables(processed, dataModel);

                result.Append(processed);
                pos = bodyEnd + "$endif".Length;
            }

            return result.ToString();
        }

        private bool EvaluateCondition(object dataModel, string condition)
        {
            var parts = condition.Split(new[] { "==" }, StringSplitOptions.None);

            if (parts.Length == 2)
            {
                var propName = parts[0].Trim();
                var expectedValue = parts[1].Trim().Trim('"');
                var actualValue = GetValueByPath(dataModel, propName)?.ToString();
                return actualValue == expectedValue;
            }

            var condValue = GetValueByPath(dataModel, condition);

            if (condValue is bool boolValue) return boolValue;
            if (condValue != null) return true;

            return false;
        }

        private string ProcessVariables(string template, object dataModel)
        {
            var varRegex = new Regex(@"\$\{([A-Za-z_][A-Za-z0-9_\.]*)\}");

            return varRegex.Replace(template, match =>
            {
                string path = match.Groups[1].Value;
                var value = GetValueByPath(dataModel, path);
                return value?.ToString() ?? string.Empty;
            });
        }

        public static object? GetValueByPath(object obj, string path)
        {
            if (obj == null || string.IsNullOrEmpty(path))
                return null;

            object? current = obj;

            foreach (var part in path.Split('.'))
            {
                if (current == null)
                    return null;

                if (current is IDictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(part, out current))
                        return null;
                }
                else
                {
                    var type = current.GetType();

                    var prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop != null)
                    {
                        current = prop.GetValue(current);
                    }
                    else
                    {
                        var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (field != null)
                            current = field.GetValue(current);
                        else
                            return null;
                    }
                }
            }

            return current;
        }
    }


}
