using Newtonsoft.Json;
using PigNet.Utils.Skins;
using PigNPC.Npc;
using MySql.Data.MySqlClient;
using System.Data;

namespace PigNPC.Database;

public class MySqlDatabase : INpcStorageProvider
{
    private readonly string _connectionString;

    public MySqlDatabase(string connectionString)
    {
        _connectionString = connectionString;
        EnsureTablesCreated();
    }

    private void EnsureTablesCreated()
    {
        using var connection = new MySqlConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
        """
        CREATE TABLE IF NOT EXISTS Npcs (
            Id VARCHAR(64) PRIMARY KEY,
            NameTag VARCHAR(64) NOT NULL,
            LevelName VARCHAR(64) NOT NULL,
            X DOUBLE,
            Y DOUBLE,
            Z DOUBLE,
            Pitch FLOAT,
            Yaw FLOAT,
            HeadYaw FLOAT,
            SkinJson LONGTEXT NOT NULL,
            IsVisible BOOLEAN NOT NULL,
            IsAlwaysShowName BOOLEAN NOT NULL,
            ActionId VARCHAR(128),
            SkinType SMALLINT NOT NULL DEFAULT 0,
            DisplayName VARCHAR(64) NOT NULL
        );
        """;
        command.ExecuteNonQuery();
    }

    public async Task<IEnumerable<NpcData>> LoadAllAsync()
    {
        var list = new List<NpcData>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("SELECT * FROM Npcs;", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(ReadNpc(reader));
        }

        return list;
    }

    public async Task<NpcData?> GetByIdAsync(string npcId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("SELECT * FROM Npcs WHERE Id = @id;", connection);
        command.Parameters.AddWithValue("@id", npcId);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return ReadNpc(reader);

        return null;
    }

    public async Task SaveAsync(NpcData npc)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand
        {
            Connection = connection,
            CommandText =
            """
            INSERT INTO Npcs (
                Id, NameTag, LevelName, X, Y, Z,
                Pitch, Yaw, HeadYaw,
                SkinJson,
                IsVisible, IsAlwaysShowName, ActionId,
                SkinType, DisplayName
            )
            VALUES (
                @Id, @NameTag, @LevelName, @X, @Y, @Z,
                @Pitch, @Yaw, @HeadYaw,
                @SkinJson,
                @IsVisible, @IsAlwaysShowName, @ActionId,
                @SkinType, @DisplayName
            )
            ON DUPLICATE KEY UPDATE
                NameTag = VALUES(NameTag),
                LevelName = VALUES(LevelName),
                X = VALUES(X),
                Y = VALUES(Y),
                Z = VALUES(Z),
                Pitch = VALUES(Pitch),
                Yaw = VALUES(Yaw),
                HeadYaw = VALUES(HeadYaw),
                SkinJson = VALUES(SkinJson),
                IsVisible = VALUES(IsVisible),
                IsAlwaysShowName = VALUES(IsAlwaysShowName),
                ActionId = VALUES(ActionId),
                SkinType = VALUES(SkinType),
                DisplayName = VALUES(DisplayName);
            """
        };

        BindNpc(command, npc);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(string npcId)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand("DELETE FROM Npcs WHERE Id = @id;", connection);
        command.Parameters.AddWithValue("@id", npcId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task SaveAllAsync(IEnumerable<NpcData> npcs)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var transaction = await connection.BeginTransactionAsync();

        foreach (var npc in npcs)
        {
            using var command = new MySqlCommand
            {
                Connection = connection,
                Transaction = (MySqlTransaction)transaction,
                CommandText =
                """
                INSERT INTO Npcs (
                    Id, NameTag, LevelName, X, Y, Z,
                    Pitch, Yaw, HeadYaw,
                    SkinJson,
                    IsVisible, IsAlwaysShowName, ActionId,
                    SkinType, DisplayName
                )
                VALUES (
                    @Id, @NameTag, @LevelName, @X, @Y, @Z,
                    @Pitch, @Yaw, @HeadYaw,
                    @SkinJson,
                    @IsVisible, @IsAlwaysShowName, @ActionId,
                    @SkinType, @DisplayName
                )
                ON DUPLICATE KEY UPDATE
                    NameTag = VALUES(NameTag),
                    LevelName = VALUES(LevelName),
                    X = VALUES(X),
                    Y = VALUES(Y),
                    Z = VALUES(Z),
                    Pitch = VALUES(Pitch),
                    Yaw = VALUES(Yaw),
                    HeadYaw = VALUES(HeadYaw),
                    SkinJson = VALUES(SkinJson),
                    IsVisible = VALUES(IsVisible),
                    IsAlwaysShowName = VALUES(IsAlwaysShowName),
                    ActionId = VALUES(ActionId),
                    SkinType = VALUES(SkinType),
                    DisplayName = VALUES(DisplayName);
                """
            };

            BindNpc(command, npc);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
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
            Pitch = reader.GetFloat(6),
            Yaw = reader.GetFloat(7),
            HeadYaw = reader.GetFloat(8),
            Skin = Skin.FromJson(reader.GetString(9)),
            IsVisible = reader.GetBoolean(10),
            IsAlwaysShowName = reader.GetBoolean(11),
            ActionId = reader.IsDBNull(12) ? null : reader.GetString(12),
            SkinType = (PigNpcLoader.SkinType)reader.GetInt16(13),
            DisplayName = reader.GetString(14)
        };
    }

    private static void BindNpc(MySqlCommand command, NpcData npc)
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
        command.Parameters.AddWithValue("@SkinJson", JsonConvert.SerializeObject(npc.Skin));
        command.Parameters.AddWithValue("@IsVisible", npc.IsVisible);
        command.Parameters.AddWithValue("@IsAlwaysShowName", npc.IsAlwaysShowName);
        command.Parameters.AddWithValue("@ActionId", (object?)npc.ActionId ?? DBNull.Value);
        command.Parameters.AddWithValue("@SkinType", (short)npc.SkinType);
        command.Parameters.AddWithValue("@DisplayName", npc.DisplayName);
    }
}