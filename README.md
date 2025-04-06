# PigNPC - Minecraft Bedrock NPC Plugin

**PigNPC** is a powerful and extensible plugin for [PigNet](https://github.com/Pigraid-Games/PigNet) that enables the creation and control of **non-player characters (NPCs)** in Minecraft Bedrock Edition.

> Designed with flexibility in mind, NPCs can have custom skins, execute actions, and be stored in JSON, SQLite, or MySQL databases.

---

## ✨ Features

- ✏️ Create NPCs with player or custom skins
- 🌐 Supports `.png` skin textures and `.json` geometry files
- ⚙️ Store NPCs in **JSON**, **SQLite**, or **MySQL**
- ⚔️ Bind actions to NPCs triggered on interaction
- ℹ️ Rich in-game command system with help menu
- ✨ Automatically reload and spawn NPCs on player join

---

## 📓 Installation

1. Place the `PigNPC.dll` into your server's `Plugins/` folder.
2. Ensure `npc_skins/` folder exists in the plugin directory.
3. Configure your preferred storage type (optional):
   - Defaults to JSON in `/npc_db/`

---

## ⚖️ Commands

Use `/npc help` in-game for a full list.

### Create NPCs

```bash
/npc create <Name> <DisplayName> <PlayerSkin|PersistantSkin>
/npc create <Name> <DisplayName> <SkinName>
```

### Skin Commands
```bash
/npc setskin <Name>
/npc setskin <Name> <SkinName>
```

### Scale
````bash
/npc scale <Name> <Scale>
````

### Movement Commands
```bash
/npc goto <Name>
/npc tp <Name>
```

### Utility
```bash
/npc list
/npc remove <Name>
/npc reload
/npc bind <Name> <ActionName>
```

---

## 👩‍🚀 Bind Custom Actions

You can register actions to your NPCs using the static registry:

```csharp
using PigNPC;
using PigNPC.Npc;

NpcActionRegistry.Actions["hello"] = (npc, player) =>
{
    player.SendMessage($"Hello! I am {npc.Data.DisplayName}");
};
```

Bind the action in-game:
```bash
/npc bind <NpcName> hello
```

---

## 📊 Using the API in Other Plugins

To interact with PigNPC in your own plugin:

```csharp
using PigNPC.Npc;

var allNpcs = PigNpcLoader.GetAll();
foreach (var npc in allNpcs)
{
    Console.WriteLine($"NPC ID: {npc.Data.Id}, Name: {npc.Data.DisplayName}");
}

var specificNpc = PigNpcLoader.GetNpcById("your-npc-id");
specificNpc?.DespawnEntity();
```

Save all changes manually:
```csharp
await PigNpcLoader.SaveAllAsync();
```

---

## 📁 Skin & Geometry Files

Place your files in the `npc_skins/` folder:
- `MySkin.png` - The skin texture
- `MySkin.json` - (Optional) Geometry data

Ensure geometry name is `geometry.MySkin` inside the `.json` file.

---

## 🌐 Supported Databases

| Type   | Setup                            |
|--------|----------------------------------|
| JSON   | No setup required                |
| SQLite | `sqlite.db` file auto-created    |
| MySQL  | Provide a connection string      |

Change provider in `PigNpcLoader.OnEnable()`

```csharp
_storageProvider = NpcStorageFactory.Create("mysql", "server=...;user=...;");
```

---

## 🚀 Future Plans

- GUI Form interaction
- Pathfinding support
- Animation control

---

## 🚀 Plugin Metadata

- Author: **Antoine Langevin**  
- Version: `1.0.0-RELEASE`  
- GitHub: [PigNPC Repository](https://github.com/pigraid-games/PigNPC)

---

Enjoy making your Minecraft world more alive! 🪖
