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
                version = context.Options.ToolVersion
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
                primary_key_is_string = primaryKeyType == "string",
                primary_key_is_guid = primaryKeyType == "Guid",
                user_id_type = userIdType,
                key_properties = context.Options.PersistenceKeys.TryGetValue(model.Name, out var keys)
                    ? keys.Select(name => model.Properties.Single(p => p.Name == name)).ToArray()
                    : model.Properties.Where(p => p.Name == "Id").ToArray()
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

        return idProperty.NonNullableDisplayType switch
        {
            "int" or "System.Int32" => "int",
            "long" or "System.Int64" => "long",
            "string" or "System.String" => "string",
            "Guid" or "System.Guid" => "Guid",
            var unsupported => throw new InvalidOperationException($"Unsupported primary key type '{unsupported}' on {model.Name}. Configure a supported key contract before generating persistence.")
        };
    }
}


