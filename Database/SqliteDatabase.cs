using System.Data;
using System.Data.SQLite;
using Newtonsoft.Json;
using PigNet.Utils.Skins;
using PigNPC.Npc;

namespace PigNPC.Database;

public class SqliteDatabase : INpcStorageProvider
{
    private readonly string _connectionString;

    public SqliteDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        if (!File.Exists(databasePath))
            SQLiteConnection.CreateFile(databasePath);

        _connectionString = $"Data Source={databasePath};Version=3;";
        EnsureTablesCreated();
    }

    private void EnsureTablesCreated()
    {
        using var connection = new SQLiteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Npcs (
            Id TEXT PRIMARY KEY,
            NameTag TEXT NOT NULL,
            LevelName TEXT NOT NULL,
            X REAL,
            Y REAL,
            Z REAL,
            Pitch REAL,
            Yaw REAL,
            HeadYaw REAL,
            Scale REAL,
            SkinJson TEXT NOT NULL,
            IsVisible INTEGER NOT NULL,
            IsAlwaysShowName INTEGER NOT NULL,
            ActionId TEXT,
            SkinType SMALLINT NOT NULL DEFAULT 0,
            DisplayName TEXT NOT NULL
        );
        """;
        command.ExecuteNonQuery();
    }

    public async Task<IEnumerable<NpcData>> LoadAllAsync()
    {
        var list = new List<NpcData>();

        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Npcs;";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(ReadNpc(reader));
        }

        return list;
    }

    public async Task<NpcData?> GetByIdAsync(string npcId)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Npcs WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", npcId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadNpc(reader);

        return null;
    }

    public async Task SaveAsync(NpcData npc)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText =
        """
        INSERT OR REPLACE INTO Npcs
        (Id, NameTag, LevelName, X, Y, Z, Pitch, Yaw, HeadYaw,
         Scale, SkinJson,
         IsVisible, IsAlwaysShowName, ActionId, SkinType, DisplayName)
        VALUES
        (@Id, @NameTag, @LevelName, @X, @Y, @Z, @Pitch, @Yaw, @HeadYaw,
         @Scale, @SkinJson,
         @IsVisible, @IsAlwaysShowName, @ActionId, @SkinType, @DisplayName);
        """;

        BindNpc(command, npc);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string npcId)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Npcs WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", npcId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveAllAsync(IEnumerable<NpcData> npcs)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = connection.BeginTransaction();

        foreach (var npc in npcs)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
            """
            INSERT OR REPLACE INTO Npcs
            (Id, NameTag, LevelName, X, Y, Z, Pitch, Yaw, HeadYaw,
             Scale, SkinJson,
             IsVisible, IsAlwaysShowName, ActionId, SkinType, DisplayName)
            VALUES
            (@Id, @NameTag, @LevelName, @X, @Y, @Z, @Pitch, @Yaw, @HeadYaw,
             @Scale, @SkinJson,
             @IsVisible, @IsAlwaysShowName, @ActionId, @SkinType, @DisplayName);
            """;

            BindNpc(command, npc);
            await command.ExecuteNonQueryAsync();
        }

        transaction.Commit();
    }

    private static NpcData ReadNpc(IDataRecord reader)
    {
        return new NpcData
        {
            Id = reader.GetString(0),
            NameTag = reader.GetString(1),
            LevelName = reader.GetString(2),
            X = reader.GetDouble(3),
            Y = reader.GetDouble(4),
            Z = reader.GetDouble(5),
            Pitch = (float)reader.GetDouble(6),
            Yaw = (float)reader.GetDouble(7),
            HeadYaw = (float)reader.GetDouble(8),
            Scale = (float)reader.GetDouble(9),
            Skin = Skin.FromJson(reader.GetString(10)),
            IsVisible = reader.GetInt32(11) != 0,
            IsAlwaysShowName = reader.GetInt32(12) != 0,
            ActionId = reader.IsDBNull(13) ? null : reader.GetString(13),
            SkinType = (PigNpcLoader.SkinType)reader.GetInt16(14),
            DisplayName = reader.GetString(15)
        };
    }

    private static void BindNpc(SQLiteCommand command, NpcData npc)
    {
        command.Parameters.AddWithValue("@Id", npc.Id);
        command.Parameters.AddWithValue("@NameTag", npc.NameTag);
        command.Parameters.AddWithValue("@LevelName", npc.LevelName);
        command.Parameters.AddWithValue("@X", npc.X);
        command.Parameters.AddWithValue("@Y", npc.Y);
        command.Parameters.AddWithValue("@Z", npc.Z);
        command.Parameters.AddWithValue("@Pitch", npc.Pitch);
        command.Parameters.AddWithValue("@Yaw", npc.Yaw);
        command.Parameters.AddWithValue("@HeadYaw", npc.HeadYaw);
        command.Parameters.AddWithValue("@Scale", npc.Scale);
        command.Parameters.AddWithValue("@SkinJson", JsonConvert.SerializeObject(npc.Skin));
        command.Parameters.AddWithValue("@IsVisible", npc.IsVisible ? 1 : 0);
        command.Parameters.AddWithValue("@IsAlwaysShowName", npc.IsAlwaysShowName ? 1 : 0);
        command.Parameters.AddWithValue("@ActionId", (object?)npc.ActionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@SkinType", (short)npc.SkinType);
        command.Parameters.AddWithValue("@DisplayName", npc.DisplayName);
    }
}