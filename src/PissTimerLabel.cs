using HUD;
using RWCustom;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Pisscat.Plugin;

namespace Pisscat
{
    internal class PissTimerLabel : HudPart
    {
        internal readonly FLabel label;
        private Vector2 pos;
        private Vector2 lastPos;
        private int remainVisibleCounter;
        private float fade;
        private float lastFade;
        private Player player;
        private readonly ConditionalWeakTable<Player, PlayerInfo> pissInfoRef;

        public PissTimerLabel(HUD.HUD hud, FContainer fContainer, Player player, ConditionalWeakTable<Player, PlayerInfo> pissInfoRef = null) : base(hud)
        {
            this.pissInfoRef = pissInfoRef;
            this.player = player;
            lastPos = pos;
            label = new(Custom.GetDisplayFont(), "");
            fContainer.AddChild(label);
        }

        internal void AssignToNewPlayer(Player player)
        {
            this.player = player;
        }

        public override void Update()
        {
            lastPos = pos;
            pos = new Vector2(100f, (int)(hud.rainWorld.options.ScreenSize.y - (15f + 10f * fade) - player.playerState.playerNumber * 20f) + 0.2f);
            lastFade = fade;

            if (remainVisibleCounter > 0)
                remainVisibleCounter--;

            if (hud.showKarmaFoodRain || remainVisibleCounter > 0)
            {
                fade = Mathf.Max(Mathf.Min(1f, fade + 0.1f), hud.foodMeter.fade);
            }
            else
            {
                fade = Mathf.Max(0f, fade - 0.1f);
            }

            if (hud.HideGeneralHud)
                fade = 0f;

            if (pissInfoRef != null && pissInfoRef.TryGetValue(player, out PlayerInfo info))
                label.text = info.GetTicksLeft();

            pos.x -= 95f;
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public override void Draw(float timeStacker)
        {
            float alpha = Mathf.Max(0.2f, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lastFade, fade, timeStacker)), 1.5f));
            label.alignment = FLabelAlignment.Left;
            label.x = DrawPos(timeStacker).x;
            label.y = DrawPos(timeStacker).y;
            label.alpha = alpha;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            label.RemoveFromContainer();
        }
    }
}
