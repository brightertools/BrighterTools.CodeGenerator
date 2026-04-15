namespace BrighterTools.CodeGenerator.Metadata;

public static class TemplateModelFactory
{
    public static object CreateModel(GenerationContext context, ClassMetadata model, string artifactName, string outputPath)
    {
        var userIdType = ResolveUserIdType(context.AllModels);
        var primaryKeyType = ResolvePrimaryKeyType(model);

        return new
        {
            tool = new
            {
                name = context.Options.ToolName,
                version = context.Options.ToolVersion,
                generated_at = context.GeneratedAt.ToString("O")
            },
            options = context.Options,
            model,
            models = context.Models,
            enums = context.Enums,
            artifact = new
            {
                name = artifactName,
                output_path = outputPath,
                primary_key_type = primaryKeyType,
                user_id_type = userIdType
            }
        };
    }

    private static string ResolveUserIdType(IReadOnlyList<ClassMetadata> models)
    {
        var userModel = models.FirstOrDefault(x => x.Name.Equals("User", StringComparison.OrdinalIgnoreCase));
        return userModel is null ? "int" : ResolvePrimaryKeyType(userModel);
    }

    private static string ResolvePrimaryKeyType(ClassMetadata model)
    {
        var idProperty = model.Properties.FirstOrDefault(x => x.Name == "Id");
        if (idProperty is null)
        {
            return "int";
        }

        return idProperty.DisplayTypeName is "long" or "System.Int64" ? "long" : "int";
    }
}