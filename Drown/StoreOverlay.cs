using Menu;
using On.MoreSlugcats;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using RainMeadow;

namespace Drown
{
    public class StoreOverlay : Menu.Menu
    {
        public AbstractCreature? foundMe;
        public Vector2 pos;



        public class ItemButton
        {
            public OnlinePhysicalObject player;
            public SimplerButton button;
            public SimplerSymbolButton? kickbutton;
            public bool mutedPlayer;
            private string clientMuteSymbol;
            public Dictionary<string, int> storeItems;
            public StoreOverlay overlay;
            public int cost;
            public string name;
            public bool didRespawn;
            public ItemButton(StoreOverlay menu, Vector2 pos, RainWorldGame game, ArenaOnlineGameMode arena, DrownMode drown, KeyValuePair<string, int> itemEntry, int index, bool canBuy = false)
            {
                this.overlay = menu;
                this.name = itemEntry.Key;
                this.cost = itemEntry.Value;
                this.button = new RainMeadow.SimplerButton(menu, menu.pages[0], $"{itemEntry.Key}: {itemEntry.Value}", pos, new Vector2(110, 30));

                AbstractCreature me = null;

                this.button.OnClick += (_) =>
                {
                    AbstractPhysicalObject desiredObject = null;
                    for (int i = 0; i < game.GetArenaGameSession.Players.Count; i++)
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(game.GetArenaGameSession.Players[i], out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                        {
                            me = game.GetArenaGameSession.Players[i];
                        } 
                    }

                        switch (index)
                    {
                        case 0:
                            desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), false);
                            break;
                        case 1:
                            desiredObject = new AbstractSpear(game.world, null, me.pos, game.GetNewID(), true);
                            break;
                        case 2:
                            desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, me.pos, game.GetNewID());
                            break;

                        case 3:

                                didRespawn = false;
                                RevivePlayer(game.GetArenaGameSession, arena, me);
                                didRespawn = true;
                            
                            break;
                        case 4:
                            DrownMode.openedDen = true;
                            if (!game.GetArenaGameSession.GameTypeSetup.spearsHitPlayers)
                            {
                                for (int j = 0; j < arena.arenaSittingOnlineOrder.Count; j++)
                                {
                                    var currentPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, j);
                                    if (!currentPlayer.isMe)
                                    {
                                        currentPlayer.InvokeOnceRPC(DrownModeRPCs.Arena_OpenDen, DrownMode.openedDen);
                                    }
                                }
                            }
                            break;
                    }

