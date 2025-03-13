using System;
using System.Linq;
using System.Reflection;
using System.Text;
using ClassicUO;

public static class GenDoc
{
    public static string GenerateMarkdown(Type type)
    {
        var sb = new StringBuilder();

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
            }
        }
        else
        {
            sb.AppendLine("_No properties found._");
        }
        sb.AppendLine();

        // List methods
        sb.AppendLine("## Methods");
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
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
            }
        }
        else
        {
            sb.AppendLine("_No methods found._");
        }

        return sb.ToString();
    }

    private static void GenReturnType(Type returnType, ref StringBuilder sb)
    {
        if (returnType != typeof(void))
        {
            sb.AppendLine($"#### Return Type: *{returnType.Name}*");
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
}
