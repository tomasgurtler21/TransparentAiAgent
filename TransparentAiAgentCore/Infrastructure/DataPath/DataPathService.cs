namespace TransparentAiAgentCore.Infrastructure.DataPath;

/// <summary>
/// Implementation of IDataPathService that uses OS-appropriate user data directories.
/// </summary>
public class DataPathService : IDataPathService
{
    private readonly string _userDataRoot;

    public DataPathService()
    {
        // Use ApplicationData for roaming user data
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _userDataRoot = Path.Combine(appData, "TransparentAiAgent");
    }

    public string GetUserDataRoot() => _userDataRoot;

    public string GetMemoryDirectory() =>
        Path.Combine(_userDataRoot, "memory");

    public string GetConversationsDirectory() =>
        Path.Combine(_userDataRoot, "conversations");

    public string GetLogsDirectory() =>
        Path.Combine(_userDataRoot, "logs");

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(GetMemoryDirectory());
        Directory.CreateDirectory(GetConversationsDirectory());
        Directory.CreateDirectory(GetLogsDirectory());
    }
}
