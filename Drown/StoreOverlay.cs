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
        public AbstractCreature? me;
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
                WorldCoordinate myAbstractPos;


                this.button.OnClick += (_) =>
                {
                    var copyofSession = game.GetArenaGameSession.Players;
                    AbstractPhysicalObject desiredObject = null;
                    for (int i = copyofSession.Count - 1; i >= 0; i--)
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(copyofSession[i], out var onlineP) && onlineP.owner == OnlineManager.mePlayer)
                        {
                            switch (index)
                            {
                                case 0:
                                    desiredObject = new AbstractSpear(game.world, null, copyofSession[i].pos, game.GetNewID(), false);
                                    break;
                                case 1:
                                    desiredObject = new AbstractSpear(game.world, null, copyofSession[i].pos, game.GetNewID(), true);
                                    break;
                                case 2:
                                    desiredObject = new AbstractPhysicalObject(game.world, AbstractPhysicalObject.AbstractObjectType.ScavengerBomb, null, copyofSession[i].pos, game.GetNewID());
                                    break;

                                case 3:
                                    didRespawn = false;
                                    var room = copyofSession[i].Room.realizedRoom;
                                    var node = copyofSession[i].realizedCreature.coord.abstractNode;

                                    RevivePlayer(copyofSession[i].realizedCreature as Player);

                                    copyofSession[i].realizedCreature.RemoveFromRoom();
                                    if (node > room.abstractRoom.exits) node = UnityEngine.Random.Range(0, room.abstractRoom.exits);

                                    var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(node).DestTile, copyofSession[i].realizedCreature, game.world.GetAbstractRoom(0), 0);

                                    shortCutVessel.entranceNode = copyofSession[i].pos.abstractNode;
                                    shortCutVessel.room = game.world.GetAbstractRoom(copyofSession[i].Room.name);

                                    game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);

                                    didRespawn = true;
                                    break;
                                case 4:
                                    drown.openedDen = true;
                                    break;
                            }

                            if (desiredObject != null)
                            {
                                (game.cameras[0].room.abstractRoom).AddEntity(desiredObject);
                                desiredObject.RealizeInRoom();
                            }
                            drown.currentPoints = drown.currentPoints - itemEntry.Value;
                        }
                    }
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
            { "Spear", 0 },
            { "Explosive Spear", 3 },
            { "Scavenger Bomb", 3 },
            { "Revive", 5 },
            { "Open Den", 50 },


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
                        me = c;
                    }

                }
            }
            if (storeItemList != null)
            {
                for (int i = 0; i < storeItemList.Count; i++)
                {
                    if (me == null && storeItemList[i].name == "Revive")
                    {
                        storeItemList[i].button.buttonBehav.greyedOut = true;
                    }
                    else
                    {
                        storeItemList[i].button.buttonBehav.greyedOut = drown.currentPoints < storeItemList[i].cost;
                    }
                }

            }
        }

        private static void RevivePlayer(Player self)
        {

            self.stun = 20;
            self.airInLungs = 0.1f;
            self.exhausted = true;
            self.aerobicLevel = 1;
            self.playerState.alive = true;
            self.playerState.permaDead = false;
            self.dead = false;
            self.killTag = null;
            self.killTagCounter = 0;

        }

    }
}