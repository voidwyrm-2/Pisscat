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
        private const float urinatedRoomWaterLevel = 1200f;

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

        private class EmptyClass { }

        private readonly List<Player> players = new();
        private readonly List<PissTimerLabel> labels = new(); // the weird way I'm handling duplicate labels
        private readonly ConditionalWeakTable<Water, EmptyClass> waterToChange = new();
        private readonly ConditionalWeakTable<Player, PlayerInfo> pissInfo = new();

        private static void PeeThePlayersPants(Player player)
        {
            if (player.room.waterObject == null)
                player.room.AddWater();
            player.room.waterObject.fWaterLevel = urinatedRoomWaterLevel;
            //waterToChange.Add(player.room.waterObject, new());
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
            //On.Water.DrawSprites += NotDrinkingEnoughWater;
            //On.Water.Destroy += WentToTheBathroom;
            On.RainWorldGame.ctor += GetOptions;
        }

        private void GetOptions(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            ticksTillUrination = Options.pissTime.Value * Intervals.Second;
        }

        private void WentToTheBathroom(On.Water.orig_Destroy orig, Water self)
        {
            waterToChange.Remove(self);
            orig(self);
        }

        private void NotDrinkingEnoughWater(On.Water.orig_DrawSprites orig, Water self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            if (waterToChange.TryGetValue(self, out var _))
            {
                Shader.EnableKeyword("HR");
                foreach (FSprite sprite in sLeaser.sprites)
                    sprite.color = new(1f, 1f, 0);
            }
            orig(self, sLeaser, rCam, timeStacker, camPos);
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
                if (i >= labels.Count)
                {
                    PissTimerLabel label = new(self, self.fContainers[1], players[i], pissInfo);
                    labels.Add(label);
                    self.AddPart(label);
                }
                else
                {
                    labels[i].AssignToNewPlayer(players[i]);
                }
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
                // (self.bodyChunks[1].vel.x < 4.7f || self.bodyChunks[1].vel.x > -4.7f) && 
                if (self.bodyChunks[1].submersion == 0 && !self.room.abstractRoom.gate && !self.room.abstractRoom.shelter && !self.inShortcut)
                    info.timer.Tick();

                if (info.timer.Ended())
                {
                    PeeThePlayersPants(self);
                    info.timer = new(ticksTillUrination);
                }
            }

            orig(self, eu);
        }

        private void CreatePissTimer(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (!pissInfo.TryGetValue(self, out var _))
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