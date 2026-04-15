using Humanizer;
using Scriban;
using Scriban.Runtime;

namespace BrighterTools.CodeGenerator.TemplateEngine;

public sealed class TemplateRenderer(TemplateLoader templateLoader)
{
    public string Render(string templateName, object model)
    {
        var source = templateLoader.Load(templateName);
        var template = Template.Parse(source, templateName);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Failed to parse template '{templateName}': {string.Join(Environment.NewLine, template.Messages)}");
        }

        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name,
            LoopLimit = 100_000
        };

        var builtins = new ScriptObject();
        builtins.Import(model, renamer: member => member.Name);
        builtins.Import(new StringTemplateFunctions(), renamer: member => member.Name);
        builtins.Import(new CodeTemplateFunctions(), renamer: member => member.Name);
        context.PushGlobal(builtins);

        return template.Render(context);
    }

    private sealed class StringTemplateFunctions
    {
        public string upper_first(string value) => string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
        public string lower_first(string value) => string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
        public string lowercase(string value) => string.IsNullOrEmpty(value) ? value : value.ToLowerInvariant();
        public string pluralize(string value) => value.Pluralize();
        public string singularize(string value) => value.Singularize(false);
        public string trim_nullable(string value) => value.TrimEnd('?');
        public string replace(string value, string oldValue, string newValue) => value.Replace(oldValue, newValue, StringComparison.Ordinal);
    }

    private sealed class CodeTemplateFunctions
    {
        public string file_header(string toolName, string toolVersion, string generatedAt) =>
            $@"// ----------------------------------------------------------------------------------------------
// This file was generated using {toolName}
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// Generated on {generatedAt}
// ----------------------------------------------------------------------------------------------
";

        public string dto_type(string typeName, bool isNullable, bool makeNullable)
        {
            var normalized = typeName switch
            {
                "System.String" => "string",
                "System.Guid" => "Guid",
                "System.DateTime" => "DateTime",
                "System.DateTimeOffset" => "DateTimeOffset",
                _ => typeName
            };

            var needsNullableSuffix = makeNullable || isNullable;
            if (normalized.EndsWith("?", StringComparison.Ordinal))
            {
                return normalized;
            }

            return normalized switch
            {
                "string" => needsNullableSuffix ? "string?" : "string",
                "int" or "long" or "short" or "bool" or "double" or "decimal" or "float" or "Guid" or "DateTime" or "DateTimeOffset" => normalized + (needsNullableSuffix ? "?" : string.Empty),
                _ => normalized + (needsNullableSuffix && !normalized.EndsWith("?", StringComparison.Ordinal) ? "?" : string.Empty)
            };
        }

        public string string_initializer(string typeName, bool isNullable) =>
            (typeName is "string" or "System.String") && !isNullable ? " = \"\";" : string.Empty;
    }
}
