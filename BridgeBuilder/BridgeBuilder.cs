using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace SFactions
{
    [ApiVersion(2, 1)]
    public class SFactions : TerrariaPlugin {
        public override string Name => "BridgeBuilder";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "Soofa";
        public override string Description => "Build bridges!";
        public SFactions(Main game) : base(game) {
        }

        private static int[] AllowedTileIDs = {19, 380, 427, 435, 436, 437, 438, 439, };    // platforms, planter boxes, team platforms

        public override void Initialize() {
            TShockAPI.Commands.ChatCommands.Add(new("bridgebuilder.bridge", BridgeCmd, "bridge") {
                AllowServer = false,
                HelpText = "Builds bridges towards the direction you're looking. (You need to be holding some amount of blocks or walls."
            });
        }

        private async void BridgeCmd(CommandArgs args) {
            await Task.Run(() => {
                TSPlayer plr = args.Player;
                int direction = plr.TPlayer.direction;
                int i = direction == -1 ? plr.TileX -1 : plr.TileX + 2; 
                int j = plr.TileY + 3;
                Item selectedItem = plr.SelectedItem;

                if (selectedItem.createTile < 0 && selectedItem.createWall < 0) {
                    plr.SendErrorMessage("The item you're holding can't be placed.");
                    return;
                }

                bool isTile = selectedItem.createTile >= 0;
                ushort placementId = selectedItem.createTile < 0 ? (ushort)selectedItem.createWall : (ushort)selectedItem.createTile;
                int styleId = selectedItem.placeStyle;

                
                if (isTile) {
                    if (!AllowedTileIDs.Contains(placementId)) {
                    plr.SendErrorMessage("The item you're holding is not allowed for auto bridging. " + 
                                                 "(Only platforms, planter boxes and walls are allowed.)");
                    return;
                    }

                    while (Math.Abs(plr.TileX - i) < 255 && plr.SelectedItem.stack > 0 && !TShock.Regions.InArea(i, j)) {
                    if (Main.tile[i, j].active()) {
                        break;
                    }

                    WorldGen.PlaceTile(i, j, placementId, false, true, -1, styleId);
                
                    plr.SelectedItem.stack--;
                    i += direction;
                    }
                }
                else {
                   while (Math.Abs(plr.TileX - i) < 255 && plr.SelectedItem.stack > 0 && !TShock.Regions.InArea(i, j)) {
                    if (Main.tile[i, j].active()) {
                        break;
                    }
                    
                    Main.tile[i, j].wall = placementId;
                    
                    plr.SelectedItem.stack--;
                    i += direction;
                   }
                }

                TSPlayer.All.SendTileRect(direction == -1 ? (short)i : (short)plr.TileX, (short)j, 255, 1);
                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(plr.SelectedItem.Name), plr.Index, plr.TPlayer.selectedItem);
                NetMessage.SendData((int)PacketTypes.PlayerSlot, plr.Index, -1, NetworkText.FromLiteral(plr.SelectedItem.Name), plr.Index, plr.TPlayer.selectedItem);
            });
        }
    }
}
