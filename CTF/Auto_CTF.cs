using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MCForge
{
    public class Team
    {
        public string color;
        public int points = 0;
        public List<Player> members;
        public Team(string color)
        {
            this.color = c.Parse(color);
            members = new List<Player>();
        }
        public void Add(Player p)
        {
            members.Add(p);
        }
        public bool isOnTeam(Player p)
        {
            if (members.IndexOf(p) != -1)
                return true;
            else
                return false;
        }
    }
    public class Data
    {
        public Player p;
        public int cap = 0;
        public int tag = 0;
        public int points = 0;
        public bool hasflag;
        public bool blue;
        public bool chatting = false;
        public Data(bool team, Player p)
        {
            blue = team; this.p = p;
        }
    }
    public class Base
    {
        public ushort x;
        public ushort y;
        public ushort z;
        public byte block;
        public Base(ushort x, ushort y, ushort z, Team team)
        {
            this.x = x; this.y = y; this.z = z;
        }
        public Base()
        {
        }
    }
    public class Auto_CTF
    {
        public int xline;
        public bool started = false;
        public int zline;
        public int yline;
        int tagpoint = 5;
        int cappoint = 10;
        int taglose = 5;
        int caplose = 10;
        bool look = false;
        public int maxpoints = 3;
        Team redteam;
        Team blueteam;
        Base bluebase;
        Base redbase;
        Level mainlevel;
        List<string> maps = new List<string>();
        List<Data> cache = new List<Data>();
        string mapname = "";
        public void LoadMap(string map)
        {
            mapname = map;
            string[] lines = File.ReadAllLines("CTF/" + mapname + ".config");
            foreach (string l in lines)
            {
                switch (l.Split('=')[0])
                {
                    case "base.red.x":
                        redbase.x = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "base.red.y":
                        redbase.y = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "game.maxpoints":
                        maxpoints = int.Parse(l.Split('=')[1]);
                        break;
                    case "game.tag.points-gain":
                        tagpoint = int.Parse(l.Split('=')[1]);
                        break;
                    case "game.tag.points-lose":
                        taglose = int.Parse(l.Split('=')[1]);
                        break;
                    case "game.capture.points-gain":
                        cappoint = int.Parse(l.Split('=')[1]);
                        break;
                    case "game.capture.points-lose":
                        caplose = int.Parse(l.Split('=')[1]);
                        break;
                    case "auto.setup":
                        look = bool.Parse(l.Split('=')[1]);
                        break;
                    case "base.red.z":
                        redbase.z = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "base.red.block":
                        redbase.block = Block.Byte(l.Split('=')[1]);
                        break;
                    case "base.blue.x":
                        bluebase.x = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "base.blue.y":
                        bluebase.y = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "base.blue.z":
                        bluebase.z = ushort.Parse(l.Split('=')[1]);
                        break;
                    case "map.line.z":
                        zline = ushort.Parse(l.Split('=')[1]);
                        break;
                }
            }
            Command.all.Find("unload").Use(null, "ctf");
            if (File.Exists("levels/ctf.lvl"))
                File.Delete("levels/ctf.lvl");
            File.Copy("CTF/maps/" + mapname + ".lvl", "levels/ctf.lvl");
            Command.all.Find("load").Use(null, "ctf");
            mainlevel = Level.Find("ctf");
        }
        public Auto_CTF()
        {
            //Load some configs
            if (!Directory.Exists("CTF")) Directory.CreateDirectory("CTF");
            if (!File.Exists("CTF/maps.config"))
            {
                Server.s.Log("No maps were found!");
                return;
            }
            string[] lines = File.ReadAllLines("CTF/maps.config");
            foreach (string l in lines)
                maps.Add(l);
            redbase = new Base();
            bluebase = new Base();
            Start();
            //Lets get started
            Player.PlayerMove += new Player.OnPlayerMove(Player_PlayerMove);
            Player.PlayerDeath += new Player.OnPlayerDeath(Player_PlayerDeath);
            Player.PlayerChat += new Player.OnPlayerChat(Player_PlayerChat);
            Player.PlayerCommand += new Player.OnPlayerCommand(Player_PlayerCommand);
            Player.PlayerBlockChange += new Player.BlockchangeEventHandler2(Player_PlayerBlockChange);
            Player.PlayerDisconnect += new Player.OnPlayerDisconnect(Player_PlayerDisconnect);
            mainlevel.LevelUnload += new Level.OnLevelUnload(mainlevel_LevelUnload);
        }

        void Player_PlayerDisconnect(Player p, string reason)
        {
            if (p.level == mainlevel)
            {
                if (blueteam.members.Contains(p))
                {
                    //cache.Remove(GetPlayer(p));
                    blueteam.members.Remove(p);
                    Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + blueteam.color + "left the ctf game");
                }
                else if (redteam.members.Contains(p))
                {
                    //cache.Remove(GetPlayer(p));
                    redteam.members.Remove(p);
                    Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + redteam.color + "left the ctf game");
                }
            }
        }

        void mainlevel_LevelUnload(Level l)
        {
            if (started)
            {
                Server.s.Log("Failed!, A ctf game is curretnly going on!");
                Plugins.Plugin.CancelLevelEvent(Plugins.Events.LevelUnload, l);
            }

        }
        public void Start()
        {
            if (Level.Find("ctf") != null)
            {
                Command.all.Find("unload").Use(null, "ctf");
                Thread.Sleep(1000);
            }
            if (started)
                return;
            blueteam = new Team("blue");
            redteam = new Team("red");
            LoadMap(maps[new Random().Next(maps.Count)]);
            if (look)
            {
                for (ushort x = 0; x < mainlevel.width; x++)
                {
                    for (ushort y = 0; y < mainlevel.depth; y++)
                    {
                        for (ushort z = 0; z < mainlevel.height; z++)
                        {
                            if (mainlevel.GetTile(x, y, z) == Block.red)
                            {
                                redbase.x = x; redbase.y = y; redbase.z = z;
                            }
                            else if (mainlevel.GetTile(x, y, z) == Block.blue || mainlevel.GetTile(x, y, z) == Block.cyan)
                            {
                                bluebase.x = x; bluebase.y = y; bluebase.z = z;
                            }
                        }
                    }
                }
                zline = mainlevel.height / 2;
            }
            Server.s.Log("[Auto_CTF] Running...");
            started = true;
            MySQL.executeQuery("CREATE TABLE if not exists CTF (ID MEDIUMINT not null auto_increment, Name VARCHAR(20), Points MEDIUMINT UNSIGNED, Captures MEDIUMINT UNSIGNED, tags MEDIUMINT UNSIGNED, PRIMARY KEY (ID));");
        }
        void End()
        {
            started = false;
            string winner = "";
            Team winnerteam = null;
            if (blueteam.points >= maxpoints || blueteam.points > redteam.points)
            {
                winnerteam = blueteam;
                winner = "blue team";
            }
            else if (redteam.points >= maxpoints || redteam.points > blueteam.points)
            {
                winnerteam = redteam;
                winner = "red team";
            }
            else
            {
                Player.GlobalMessageLevel(mainlevel, "The game ended in a tie!");
                //Vote();
            }
            Player.GlobalMessageLevel(mainlevel, "The winner was " + winnerteam.color + winner + "!!");
            Thread.Sleep(4000);
            //MYSQL!
            cache.ForEach(delegate(Data d)
            {
                string commandString =
               "UPDATE CTF SET Points='" + d.points + "'" +
               ", Captures=" + d.cap +
               ", tags=" + d.tag +
               "' WHERE Name='" + d.p.name + "'";
                d.hasflag = false;
                MySQL.executeQuery(commandString);
            });
            //Vote();
            Player.GlobalMessageLevel(mainlevel, "Starting a new game!");
            redbase = null;
            redteam = null;
            bluebase = null;
            blueteam = null;
            bluebase = new Base();
            redbase = new Base();
            Thread.Sleep(2000);
        }
        void Player_PlayerBlockChange(Player p, ushort x, ushort y, ushort z, byte type)
        {
            if (p.level == mainlevel && !blueteam.members.Contains(p) && !redteam.members.Contains(p))
            {
                p.SendBlockchange(x, y, z, p.level.GetTile(x, y, z));
                Player.SendMessage(p, "You are not on a team!");
                Plugins.Plugin.CancelPlayerEvent(Plugins.Events.BlockChange, p);
            }
            if (p.level == mainlevel && blueteam.members.Contains(p) && x == redbase.x && y == redbase.y && z == redbase.z && mainlevel.GetTile(redbase.x, redbase.y, redbase.z) != Block.air)
            {
                Player.GlobalMessageLevel(mainlevel, blueteam.color + p.name + " took the " + redteam.color + " red team's FLAG!");
                GetPlayer(p).hasflag = true;
            }
            if (p.level == mainlevel && redteam.members.Contains(p) && x == bluebase.x && y == bluebase.y && z == bluebase.z && mainlevel.GetTile(bluebase.x, bluebase.y, bluebase.z) != Block.air)
            {
                Player.GlobalMessageLevel(mainlevel, redteam.color + p.name + " took the " + blueteam.color + " blue team's FLAG");
                GetPlayer(p).hasflag = true;
            }
            if (p.level == mainlevel && blueteam.members.Contains(p) && x == bluebase.x && y == bluebase.y && z == bluebase.z && mainlevel.GetTile(bluebase.x, bluebase.y, bluebase.z) != Block.air)
            {
                if (GetPlayer(p).hasflag)
                {
                    Player.GlobalMessageLevel(mainlevel, blueteam.color + p.name + " RETURNED THE FLAG!");
                    GetPlayer(p).hasflag = false;
                    GetPlayer(p).cap++;
                    GetPlayer(p).points += cappoint;
                    blueteam.points++;
                    mainlevel.Blockchange(redbase.x, redbase.y, redbase.z, Block.red);
                    p.SendBlockchange(x, y, z, p.level.GetTile(x, y, z));
                    Plugins.Plugin.CancelPlayerEvent(Plugins.Events.BlockChange, p);
                    if (blueteam.points >= maxpoints)
                    {
                        End();
                        Start();
                    }
                }
                else
                {
                    Player.SendMessage(p, "You cant take your own flag!");
                    p.SendBlockchange(x, y, z, p.level.GetTile(x, y, z));
                    Plugins.Plugin.CancelPlayerEvent(Plugins.Events.BlockChange, p);
                }
            }
            if (p.level == mainlevel && redteam.members.Contains(p) && x == redbase.x && y == redbase.y && z == redbase.z && mainlevel.GetTile(redbase.x, redbase.y, redbase.z) != Block.air)
            {
                if (GetPlayer(p).hasflag)
                {
                    Player.GlobalMessageLevel(mainlevel, redteam.color + p.name + " RETURNED THE FLAG!");
                    GetPlayer(p).hasflag = false;
                    GetPlayer(p).points += cappoint;
                    GetPlayer(p).cap++;
                    redteam.points++;
                    mainlevel.Blockchange(bluebase.x, bluebase.y, bluebase.z, Block.blue);
                    p.SendBlockchange(x, y, z, p.level.GetTile(x, y, z));
                    Plugins.Plugin.CancelPlayerEvent(Plugins.Events.BlockChange, p);
                    if (redteam.points >= maxpoints)
                    {
                        End();
                        Start();
                    }
                }
                else
                {
                    Player.SendMessage(p, "You cant take your own flag!");
                    p.SendBlockchange(x, y, z, p.level.GetTile(x, y, z));
                    Plugins.Plugin.CancelPlayerEvent(Plugins.Events.BlockChange, p);
                }
            }
        }
        public Data GetPlayer(Player p)
        {
            foreach (Data d in cache)
            {
                if (d.p == p)
                    return d;
            }
            return null;
        }
        void Player_PlayerCommand(string cmd, Player p, string message)
        {
            if (cmd == "teamchat" && p.level == mainlevel)
            {
                if (GetPlayer(p) != null)
                {
                    Data d = GetPlayer(p);
                    if (d.chatting)
                    {
                        Player.SendMessage(d.p, "You are no longer chatting with your team!");
                        d.chatting = !d.chatting;
                    }
                    else
                    {
                        Player.SendMessage(d.p, "You are now chatting with your team!");
                        d.chatting = !d.chatting;
                    }
                    Plugins.Plugin.CancelPlayerEvent(Plugins.Events.PlayerCommand, p);
                }
            }
            if (cmd == "goto")
            {
                if (message == "ctf" && p.level != mainlevel)
                {
                    if (blueteam.members.Count > redteam.members.Count)
                    {
                        if (GetPlayer(p) == null)
                            cache.Add(new Data(false, p));
                        else
                        {
                            GetPlayer(p).hasflag = false;
                            GetPlayer(p).blue = false;
                        }
                        redteam.Add(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + c.Parse("red") + "joined the RED Team");
                        Player.SendMessage(p, c.Parse("red") + "You are now on the red team!");
                    }
                    else if (redteam.members.Count > blueteam.members.Count)
                    {
                        if (GetPlayer(p) == null)
                            cache.Add(new Data(true, p));
                        else
                        {
                            GetPlayer(p).hasflag = false;
                            GetPlayer(p).blue = true;
                        }
                        blueteam.Add(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + c.Parse("blue") + "joined the BLUE Team");
                        Player.SendMessage(p, c.Parse("blue") + "You are now on the blue team!");
                    }
                    else if (new Random().Next(2) == 0)
                    {
                        if (GetPlayer(p) == null)
                            cache.Add(new Data(false, p));
                        else
                        {
                            GetPlayer(p).hasflag = false;
                            GetPlayer(p).blue = false;
                        }
                        redteam.Add(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + c.Parse("red") + "joined the RED Team");
                        Player.SendMessage(p, c.Parse("red") + "You are now on the red team!");
                    }
                    else
                    {
                        if (GetPlayer(p) == null)
                            cache.Add(new Data(true, p));
                        else
                        {
                            GetPlayer(p).hasflag = false;
                            GetPlayer(p).blue = true;
                        }
                        blueteam.Add(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + c.Parse("blue") + "joined the BLUE Team");
                        Player.SendMessage(p, c.Parse("blue") + "You are now on the blue team!");
                    }
                }
                else if (message != "ctf" && p.level == mainlevel)
                {
                    if (blueteam.members.Contains(p))
                    {
                        //cache.Remove(GetPlayer(p));
                        blueteam.members.Remove(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + blueteam.color + "left the ctf game");
                    }
                    else if (redteam.members.Contains(p))
                    {
                        //cache.Remove(GetPlayer(p));
                        redteam.members.Remove(p);
                        Player.GlobalMessageLevel(mainlevel, p.color + p.name + " " + redteam.color + "left the ctf game");
                    }
                }
            }
        }
        void Player_PlayerChat(Player p, string message)
        {
            if (p.level == mainlevel)
            {
                if (GetPlayer(p).chatting)
                {
                    if (blueteam.members.Contains(p))
                    {
                        Player.players.ForEach(delegate(Player p1)
                        {
                            if (blueteam.members.Contains(p1))
                                Player.SendMessage(p1, blueteam.color + "<Team-Chat>" + p.color + p.name + ": " + c.Parse("white") + message);
                        });
                        Plugins.Plugin.CancelPlayerEvent(Plugins.Events.PlayerChat, p);
                    }
                    if (redteam.members.Contains(p))
                    {
                        Player.players.ForEach(delegate(Player p1)
                        {
                            if (redteam.members.Contains(p1))
                                Player.SendMessage(p1, redteam.color + "<Team-Chat>" + p.color + p.name + ": " + c.white + message);
                        });
                        Plugins.Plugin.CancelPlayerEvent(Plugins.Events.PlayerChat, p);
                    }
                }
            }
        }
        void Player_PlayerDeath(Player p, byte deathblock)
        {
            if (p.level == mainlevel)
            {
                if (GetPlayer(p).hasflag)
                {
                    if (redteam.members.Contains(p))
                    {
                        Player.GlobalMessageLevel(mainlevel, redteam.color + p.name + " DROPPED THE FLAG!");
                        GetPlayer(p).points -= caplose;
                        mainlevel.Blockchange(redbase.x, redbase.y, redbase.z, Block.red);
                    }
                    else if (blueteam.members.Contains(p))
                    {
                        Player.GlobalMessageLevel(mainlevel, blueteam.color + p.name + " DROPPED THE FLAG!");
                        GetPlayer(p).points -= caplose;
                        mainlevel.Blockchange(bluebase.x, bluebase.y, bluebase.z, Block.blue);
                    }
                    GetPlayer(p).hasflag = false;
                }
            }
        }
        bool OnSide(ushort z, Base b)
        {
            if (b.z < zline && z / 32 < zline)
                return true;
            else if (b.z > zline && z / 32 > zline)
                return true;
            else
                return false;
        }
        bool OnSide(Player p, Base b)
        {
            if (b.z < zline && p.pos[2] / 32 < zline)
                return true;
            else if (b.z > zline && p.pos[2] / 32 > zline)
                return true;
            else
                return false;
        }
        void Player_PlayerMove(Player p, ushort x, ushort y, ushort z)
        {
            if (p.level == mainlevel)
            {
                if (blueteam.members.Contains(p) && OnSide(p, bluebase))
                {
                    foreach (Player p1 in redteam.members)
                    {
                        if (Math.Abs(p1.pos[0] - x) < 2 && Math.Abs(p1.pos[1] - y) < 2 && Math.Abs(p1.pos[2] - z) < 2)
                        {
                            Player.SendMessage(p1, p.color + p.name + Server.DefaultColor + " tagged you!");
                            Random rand = new Random();
                            ushort xx = (ushort)(rand.Next(0, mainlevel.width * 32));
                            ushort yy = (ushort)(rand.Next(0, mainlevel.depth * 32));
                            ushort zz = (ushort)(rand.Next(0, mainlevel.height * 32));
                            while (mainlevel.GetTile((ushort)(xx / 32), (ushort)(yy / 32), (ushort)(zz / 32)) != Block.air && OnSide(zz, redbase))
                            {
                                xx = (ushort)(rand.Next(0, mainlevel.width * 32));
                                yy = (ushort)(rand.Next(0, mainlevel.depth * 32));
                                zz = (ushort)(rand.Next(0, mainlevel.height * 32));
                            }
                            p1.SendPos(0, xx, yy, zz, p1.rot[0], p1.rot[1]);
                            if (GetPlayer(p1).hasflag)
                            {
                                Player.GlobalMessageLevel(mainlevel, redteam.color + p.name + " DROPPED THE FLAG!");
                                GetPlayer(p1).points -= caplose;
                                mainlevel.Blockchange(redbase.x, redbase.y, redbase.z, Block.red);
                                GetPlayer(p).hasflag = false;
                            }
                            GetPlayer(p).points += tagpoint;
                            GetPlayer(p1).points -= taglose;
                            GetPlayer(p).tag++;
                        }
                    }
                }
                if (redteam.members.Contains(p) && OnSide(p, redbase))
                {
                    foreach (Player p1 in blueteam.members)
                    {
                        if (Math.Abs(p1.pos[0] - x) < 2 && Math.Abs(p1.pos[1] - y) < 2 && Math.Abs(p1.pos[2] - z) < 2)
                        {
                            Player.SendMessage(p1, p.color + p.name + Server.DefaultColor + " tagged you!");
                            Random rand = new Random();
                            ushort xx = (ushort)(rand.Next(0, mainlevel.width * 32));
                            ushort yy = (ushort)(rand.Next(0, mainlevel.depth * 32));
                            ushort zz = (ushort)(rand.Next(0, mainlevel.height * 32));
                            while (mainlevel.GetTile((ushort)(xx / 32), (ushort)(yy / 32), (ushort)(zz / 32)) != Block.air && OnSide(zz, bluebase))
                            {
                                xx = (ushort)(rand.Next(0, mainlevel.width * 32));
                                yy = (ushort)(rand.Next(0, mainlevel.depth * 32));
                                zz = (ushort)(rand.Next(0, mainlevel.height * 32));
                            }
                            p1.SendPos(0, xx, yy, zz, p1.rot[0], p1.rot[1]);
                            if (GetPlayer(p1).hasflag)
                            {
                                Player.GlobalMessageLevel(mainlevel, blueteam.color + p.name + " DROPPED THE FLAG!");
                                GetPlayer(p1).points -= caplose;
                                mainlevel.Blockchange(bluebase.x, bluebase.y, bluebase.z, Block.blue);
                                GetPlayer(p).hasflag = false;
                            }
                            GetPlayer(p).points += tagpoint;
                            GetPlayer(p1).points -= taglose;
                            GetPlayer(p).tag++;
                        }
                    }
                }
            }
        }
    }
}
