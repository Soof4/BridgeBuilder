using Terraria;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;

namespace SFactions
{
    [ApiVersion(2, 1)]
    public class SFactions : TerrariaPlugin {
        public override string Name => "BridgeBuilder";
        public override Version Version => new Version(1, 0, 2);
        public override string Author => "Soofa";
        public override string Description => "Build bridges!";
        public SFactions(Main game) : base(game) {
        }

        private static int[] AllowedTileIDs = {19, 380, 427, 435, 436, 437, 438, 439, };    // platforms, planter boxes, team platforms

        public override void Initialize() {
            TShockAPI.Commands.ChatCommands.Add(new("bridgebuilder.bridge", BridgeCmd, "bridge") {
                AllowServer = false,
                HelpText = "Builds bridges towards the direction you're looking. (You need to be holding some amount of blocks or walls.)"
            });
        }

        private async void BridgeCmd(CommandArgs args) {
            await Task.Run(() => {
                TSPlayer plr = args.Player;
                int direction = plr.TPlayer.direction;
                int startX = direction == -1 ? plr.TileX - 1 : plr.TileX + 2;
                int i = 0;
                int j = plr.TileY + 3;
                Item selectedItem = plr.SelectedItem;

                if (selectedItem.createTile < 0 && selectedItem.createWall < 0) {
                    plr.SendErrorMessage("The item you're holding can't be placed.");
                    return;
                }

                bool isTile = selectedItem.createTile >= 0;
                int styleId = selectedItem.placeStyle;

                if (j > Main.maxTilesY || j < 0) {
                    plr.SendErrorMessage("Can't build a bridge here.");
                    return;
                }
                
                if (isTile) {
                    if (!AllowedTileIDs.Contains(selectedItem.createTile)) {
                        plr.SendErrorMessage("The item you're holding is not allowed for auto bridging. " + 
                                             "(Only platforms, planter boxes and walls are allowed.)");
                        return;
                    }

                    while (CheckTileAvailability(startX, startX + i, j, plr)) {
                        WorldGen.PlaceTile(startX + i, j, selectedItem.createTile, false, true, -1, styleId);
                        TSPlayer.All.SendTileRect((short)(startX + i), (short)j, 1, 1);
                        plr.SelectedItem.stack--;
                        i += direction;
                    }
                }
                else {
                    while (CheckTileAvailability(startX, startX + i, j, plr)) {
                        Main.tile[startX + i, j].wall = (ushort)selectedItem.createWall;
                        TSPlayer.All.SendTileRect((short)(startX + i), (short)j, 1, 1);
                        plr.SelectedItem.stack--;
                        i += direction;
                    }
                }

                NetMessage.SendData((int)PacketTypes.PlayerSlot, -1, -1, NetworkText.FromLiteral(plr.SelectedItem.Name), plr.Index, plr.TPlayer.selectedItem);
                NetMessage.SendData((int)PacketTypes.PlayerSlot, plr.Index, -1, NetworkText.FromLiteral(plr.SelectedItem.Name), plr.Index, plr.TPlayer.selectedItem);
            });
        }

        private static bool CheckTileAvailability(int startX, int x, int y, TSPlayer plr) {
            return x < Main.maxTilesX && x >= 0 && 
                   Math.Abs(startX - x) < 256 && 
                   plr.SelectedItem.stack > 0 && 
                   !TShock.Regions.InArea(x, y) &&
                   !Main.tile[x, y].active();
        }
    }
}
