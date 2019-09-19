﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using InsurgencySandstormRcon.Dialogs;

namespace InsurgencySandstormRcon
{
    public partial class Form1 : Form
    {
        ServerManagmentDialog serverManagmentDialog;
        ScenarioListDialog scenarioListDialog;
        MapListDialog mapListDialog;
        BanListDialog banListDialog;
        KickPlayersDialog kickPlayersDialog;
        BanPlayersDialog banPlayersDialog;

        RconServerManager rconManager;
        RconServer selectedServer = null;
        RconPacketLog rconDebug = new RconPacketLog();

        Timer autoUpdateTimer;

        public Form1()
        {
            InitializeComponent();

            rconManager = new RconServerManager();
            //rconManager.Test();
            rconManager.Load();
            toolStripDropDownButton1.DropDownItemClicked += ToolStripDropDownButton1_DropDownItemClicked;

            autoUpdateTimer = new Timer();
            autoUpdateTimer.Interval = 30000;
            autoUpdateTimer.Tick += AutoUpdateTimer_Tick;
            autoUpdateTimer.Start();

            serverManagmentDialog = new ServerManagmentDialog();
            scenarioListDialog = new ScenarioListDialog();
            mapListDialog = new MapListDialog();
            banListDialog = new BanListDialog();
            kickPlayersDialog = new KickPlayersDialog();
            banPlayersDialog = new BanPlayersDialog();
        }

        public void UpdateServerInfo()
        {
            GetPlayerList();
            GetCurrentScenario();
        }

        public void GetPlayerList()
        {
            if(rconManager.ActiveServer != null)
            {
                listView1.Items.Clear();
                string data = rconManager.ActiveServer.Rcon.SendCommand("listplayers");
                string[] parts = data.Split('|');

                for (int i = 5; i < parts.Length; i += 5)
                {
                    if ((i + 5) < parts.Length)
                    {
                        string id = parts[i].Trim();
                        string name = parts[i + 1].Trim();
                        string netid = parts[i + 2].Trim();
                        string ip = parts[i + 3].Trim();
                        string score = parts[i + 4].Trim();

                        if (netid != "INVALID")
                        {
                            ListViewItem lvi = new ListViewItem();
                            lvi.Text = name;
                            lvi.SubItems.Add(score);
                            lvi.Tag = netid;

                            listView1.Items.Add(lvi);
                        }
                    }
                }

                if(listView1.Items.Count == 0)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Text = "No players currently on server";
                    listView1.Items.Add(lvi);
                }
            }
            else
            {
                ListViewItem lvi = new ListViewItem();
                lvi.Text = "No servers are currently selected. Please select one";
                listView1.Items.Add(lvi);
            }
        }

        public void GetCurrentScenario()
        {
            if(rconManager.ActiveServer != null)
            {
                string data = rconManager.ActiveServer.Rcon.SendCommand("listgamemodeproperties scenario");
                string[] lines = data.Split('\n');

                foreach(string line in lines)
                {
                    if(line.Contains("MultiplayerScenario = "))
                    {
                        string name = ParseScenarioString(line);
                        label3.Text = name;
                    }
                }
            }
        }

        protected string ParseScenarioString(string input)
        {
            Regex regex = new Regex("(.Scenario_[A-Za-z_]+')");

            if(regex.IsMatch(input))
            {
                MatchCollection matches = regex.Matches(input);

                foreach(Match m in matches)
                {
                    if(m.Value.StartsWith(".Scenario"))
                    {
                        string scenarioStr = m.Value;
                        scenarioStr = scenarioStr.Replace("'", string.Empty);
                        scenarioStr = scenarioStr.Replace(".", string.Empty);

                        string[] parts = scenarioStr.Split('_');

                        if (parts.Length == 3)
                        {
                            return string.Format("{0} on {1}", parts[2], parts[1]);
                        }
                        else if (parts.Length == 4)
                        {
                            return string.Format("{0} {1} on {2}", parts[2], parts[3], parts[1]);
                        }
                    }
                }
            }
            return string.Empty;
        }

        public void UnselectAllServers()
        {
            selectedServer = null;
            rconManager.ActiveServer = null;

            foreach (ToolStripItem item in toolStripDropDownButton1.DropDownItems)
            {
                if (item.GetType() == typeof(ToolStripSeparator))
                    continue;
                else if (item.GetType() == typeof(ToolStripMenuItem))
                {
                    ToolStripMenuItem mi = (ToolStripMenuItem)item;
                    mi.Checked = false;
                }
            }
        }

        public void ReloadServers()
        {
            for (int i = 0; i < toolStripDropDownButton1.DropDownItems.Count; i++)
            {
                if (i == 0 || i == 1)
                    continue;
                else
                {
                    toolStripDropDownButton1.DropDownItems.RemoveAt(i);
                }
            }

            foreach (RconServer server in rconManager.Servers)
            {
                ToolStripMenuItem menuItem = new ToolStripMenuItem();
                menuItem.Text = server.ToString();
                menuItem.Tag = server;
                toolStripDropDownButton1.DropDownItems.Add(menuItem);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            UnselectAllServers();
            ReloadServers();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            rconManager.Save();
            base.OnClosing(e);
        }

        protected void AddMessageToConsole(string msg)
        {
            if (msg != string.Empty)
            {
                richTextBox1.Text += msg;
                richTextBox1.Text += "\n";
            }
        }

        private void AutoUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateServerInfo();
        }

        private void ToolStripDropDownButton1_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            UnselectAllServers();
            ToolStripItem menuItem = e.ClickedItem;

