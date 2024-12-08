﻿using RainMeadow;

namespace Drown
{
    public static class DrownModeRPCs
    {
        [RainMeadow.RPCMethod]
        public static void Arena_IncrementPlayerScore(RPCEvent rpcEvent, int score, ushort userWhoScored)
        {
            if (RainMeadow.RainMeadow.isArenaMode(out var arena))
            {
                var game = (RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame);
                if (game.manager.upcomingProcess != null)
                {
                    return;
                }
                if (!game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers) // team work makes the dream work
                {
                    DrownMode.currentPoints = score;
                }
                var oe = ArenaHelpers.FindOnlinePlayerByLobbyId(userWhoScored);
                var playerWhoScored = ArenaHelpers.FindOnlinePlayerNumber(arena, oe);
                game.GetArenaGameSession.arenaSitting.players[playerWhoScored].score = score;
            }
        }

        [RainMeadow.RPCMethod]
        public static void Arena_OpenDen(RPCEvent rpcEvent, bool denOpen)
        {

           DrownMode.openedDen = denOpen;

        }
    }
}