                    if (desiredObject != null && me != null)
                    {
                        (game.cameras[0].room.abstractRoom).AddEntity(desiredObject);
                        desiredObject.RealizeInRoom();
                    }
                    DrownMode.currentPoints = DrownMode.currentPoints - itemEntry.Value;


                };
                this.button.owner.subObjects.Add(button);
            }

            public void Destroy()
            {
                this.button.RemoveSprites();
                this.button.page.RemoveSubObject(this.button);
            }
        }

        public RainWorldGame game;
        public List<ItemButton> storeItemList;
        ItemButton itemButtons;
        public DrownMode drown;

        public StoreOverlay(ProcessManager manager, RainWorldGame game, DrownMode drown, ArenaOnlineGameMode arena) : base(manager, RainMeadow.RainMeadow.Ext_ProcessID.SpectatorMode)
        {
            this.game = game;
            this.drown = drown;
            this.pages.Add(new Page(this, null, "store", 0));
            this.selectedObject = null;
            this.storeItemList = new();
            this.pos = new Vector2(180, 553);
            this.pages[0].subObjects.Add(new Menu.MenuLabel(this, this.pages[0], this.Translate("STORE"), new(pos.x, pos.y + 30f), new(110, 30), true));
            var storeItems = new Dictionary<string, int> {
            { "Spear", 1 },
            { "Explosive Spear", 3 },
            { "Scavenger Bomb", 3 },
            { "Revive", 5 },
            { "Open Den", 30 },


        };
            int index = 0; // Initialize an index variable

            foreach (var item in storeItems)
            {
                // Format the message for the button, for example: "Spear: 1"
                string buttonMessage = $"{item.Key}: {item.Value}";

                // Create a new ItemButton for each dictionary entry
                this.itemButtons = new ItemButton(this, pos, game, arena, drown, item, index, true);
                this.storeItemList.Add(itemButtons);


                pos.y -= 40; // Move the button 40 units down for the next one
                index++;
            }

        }

        public override void Update()
        {
            base.Update();
            foreach (var c in game.Players)
            {
                if (OnlinePhysicalObject.map.TryGetValue(c, out var onlineC))
                {

                    if (onlineC.owner == OnlineManager.mePlayer)
                    {
                        foundMe = c;
                    }

                }
            }
            if (storeItemList != null)
            {
                for (int i = 0; i < storeItemList.Count; i++)
                {
                    if (foundMe == null && storeItemList[i].name != "Revive")
                    {
                        storeItemList[i].button.buttonBehav.greyedOut = true;

                    }
                    else
                    {
                        storeItemList[i].button.buttonBehav.greyedOut = DrownMode.currentPoints < storeItemList[i].cost;
                    }

                }

            }
        }

        private static void RevivePlayer(ArenaGameSession game, ArenaOnlineGameMode arena, AbstractCreature player)
        {


            List<int> exitList = new List<int>();

            for (int i = 0; i < game.room.world.GetAbstractRoom(0).exits; i++)
            {
                exitList.Add(i);
            }



            arena.onlineArenaGameMode.SpawnPlayer(arena, game, game.room, exitList);
            game.game.cameras[0].hud.AddPart(new OnlineHUD(game.game.cameras[0].hud, game.game.cameras[0], arena));

            var absRoom = game.room.abstractRoom;
            if (RoomSession.map.TryGetValue(game.room.abstractRoom, out var roomSession))
            {
                // we go over all APOs in the room
                var entities = absRoom.entities;
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                    {
                        if (oe.isMine && oe.apo is AbstractCreature && (oe.apo as AbstractCreature).state.dead)
                        {
                            oe.apo.LoseAllStuckObjects();
                            // not-online-aware removal
                            oe.beingMoved = true;

                            if (oe.apo.realizedObject is Creature c && c.inShortcut)
                            {
                                if (c.RemoveFromShortcuts()) c.inShortcut = false;
                            }

                            entities.Remove(oe.apo);

                            absRoom.creatures.Remove(oe.apo as AbstractCreature);

                            absRoom.realizedRoom.RemoveObject(oe.apo.realizedObject);
                            //absRoom.realizedRoom.rooroom.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                            oe.beingMoved = false;
                        }
                    }
                }
            }

            //if (player != null)
            //{
            //    game.Players.Remove(player);
            //}
            //RainMeadow.RainMeadow.sSpawningAvatar = true;
            //AbstractCreature abstractCreature = new AbstractCreature(game.room.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), player?.ID ?? new EntityID(-1, 0));

            //abstractCreature.pos.room = game.room.abstractRoom.index;
            //abstractCreature.pos.abstractNode = game.room.ShortcutLeadingToNode(0).destNode;

            //RainMeadow.RainMeadow.Debug("assigned ac, registering");

            //game.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            //RainMeadow.RainMeadow.sSpawningAvatar = false;

            //if (ModManager.MSC)
            //{
            //    game.game.cameras[0].followAbstractCreature = abstractCreature;
            //}

            //if (abstractCreature.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
            //{
            //    abstractCreature.state = new PlayerState(abstractCreature, ArenaHelpers.FindOnlinePlayerNumber(arena, oe.owner), customization.playingAs, isGhost: false);

            //}
            //else
            //{
            //    RainMeadow.RainMeadow.Error("Could not get online owner for spawned player!");
            //    abstractCreature.state = new PlayerState(abstractCreature, 0, game.characterStats_Mplayer[0].name, isGhost: false);
            //}

            //RainMeadow.RainMeadow.Debug("Arena: Realize Creature!");
            //abstractCreature.Realize();

            //if (abstractCreature.pos.abstractNode > abstractCreature.Room.exits) abstractCreature.pos.abstractNode = UnityEngine.Random.Range(0, abstractCreature.Room.exits);



            //var shortCutVessel = new ShortcutHandler.ShortCutVessel(abstractCreature.Room.realizedRoom.ShortcutLeadingToNode(abstractCreature.pos.abstractNode).DestTile, abstractCreature.realizedCreature, game.room.world.GetAbstractRoom(0), 0);

            //shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            //shortCutVessel.room = game.room.world.GetAbstractRoom(abstractCreature.Room.name);

            //game.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);

            if (player != null)
            {
                game.game.world.GetResource().ApoLeavingWorld(player);
            }


        }

    }
}