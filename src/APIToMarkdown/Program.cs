using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public static class GenDoc
{
    public static string GenerateMarkdown(string filePath, out StringBuilder python)
    {
        var sb = new StringBuilder();
        python = new StringBuilder();

        var code = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot() as CompilationUnitSyntax;

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        sb.AppendLine("# Python API Documentation  ");
        sb.AppendLine("This is automatically generated documentation for the Python API scripting.  ");
        sb.AppendLine("All methods, properties, enums, etc need to pre prefaced with `API.` for example: `API.Msg(\"An example\")`.  ");
        sb.AppendLine("  ");
        sb.AppendLine("If you download the [API.py](API.py) file, put it in the same folder as your python scripts and add `import API` to your script, that will enable some mild form of autocomplete in an editor like VS Code.  ");
        sb.AppendLine("You can now type `-updateapi` in game to download the latest API.py file.  ");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("[Additional notes](notes.md)  ");
        sb.AppendLine("  ");

        sb.AppendLine($"This was generated on `{DateTime.Now.Date.ToShortDateString()}`.");

        foreach (var classDeclaration in classes)
        {
            // Add class name
            sb.AppendLine($"# {classDeclaration.Identifier.Text}  ");
            sb.AppendLine();

            // Add class description
            var classSummary = GetXmlSummary(classDeclaration);
            if (!string.IsNullOrEmpty(classSummary))
            {
                sb.AppendLine("## Class Description");
                sb.AppendLine(classSummary);
                sb.AppendLine();
            }

            // List properties
            sb.AppendLine("## Properties");
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
            if (properties.Any())
            {
                foreach (var property in properties)
                {
                    if (!property.Modifiers.Any(SyntaxKind.PublicKeyword))
                        continue;

                    var propertySummary = GetXmlSummary(property);
                    sb.AppendLine($"- **{property.Identifier.Text}** (*{property.Type}*)");
                    if (!string.IsNullOrEmpty(propertySummary))
                    {
                        sb.AppendLine($"  - {propertySummary}");
                    }
                    python.AppendLine($"{property.Identifier.Text} = None");
                }
            }
            else
            {
                sb.AppendLine("_No properties found._");
            }
            sb.AppendLine();

            // List enums
            sb.AppendLine("## Enums");
            var enums = classDeclaration.Members.OfType<EnumDeclarationSyntax>();
            if (enums.Any())
            {
                foreach (var enumDeclaration in enums)
                {
                    if (!enumDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
                        continue;

                    python.AppendLine();
                    python.AppendLine($"class {enumDeclaration.Identifier.Text}:");

                    sb.AppendLine($"### {enumDeclaration.Identifier.Text}");
                    sb.AppendLine();

                    var enumSummary = GetXmlSummary(enumDeclaration);
                    if (!string.IsNullOrEmpty(enumSummary))
                    {
                        sb.AppendLine(enumSummary);
                        sb.AppendLine();
                    }

                    sb.AppendLine("**Values:**");
                    byte last = 0;
                    foreach (var member in enumDeclaration.Members)
                    {
                        sb.AppendLine($"- {member.Identifier.Text}");

                        var value = last += 1;
                        if (member.EqualsValue?.Value.ToString() != null)
                        {
                            if (byte.TryParse(member.EqualsValue?.Value.ToString(), out last))
                                value = last;
                        }
                        python.AppendLine($"    {member.Identifier.Text} = {value}");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("_No enums found._");
            }
            python.AppendLine();
            sb.AppendLine();

            // List methods
            sb.AppendLine("## Methods");
            var methods = classDeclaration.Members.OfType<MethodDeclarationSyntax>();
            if (methods.Any())
            {
                foreach (var method in methods)
                {
                    if (!method.Modifiers.Any(SyntaxKind.PublicKeyword))
                        continue;

                    var methodSummary = GetXmlSummary(method);
                    sb.AppendLine();
                    sb.AppendLine("<details>");
                    sb.Append($"<summary><h3>{method.Identifier.Text}");
                    GenParametersParenthesis(method.ParameterList.Parameters, ref sb);
                    sb.Append("</h3></summary>");
                    sb.AppendLine();
                    sb.AppendLine();

                    if (!string.IsNullOrEmpty(methodSummary))
                    {
                        var summaryWithBreaks = string.Join("\n", methodSummary
                            .Split('\n')
                            .Select(line => line.TrimEnd() + "  "));
                        sb.AppendLine(summaryWithBreaks);
                        //sb.AppendLine(methodSummary);
                        sb.AppendLine();
                    }

                    GenParameters(method.ParameterList.Parameters, ref sb, method);

                    GenReturnType(method.ReturnType, ref sb);

                    sb.AppendLine("</details>");
                    sb.AppendLine();

                    sb.AppendLine("***");
                    sb.AppendLine();

                    python.AppendLine($"def {method.Identifier.Text}({GetPythonParameters(method.ParameterList.Parameters)})"
                     + $" -> {MapCSharpTypeToPython(method.ReturnType.ToString())}:");
                    if (!string.IsNullOrWhiteSpace(methodSummary))
                    {
                        // Indent and escape triple quotes in summary if present
                        var pyDoc = methodSummary.Replace("\"\"\"", "\\\"\\\"\\\"");
                        var indentedDoc = string.Join("\n", pyDoc.Split('\n').Select(line => "    " + line.TrimEnd()));
                        python.AppendLine("    \"\"\"");
                        python.AppendLine(indentedDoc);
                        python.AppendLine("    \"\"\"");
                    }
                    python.AppendLine($"    pass");
                    python.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("_No methods found._");
            }
        }

        return sb.ToString();
    }

    private static string GetXmlSummary(SyntaxNode node)
    {
        var trivia = node.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (trivia != null)
        {
            var summary = trivia.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "summary");

            if (summary != null)
            {
                string rawText = string.Join(" ", summary.Content.Select(c => c.ToString().Trim()));

                // 2. Remove any potential leftover XML comment markers and trim ends
                //rawText = rawText.Replace("///", "").Trim();

                // 3. Split by space, remove empty results, join with single space
                //string cleanedText = string.Join(" ", rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                string cleanedDocumentation = Regex.Replace(
                        rawText,
                        @"^\s*(///.*)$",  // The pattern to find
                        "$1",             // The replacement string (content of group 1)
                        RegexOptions.Multiline // Treat ^ and $ as start/end of LINE
                    );

                return cleanedDocumentation.Replace("///", "");
            }
        }

        return string.Empty;
    }

    private static void GenReturnType(TypeSyntax returnType, ref StringBuilder sb)
    {
        if (returnType.ToString() != "void")
        {
            sb.AppendLine($"#### Return Type: *{returnType}*");
        }
        else
        {
            sb.AppendLine("#### Does not return anything");
        }
        sb.AppendLine();
    }

    private static void GenParameters(SeparatedSyntaxList<ParameterSyntax> parameters, ref StringBuilder sb, SyntaxNode methodNode)
    {
        if (parameters.Count == 0) return;

        sb.AppendLine("**Parameters**  ");
        sb.AppendLine("| Name | Type | Optional | Description |");
        sb.AppendLine("| --- | --- | --- | --- |");

        foreach (var param in parameters)
        {
            var isOptional = param.Default != null ? "Yes" : "No";
            var paramSummary = GetXmlParamSummary(methodNode, param.Identifier.Text);
            sb.AppendLine($"| {param.Identifier.Text} | {param.Type} | {isOptional} | {paramSummary} |");
        }
    }

    private static string GetXmlParamSummary(SyntaxNode methodNode, string paramName)
    {
        var trivia = methodNode.GetLeadingTrivia()
            .Select(i => i.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        if (trivia != null)
        {
            var paramElement = trivia.Content
                .OfType<XmlElementSyntax>()
                .FirstOrDefault(e => e.StartTag.Name.LocalName.Text == "param" &&
                                     e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>()
                                     .Any(a => a.Identifier.Identifier.Text == paramName));

            if (paramElement != null)
            {
                string r = string.Join(" ", paramElement.Content.Select(c => c.ToString().Trim()));
                r = r.Replace("///", "").Trim()
                  .Replace("\n", "  \n");
                return r;
            }
        }

        return string.Empty;
    }

    private static void GenParametersParenthesis(SeparatedSyntaxList<ParameterSyntax> parameters, ref StringBuilder sb)
    {
        sb.Append("(");

        foreach (var param in parameters)
        {
            sb.Append($"{param.Identifier.Text}, ");
        }

        if (parameters.Count > 0) sb.Remove(sb.Length - 2, 2);

        sb.Append(")");
    }

    private static string GetPythonParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        if (parameters.Count == 0) return "";

        var sb = new StringBuilder();
        foreach (var param in parameters)
        {
            string pythonType = MapCSharpTypeToPython(param.Type.ToString());

            string defaultValue = param.Default != null ? $" = {MapDefaultToPython(param.Default.ToString())}" : string.Empty;

            sb.Append($"{param.Identifier.Text}: {pythonType}{defaultValue}, ");
        }
        sb.Remove(sb.Length - 2, 2);

        return sb.ToString();
    }

    private static string MapDefaultToPython(string defaultValue)
    {
        defaultValue = defaultValue.Replace("=", "").Trim();

        if (defaultValue != "false")
            defaultValue = defaultValue.Replace("f", ""); //Remove f suffix from float literals

        // Map C# default values to Python
        return defaultValue.Trim() switch
        {
            "uint.MaxValue" => "1337",
            "ushort.MaxValue" => "1337",
            "int.MinValue" => "1337",
            "true" => "True",
            "false" => "False",
            "null" => "None",
            _ => defaultValue // Keep the original value if not mapped
        };
    }

    private static string MapCSharpTypeToPython(string csharpType)
    {
        // Trim whitespace just in case
        csharpType = csharpType.Trim();

        // 1. Handle array types (e.g., int[], string[], MyClass[])
        if (csharpType.EndsWith("[]"))
        {
            // Get the element type (e.g., "int" from "int[]")
            string elementType = csharpType.Substring(0, csharpType.Length - 2);
            // Recursively map the element type
            string pythonElementType = MapCSharpTypeToPython(elementType);
            // Use modern Python list hint syntax: list[T]
            return $"list[{pythonElementType}]";
        }

        // 2. Handle common generic collection types (List<T>, IEnumerable<T>, etc.)
        // This uses basic string parsing; more robust parsing might be needed for complex cases.
        string[] collectionPrefixes = {
        "List<", "IList<", "IEnumerable<", "ICollection<", "Collection<",
        "System.Collections.Generic.List<",
        "System.Collections.Generic.IList<",
        "System.Collections.Generic.IEnumerable<",
        "System.Collections.Generic.ICollection<",
        "System.Collections.ObjectModel.Collection<"
    };

        // Check if the type starts with one of the prefixes and ends with ">"
        string matchedPrefix = collectionPrefixes.FirstOrDefault(prefix => csharpType.StartsWith(prefix));
        if (matchedPrefix != null && csharpType.EndsWith(">"))
        {
            // Extract the element type T from Collection<T>
            int openBracketIndex = matchedPrefix.Length - 1; // Index of '<'
            int closeBracketIndex = csharpType.Length - 1;   // Index of '>'

            if (closeBracketIndex > openBracketIndex)
            {
                string elementType = csharpType.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();
                // Recursively map the element type
                string pythonElementType = MapCSharpTypeToPython(elementType);
                // Use modern Python list hint syntax: list[T]
                return $"list[{pythonElementType}]";
            }
        }

        // 3. Handle Nullable<T> or T?
        if (csharpType.EndsWith("?") || csharpType.StartsWith("Nullable<") || csharpType.StartsWith("System.Nullable<"))
        {
            string underlyingType;
            if (csharpType.EndsWith("?"))
            {
                underlyingType = csharpType.Substring(0, csharpType.Length - 1);
            }
            else // StartsWith("Nullable<") or StartsWith("System.Nullable<")
            {
                int openBracket = csharpType.IndexOf('<');
                int closeBracket = csharpType.LastIndexOf('>');
                if (openBracket != -1 && closeBracket > openBracket)
                {
                    underlyingType = csharpType.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
                }
                else
                {
                    underlyingType = "object"; // Fallback
                }
            }
            string pythonUnderlyingType = MapCSharpTypeToPython(underlyingType);
            // Use Python 3.10+ Union syntax: T | None
            return $"{pythonUnderlyingType} | None";
        }


        // 4. Handle base types (add more as needed)
        // Include fully qualified names if they might appear from ToString()
        return csharpType switch
        {
            "int" or "Int32" or "System.Int32" => "int",
            "uint" or "UInt32" or "System.UInt32" => "int", // Map unsigned to int
            "short" or "Int16" or "System.Int16" => "int",
            "ushort" or "UInt16" or "System.UInt16" => "int",
            "long" or "Int64" or "System.Int64" => "int",
            "ulong" or "UInt64" or "System.UInt64" => "int",
            "byte" or "Byte" or "System.Byte" => "int", // Map C# byte to Python int
            "sbyte" or "SByte" or "System.SByte" => "int",
            "string" or "String" or "System.String" => "str",
            "char" or "Char" or "System.Char" => "str", // Map C# char to Python str
            "bool" or "Boolean" or "System.Boolean" => "bool",
            "double" or "Double" or "System.Double" => "float",
            "float" or "Single" or "System.Single" => "float", // C# float is System.Single
            "decimal" or "Decimal" or "System.Decimal" => "float", // Or use Python's Decimal type
            "object" or "Object" or "System.Object" => "Any", // Requires 'from typing import Any'
            "void" or "System.Void" => "None", // Typically for return types

            // Add specific mappings for other common types if desired
            "DateTime" or "System.DateTime" => "datetime", // Requires 'import datetime'
            "Guid" or "System.Guid" => "str", // Often represented as string or UUID

            "Gump" => "Gump", // Custom types
            "Control" or "ScrollArea" or "SimpleProgressBar" or "TextBox" or "TTFTextInputField" or "GumpPic" => "Control",
            "RadioButton" or "NiceButton" or "Button" or "ResizableStaticPic" or "AlphaBlendControl" => "Control",
            "Label" or "Checkbox" => "Control",
            "Item" => "Item",
            "Mobile" => "Mobile",
            "Skill" => "Skill",
            "Buff" => "Buff",
            "ScanType" => "ScanType",
            "Notoriety" => "Notoriety",
            "GameObject" => "GameObject",

            // Fallback for unknown types
            _ => "Any"
        };
    }
}

class Program
{
    static void Main(string[] args)
    {
        string filePath;
        bool sameDir = true;

        if (args.Length > 0)
        {
            filePath = args[0];
            if (args.Length > 1)
                sameDir = args[1].ToLower() != "n";
        }
        else
        {
            Console.WriteLine("Enter a cs file path: ");
            filePath = Console.ReadLine();

            Console.WriteLine("Save output files to same directory as cs file? ([y]/n)");
            string saveToSameDirectory = Console.ReadLine();
            if (!string.IsNullOrEmpty(saveToSameDirectory) && saveToSameDirectory.ToLower() == "n")
            {
                sameDir = false;
            }
        }

        if (string.IsNullOrEmpty(filePath)) return;
        
        if(!File.Exists(filePath)) return;

        var markdown = GenDoc.GenerateMarkdown(filePath, out var python);
        Console.WriteLine("Saved to doc.md and API.py");

        string path = string.Empty;
        if (sameDir)
        {
            path = Path.GetDirectoryName(filePath);
        }

        File.WriteAllText(Path.Combine(path, "doc.md"), markdown);
        File.WriteAllText(Path.Combine(path, "API.py"), python.ToString());
    }
}
