namespace BrighterTools.CodeGenerator.Runtime;

internal sealed class GeneratedFileWriteResult
{
    public int WrittenCount { get; set; }
    public int SkippedExistingCount { get; set; }

    public void Add(GeneratedFileWriteResult other)
    {
        WrittenCount += other.WrittenCount;
        SkippedExistingCount += other.SkippedExistingCount;
    }
}
