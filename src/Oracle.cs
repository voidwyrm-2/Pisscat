namespace Pisscat
{
    internal static class Oracle
    {
        internal static void ApplyOracleHooks()
        {
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.Update += SSOracleBehavior_Update;
        }

        private static void SSOracleBehavior_Update(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            orig(self, eu);

            if (self.oracle.room.game.StoryCharacter != Enums.Pisscat)
                return;

            if ((self.player == null || self.player.room != self.oracle.room) && self.pearlConversation == null && self.dialogBox.messages.Count > 0)
                self.dialogBox.messages.Clear();
        }

        private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            if (self.oracle.room.game.StoryCharacter != Enums.Pisscat)
            {
                orig(self);
                return;
            }

            if (self.dialogBox != null)
            {
                self.dialogBox.NewMessage(self.Translate("Hello again little one!"), 0);
                self.dialogBox.NewMessage(self.Translate("What brings you here?"), 0);
            }

            orig(self);
        }
    }
}
