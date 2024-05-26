namespace NovelsCollector.SDK
{
    public interface IPlugin
    {
        string Name { get; }
        string ExecuteCommand();

    }
}
