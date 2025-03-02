using BepInEx;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Nuktils.Utils;

namespace Pisscat
{
    [BepInDependency("slime-cubed.slugbase", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("nc.Nuktils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(MOD_ID, "Pisscat", "1.0.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "nuclear.pisscat";

        private int ticksTillUrination = -1;

        internal class PlayerInfo
        {
            internal Timer timer;
            private readonly int playerNumber;
            private readonly int ticksTill;

            internal PlayerInfo(Timer timer, int ticksTill, int playerNumber = -1)
            {
                this.timer = timer;
                this.ticksTill = ticksTill;
                this.playerNumber = playerNumber;
            }

            internal string GetTicksLeft()
            {
                return $"{playerNumber + 1}: {(ticksTill - timer.Ticks) / Intervals.Second}";
            }
        }

        private class RoomFloodLevel
        {
            internal float level;

            internal RoomFloodLevel(float level)
            {
                this.level = level;
            }
        }

        private readonly List<Player> players = new();
        private readonly List<PissTimerLabel> labels = new(); // the weird way I'm handling duplicate labels
        private readonly ConditionalWeakTable<Player, PlayerInfo> pissInfo = new();
        private readonly ConditionalWeakTable<Room, RoomFloodLevel> roomsToFlood = new();

        private static void PeeThePlayersPants(Player player, ConditionalWeakTable<Room, RoomFloodLevel> roomsToFlood)
        {
            if (player.room.waterObject == null)
                player.room.AddWater();

            if (!roomsToFlood.TryGetValue(player.room, out var _))
                roomsToFlood.Add(player.room, new(player.room.waterObject?.originalWaterLevel ?? 0));
        }

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += LoadOptions;

            On.Player.ctor += CreatePissTimer;
            On.Player.Update += Player_Update;
            On.Player.Die += RemovePissTimer;
            On.Player.Destroy += CleanupPlayerUrine;
            On.HUD.HUD.InitSinglePlayerHud += AddSTimerLabels;
            On.HUD.HUD.InitMultiplayerHud += AddMTimerLabels;
            On.RainWorldGame.ctor += ApplyOptions;
            On.Room.Update += Room_Update;

            //Oracle.ApplyOracleHooks();
        }

        private void Room_Update(On.Room.orig_Update orig, Room self)
        {
            orig(self);
            if (roomsToFlood.TryGetValue(self, out RoomFloodLevel floodLevel) && self.water && self.waterObject != null)
            {
                if (floodLevel.level < self.PixelHeight)
                    floodLevel.level += 4f;
                self.waterObject.fWaterLevel = floodLevel.level;
            }
        }

        private void ApplyOptions(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            ticksTillUrination = Options.pissTime.Value * Intervals.Second;
        }

        private void AddMTimerLabels(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
        {
            orig(self, session);
            for (int i = 0; i < session.Players.Count; i++)
            {
                if (i >= labels.Count)
                {
                    PissTimerLabel label = new(self, self.fContainers[1], session.Players[i].realizedCreature as Player, pissInfo);
                    labels.Add(label);
                    self.AddPart(label);
                }
                else
                {
                    labels[i].AssignToNewPlayer(session.Players[i].realizedCreature as Player);
                }
            }
        }

        private void AddSTimerLabels(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            for (int i = 0; i < players.Count; i++)
            {
                if (i < labels.Count)
                {
                    labels[i].AssignToNewPlayer(players[i]);
                    continue;
                }

                PissTimerLabel label = new(self, self.fContainers[1], players[i], pissInfo);
                labels.Add(label);
                self.AddPart(label);
            }
        }

        private void CleanupPlayerUrine(On.Player.orig_Destroy orig, Player self)
        {
            players.Remove(self);
            pissInfo.Remove(self);
            orig(self);
        }

        private void RemovePissTimer(On.Player.orig_Die orig, Player self)
        {
            pissInfo.Remove(self);
            orig(self);
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (pissInfo.TryGetValue(self, out PlayerInfo info))
            {
                Logger.LogDebug($"room of player {self.playerState.playerNumber}('{self.room.abstractRoom.name}'): {self.room.Height}, {self.room.PixelHeight}, {self.room.water}, {self.room.waterObject?.originalWaterLevel}, {self.room.waterObject?.fWaterLevel}");

                // (self.bodyChunks[1].vel.x < 4.7f || self.bodyChunks[1].vel.x > -4.7f) && 
                if (self.bodyChunks[1].submersion == 0f && !self.room.abstractRoom.gate && !self.room.abstractRoom.shelter && !self.inShortcut)
                    info.timer.Tick();

                if (info.timer.Ended())
                {
                    PeeThePlayersPants(self, roomsToFlood);
                    info.timer = new(ticksTillUrination);
                }
            }

            orig(self, eu);
        }

        private void CreatePissTimer(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (self.SlugCatClass == Enums.Pisscat && !pissInfo.TryGetValue(self, out var _))
            {
                pissInfo.Add(self, new(new(ticksTillUrination), ticksTillUrination, self.playerState.playerNumber));
                Logger.LogDebug($"creating info for player {self.playerState.playerNumber}");
                players.Add(self);
            }
        }

        private void LoadOptions(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            Logger.LogInfo("loading options...");
            MachineConnector.SetRegisteredOI(MOD_ID, new Options());
            Logger.LogInfo("loaded options");
        }
    }
}