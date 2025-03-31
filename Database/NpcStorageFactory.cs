namespace PigNPC.Database;

public static class NpcStorageFactory
{
    public static INpcStorageProvider Create(string type, string connectionStringOrPath)
    {
        return type.ToLowerInvariant() switch
        {
            "sqlite" => new SqliteDatabase(connectionStringOrPath),
            "mysql"  => new MySqlDatabase(connectionStringOrPath),
            "json"   => new JsonDatabase(connectionStringOrPath),
            _ => throw new ArgumentException($"Unsupported storage type: {type}")
        };
    }
}
