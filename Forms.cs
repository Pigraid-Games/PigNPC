using PigNet;
using PigNet.UI;
using PigNPC.Npc;

namespace PigNPC;

public class Forms
{
    public static async void ShowForm(Player player, CustomNpc customNpc)
    {
        var form = new CustomForm
        {
            Title = $"PigNpc: {customNpc.Data.DisplayName}",
            Content =
            [
                new Label { Text = "You can change the settings of the NPC here." },
                new Input { Text = "DisplayName", Placeholder = "Name", Value = customNpc.Data.DisplayName },
                new Input { Text = "ActionId", Placeholder = "open-cos-form", Value = customNpc.Data.ActionId },
                new Toggle { Text = "IsVisible", Value = customNpc.Data.IsVisible },
                new Toggle { Text = "IsAlwaysShowName", Value = customNpc.Data.IsAlwaysShowName },
                new Dropdown { Text = "SkinType", Options = ["PersistantSkin", "Skin"] },
                new Label { Text = $"For the skin position, use /npc tp {customNpc.NameTag}" },
                new Label { Text = $"For the skin, use /npc setskin {customNpc.Data.NameTag} <skinName>" }
            ],
            ExecuteAction = async void (_, customForm) =>
            {
                var displayName = ((Input)customForm.Content[1]).Value ?? "N/A";
                var actionId = ((Input)customForm.Content[2]).Value ?? "N/A";
                var isVisible = ((Toggle)customForm.Content[3]).Value;
                var isAlwaysShowName = ((Toggle)customForm.Content[4]).Value;
                var skinType = ((Dropdown)customForm.Content[5]).Value;
                
                customNpc.Data.DisplayName = displayName;
                customNpc.Data.ActionId = actionId;
                customNpc.Data.IsVisible = isVisible;
                customNpc.Data.IsAlwaysShowName = isAlwaysShowName;
                customNpc.Data.SkinType = (PigNpcLoader.SkinType)skinType;

                await PigNpcLoader.SaveAllAsync();
                Commands.NpcReload(player);
            }
        };
        player.SendForm(form);
    }
}