            if (menuItem.Tag == null)
                return;
            else
            {
                rconManager.ActiveServer = (RconServer)menuItem.Tag;
                ((ToolStripMenuItem)menuItem).Checked = true;
                //TODO : Grab some data and run the inital lookup of server info, etc
            }
        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if(!string.IsNullOrEmpty(textBox3.Text))
            {
                string cmd = textBox3.Text;
                Console.WriteLine("Sending command {0}", cmd);
                if(rconManager.ActiveServer != null)
                {
                    //TODO : Setup allowed command whitelist to check commands against
                    string output = rconManager.ActiveServer.Rcon.SendCommand(cmd);
                    AddMessageToConsole(output);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();

            MessageBox.Show("Current packets in debug : " + RconPacketLog.Instance.Packets.Count);

            foreach (RconDebugPacket packet in RconPacketLog.Instance.Packets)
            {
                ListViewItem lvi = new ListViewItem();

                if (packet.Direction == PacketDirection.Sent)
                    lvi.Text = "Sent";
                else
                    lvi.Text = "Received";

                lvi.SubItems.Add(packet.Size.ToString());
                lvi.SubItems.Add(packet.Id.ToString());

                if(packet.Direction == PacketDirection.Sent)
                {
                    if (packet.Type == 0x03)
                        lvi.SubItems.Add("SERVERDATA_AUTH");
                    else if (packet.Type == 0x02)
                        lvi.SubItems.Add("SERVERDATA_EXECCOMMAND");
                    else if (packet.Type == 0x00)
                        lvi.SubItems.Add("SERVERDATA_EMPTY_PACKET");
                }
                else
                {
                    if (packet.Type == 0x02)
                        lvi.SubItems.Add("SERVERDATA_AUTH_RESPONSE");
                    else if (packet.Type == 0x00)
                        lvi.SubItems.Add("SERVERDATA_RESPONSE_VALUE");
                    else
                        lvi.SubItems.Add("SERVERDATA_VALUE_CONT");
                }
                //lvi.SubItems.Add(packet.Type.ToString());
                lvi.SubItems.Add(packet.Data);

                listView2.Items.Add(lvi);
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            UpdateServerInfo();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            int updateFreq = (int)numericUpDown1.Value;
            autoUpdateTimer.Stop();
            autoUpdateTimer.Interval = (updateFreq * 1000);
            autoUpdateTimer.Start();
            
        }

        private void manageConnectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(serverManagmentDialog.ShowDialog() == DialogResult.OK)
            {
                //The server management dialog should have taking care of adding / removing servers from the server list
                //We just need to reload the servers from the server list.

                UnselectAllServers();
                ReloadServers();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (rconManager.ActiveServer != null)
            {
                string data = rconManager.ActiveServer.Rcon.SendCommand("maps");
                string[] maps = data.Split('\n');

                mapListDialog.ShowDialog(maps, false);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (rconManager.ActiveServer != null)
            {
                string data = rconManager.ActiveServer.Rcon.SendCommand("maps");
                string[] maps = data.Split('\n');

                if(mapListDialog.ShowDialog(maps, true) == DialogResult.OK)
                {
                    string map = mapListDialog.SelectedMap;
                    //rconManager.ActiveServer.Rcon.SendCommand("travel " + map);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (rconManager.ActiveServer != null)
            {
                string data = rconManager.ActiveServer.Rcon.SendCommand("scenarios");
                string[] scenarioList = data.Split('\n');

                for (int i = 0; i < scenarioList.Length; i++)
                {
                    int start = scenarioList[i].IndexOf("(");
                    int end = scenarioList[i].IndexOf(")");
                    int length = (end - start);

                    if (start >= 0 && end > start)
                    {
                        scenarioList[i] = scenarioList[i].Remove(start, length + 1).Trim();
                    }
                }

                if (scenarioListDialog.ShowDialog(scenarioList) == DialogResult.OK)
                {
                    string scenario = scenarioListDialog.SelectedScenario;
                    string output = rconManager.ActiveServer.Rcon.SendCommand("travelscenario " + scenario);
                    AddMessageToConsole(output);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Ban player
            ListView.SelectedListViewItemCollection selectedItems = listView1.SelectedItems;
            ListViewItem[] items = new ListViewItem[selectedItems.Count];

            for(int i = 0; i < selectedItems.Count; i++)
            {
                items[i] = (ListViewItem)selectedItems[i].Clone();
            }

            if (selectedItems.Count > 0)
            {
                banPlayersDialog.ShowDialog(items);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Kick player
            ListView.SelectedListViewItemCollection selectedItems = listView1.SelectedItems;
            ListViewItem[] items = new ListViewItem[selectedItems.Count];

            for (int i = 0; i < selectedItems.Count; i++)
            {
                items[i] = (ListViewItem)selectedItems[i].Clone();
            }

            if (selectedItems.Count > 0)
            {
                kickPlayersDialog.ShowDialog(items);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //Restart round
            if (rconManager.ActiveServer != null)
            {
                DialogResult dr = MessageBox.Show("Would you like to swap teams when restarting round?",
                    "Restart round", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    rconManager.ActiveServer.Rcon.SendCommand("restartround 1");
                    MessageBox.Show("Restarting the round");
                }
                else if (dr == DialogResult.No)
                {
                    rconManager.ActiveServer.Rcon.SendCommand("restartround 0");
                    MessageBox.Show("Restarting the round and swapping teams");
                }
            }
            else
            {
                MessageBox.Show("No server is currenly selected / active");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (rconManager.ActiveServer != null)
            {
                string text = textBox1.Text;
                string formattedText = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(text));
                rconManager.ActiveServer.Rcon.SendCommand("say " + formattedText);
            }
            else
            {
                MessageBox.Show("No server is currently selected / active");
            }
        }
    }
}
