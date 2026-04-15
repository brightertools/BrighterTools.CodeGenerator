using System.Collections.Concurrent;

namespace BrighterTools.CodeGenerator.TemplateEngine;

public sealed class TemplateLoader(string templatesDirectory)
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);

    public string TemplatesDirectory { get; } = templatesDirectory;

    public string Load(string templateName)
    {
        return _cache.GetOrAdd(templateName, name =>
        {
            var path = Path.Combine(TemplatesDirectory, name);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Template '{name}' was not found.", path);
            }

            return File.ReadAllText(path);
        });
    }
}
