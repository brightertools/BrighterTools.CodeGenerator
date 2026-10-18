using BrighterTools.CodeGenerator.Metadata;

namespace BrighterTools.CodeGenerator.Runtime;

internal static class PersistenceModelValidator
{
    internal static void Validate(GeneratorOptions options, IReadOnlyList<ClassMetadata> models)
    {
        if (options.PersistenceMode is not ("crud" or "scoped-read")) throw new InvalidOperationException("Unknown persistenceMode.");
        if (options.EnabledGenerators.Count != 0 && !options.EnabledGenerators.Any(x => x is "repositories" or "services")) return;
        foreach (var model in models.Where(x => !x.IsBaseModel && !x.IsJsonModel && !x.IsJoinTable))
        {
            var keys = options.PersistenceMode == "scoped-read" && options.PersistenceKeys.TryGetValue(model.Name, out var configured) ? configured : ["Id"];
            if (keys.Length == 0 || keys.Distinct().Count() != keys.Length || keys.Any(k => !model.Properties.Any(p => p.Name == k && !p.IsNullable && !p.IsNavigation && !p.IsCollection)))
                throw new InvalidOperationException($"Persistence generation requires an explicit supported key contract for {model.Name}. Composite keys must not be inferred as int or replaced with new identifiers.");
        }
    }
}
