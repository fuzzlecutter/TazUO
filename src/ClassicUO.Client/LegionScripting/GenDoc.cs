using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ClassicUO;

public static class GenDoc
{
    public static string GenerateMarkdown(Type type, out StringBuilder python)
    {
        var sb = new StringBuilder();
        python = new StringBuilder();
        // Add class name
        sb.AppendLine($"# {type.Name}  ");
        sb.AppendLine($"This was automatically generated on version `v{CUOEnviroment.Version}`.  ");
        sb.AppendLine();

        // Add class description
        sb.AppendLine("## Class Description");
        sb.AppendLine($"This class represents the **{type.Name}** type.  ");
        sb.AppendLine();

        // List properties
        sb.AppendLine("## Properties");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Any())
        {
            foreach (var property in properties)
            {
                sb.AppendLine($"- **{property.Name}** (*{property.PropertyType.Name}*)");
                python.AppendLine($"{property.Name} = None");
            }
        }
        else
        {
            sb.AppendLine("_No properties found._");
        }
        sb.AppendLine();

        GenEnums(type, ref sb, ref python);

        // List methods
        sb.AppendLine("## Methods");
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName); ;
        if (methods.Any())
        {
            foreach (var method in methods)
            {
                sb.AppendLine();
                sb.Append($"### {method.Name}");
                GenParametersParenthesis(method.GetParameters(), ref sb);
                sb.AppendLine();

                GenParameters(method.GetParameters(), ref sb);

                GenReturnType(method.ReturnType, ref sb);

                sb.AppendLine("***");
                sb.AppendLine();

                python.AppendLine($"def {method.Name}({GetPythonParameters(method.GetParameters())}):");
                python.AppendLine($"    pass");
                python.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("_No methods found._");
        }

        return sb.ToString();
    }

    private static void GenEnums(Type type, ref StringBuilder sb, ref StringBuilder python)
    {
        // List enums
        sb.AppendLine("## Enums");
        var enums = type.GetNestedTypes(BindingFlags.Public).Where(t => t.IsEnum);
        if (enums.Any())
        {
            foreach (var enumType in enums)
            {
                sb.AppendLine($"### {enumType.Name}");
                sb.AppendLine("| Name | Value |");
                sb.AppendLine("| --- | --- |");
                foreach (var enumValue in Enum.GetValues(enumType))
                {
                    sb.AppendLine($"| {enumValue} | {(byte)enumValue} |");
                }
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("_No enums found._");
        }
        sb.AppendLine();
    }
    private static void GenReturnType(Type returnType, ref StringBuilder sb)
    {
        if (returnType != typeof(void))
        {
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Tuple<,>))
            {
                sb.AppendLine();
                sb.Append("#### Return Type: *Tuple(");

                var genericArguments = returnType.GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    sb.Append($"{genericArguments[i].Name}, ");
                }
                if (genericArguments.Length > 0) sb.Remove(sb.Length - 2, 2);
                sb.Append(")*");
            }
            else
            {
                sb.AppendLine($"#### Return Type: *{returnType.Name}*");
            }
        }
        else
        {
            sb.AppendLine("#### Does not return anything");
        }
        sb.AppendLine();
    }

    private static void GenParameters(ParameterInfo[] parameters, ref StringBuilder sb)
    {
        if (parameters.Length == 0) return;

        sb.AppendLine("**Parameters**  ");
        sb.AppendLine("| Name | Type | Optional |");
        sb.AppendLine("| --- | --- | --- |");

        string optional = "";

        foreach (var param in parameters)
        {
            if (param.IsOptional) optional = "*";
            else optional = "";

            sb.Append("| ");

            sb.Append($"{optional}{param.Name}{optional} | {optional}{param.ParameterType.Name}{optional}");

            sb.Append($" | {(param.IsOptional ? "YES" : "No")} |");
            sb.AppendLine();
        }
    }

    private static void GenParametersParenthesis(ParameterInfo[] parameters, ref StringBuilder sb)
    {
        sb.Append("(");

        string optional = "";
        foreach (var param in parameters)
        {
            if (param.IsOptional) optional = "*";
            else optional = "";

            sb.Append($"{optional}{param.Name}{optional}, ");
        }

        if (parameters.Length > 0) sb.Remove(sb.Length - 2, 2);

        sb.Append(")");
    }
    private static string GetPythonParameters(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0) return "";

        var sb = new StringBuilder();
        foreach (var param in parameters)
        {
            sb.Append($"{param.Name}");
            if (param.IsOptional) sb.Append("=None");
            sb.Append(", ");
        }
        sb.Remove(sb.Length - 2, 2);

        return sb.ToString();
    }
}
