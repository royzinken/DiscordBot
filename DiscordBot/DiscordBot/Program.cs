using DSharpPlus;
using DSharpPlus.Entities;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        //Init de discord bot
        static DiscordClient discord;
        static DiscordChannel Channel_Chat;
        static DiscordChannel Channel_Craft;

        private static IniData ini;

        static void Main(string[] args)
        {
            LoadDiscordBot(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        static async Task LoadDiscordBot(string[] args)
        {
            if (!File.Exists(thisDir("Settings.ini")))
            {
                Console.WriteLine("[ERROR] Unable to load 'Divine_Settings.ini'.");
                Thread.Sleep(-1);
            }
            string strProvider = string.Empty;

            string HOST = Ini["DATABASE"]["Host"].Replace("\"", ""); // Run exe name
            string NAME = Ini["DATABASE"]["DB_Name"].Replace("\"", ""); // Run exe name
            string USER = Ini["DATABASE"]["DB_User"].Replace("\"", ""); // Run exe name
            string PASS = Ini["DATABASE"]["DB_Pass"].Replace("\"", ""); // Run exe name

            string Bot_Token = Ini["DISCORD_API"]["Token"].Replace("\"", ""); // Run exe name
            string Channel_ID_Chat = Ini["DISCORD_API"]["Channel_ID"].Replace("\"", ""); // Run exe name
            string Channel_ID_Crafting = Ini["DISCORD_API"]["Crafting_ID"].Replace("\"", ""); // Run exe name

            strProvider = "Provider=SQLOLEDB;Data Source=" + HOST + ";Initial Catalog=" + NAME + ";User ID=" + USER + ";Password=" + PASS + ";";

            try
            {
                discord = new DiscordClient(new DiscordConfiguration
                {
                    Token = Bot_Token,
                    TokenType = TokenType.Bot
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("[BOT TOKEN ERROR]" + ex.Message);
                Thread.Sleep(-1);
            }

            Console.WriteLine("BOT Token loaded!");
            try
            {
                Channel_Chat = await discord.GetChannelAsync((ulong)Int64.Parse(Channel_ID_Chat));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Channel ERROR]" + ex.Message);
                Thread.Sleep(-1);
            }

            Console.WriteLine("Discord Chat Channel Loaded!");
            try
            {
                Channel_Craft = await discord.GetChannelAsync((ulong)Int64.Parse(Channel_ID_Crafting));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Channel ERROR]" + ex.Message);
                Thread.Sleep(-1);
            }

            Console.WriteLine("Discord Crafting Channel Loaded!");

            OleDbConnection excelConnection = new OleDbConnection(strProvider);
            try
            {
                excelConnection.Open();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("[DATABASE ERROR]" + ex.Message);
                Thread.Sleep(-1);
            }
            Console.WriteLine("Connected to DB!");
            Console.WriteLine("============READY==============");
            bool loop = true;

            await discord.ConnectAsync();

            while (loop)
            {
                Console.WriteLine("~");
                string strQuery = "SELECT [Username],[Message],[Level],[Class],[Color],[region_name],[Region],[dbkey],[GuildName],[FullClassName],[Time] FROM [" + NAME + "].[dbo].[ChatHistory] WHERE DELIVERED_YN = 'N'";
                string QueryCrafting = "SELECT [Username],[Message],[Time],[attempts] FROM [" + NAME + "].[dbo].[CraftingHistory] WHERE DELIVERED_YN = 'N'";

                OleDbCommand dbCommand_Chat = new OleDbCommand(strQuery, excelConnection);
                OleDbCommand dbCommand_Crafting = new OleDbCommand(QueryCrafting, excelConnection);

                OleDbDataAdapter dataAdapter_Chat = new OleDbDataAdapter(dbCommand_Chat);
                OleDbDataAdapter dataAdapter_Crafting = new OleDbDataAdapter(dbCommand_Crafting);

                DataTable dTable_Chat = new DataTable();
                DataTable dTable_Crafting = new DataTable();

                dataAdapter_Chat.Fill(dTable_Chat);
                dataAdapter_Crafting.Fill(dTable_Crafting);

                foreach (DataRow row in dTable_Chat.Rows)
                {
                    try
                    {
                        string Name = row[0].ToString().TrimEnd();
                        string Message = row[1].ToString().TrimStart();
                        string Level = row[2].ToString().TrimStart();
                        string Class = row[3].ToString().TrimStart();
                        string Color = row[4].ToString().Trim();
                        string region_name = row[5].ToString().TrimStart();
                        string Region = row[6].ToString().TrimStart();
                        string dbkey = row[7].ToString().TrimStart();
                        string GuildName = row[8].ToString().TrimStart();
                        string FullClassName = row[9].ToString().TrimStart();
                        string TimeStamp = row[10].ToString();

                        if (String.IsNullOrEmpty(Name))
                            Name = "";
                        if (String.IsNullOrEmpty(Message))
                            Message = "";
                        if (String.IsNullOrEmpty(Level))
                            Level = "1";
                        if (String.IsNullOrEmpty(Class))
                            Class = "tf";
                        if (String.IsNullOrEmpty(Color))
                            Color = "#800080";
                        if (String.IsNullOrEmpty(region_name))
                            region_name = "Hakain's Crossing";
                        if (String.IsNullOrEmpty(Region))
                            Region = "map_c01";
                        if (String.IsNullOrEmpty(dbkey))
                            dbkey = "0";
                        if (String.IsNullOrEmpty(GuildName))
                            GuildName = "EMPTY";
                        if (String.IsNullOrEmpty(FullClassName))
                            FullClassName = "Assassin";
                        if (String.IsNullOrEmpty(TimeStamp))
                            TimeStamp = "1337-4-20 11:33:33.000";

                        Console.WriteLine(Name + ": " + Message);

                        var builder = new DiscordEmbedBuilder();

                        builder.WithAuthor(region_name);
                        builder.AddField("Message:", Message, true);
                        builder.AddField("Level:", Level, true);
                        builder.WithThumbnailUrl("https://ic.divinegames.to/images/logo-default.png");
                        builder.WithTitle(Name + " (" + GuildName + ")");
                        builder.WithUrl("https://ic.divinegames.to/ranking/character-" + dbkey + ".html");
                        builder.WithFooter(FullClassName, "https://ic.divinegames.to/media/images/Classes/" + FullClassName + "_pink.png");

                        builder.WithColor(new DiscordColor(Color));

                        builder.WithTimestamp(DateTime.Parse(TimeStamp));

                        await discord.SendMessageAsync(Channel_Chat, "", false, builder); //Get Messay

                        string Dilivered = "UPDATE [" + NAME + "].[dbo].[ChatHistory] SET DELIVERED_YN = 'Y' WHERE Message = '" + Message + "'";
                        OleDbCommand DeliverMessage = new OleDbCommand(Dilivered, excelConnection);
                        DeliverMessage.ExecuteNonQuery();
                        DeliverMessage.Dispose();
                        Thread.Sleep(4000);
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine("Chat ERROR:" + Error.Message);
                    }


                }
                foreach (DataRow row in dTable_Crafting.Rows)
                {
                    try
                    {
                        string Name = row[0].ToString().TrimEnd();
                        string Message = row[1].ToString().TrimStart();
                        string Time = row[2].ToString().TrimStart();
                        string Attempts = row[3].ToString();

                        if (String.IsNullOrEmpty(Name))
                            Name = "";
                        if (String.IsNullOrEmpty(Message))
                            Message = "";
                        if (String.IsNullOrEmpty(Time))
                            Time = "1337-4-20 11:33:33.000";
                        if (String.IsNullOrEmpty(Attempts))
                            Attempts = "-1";

                        Console.WriteLine(Name + ": " + Message);

                        var builder = new DiscordEmbedBuilder();

                        builder.WithAuthor(Name);
                        builder.AddField("Successfuly crafted", Message, true);
                        if (Attempts != "-1")
                            builder.AddField("Attempts:", Attempts, true);
                        builder.WithThumbnailUrl("https://ic.divinegames.to/images/logo-default.png");
                        //builder.WithTitle(Name);
                        //builder.WithUrl("https://ic.divinegames.to/ranking/character-" + dbkey + ".html");
                        //builder.WithFooter(FullClassName, "https://ic.divinegames.to/media/images/Classes/" + FullClassName + "_pink.png");

                        builder.WithColor(new DiscordColor("#B600FF"));

                        builder.WithTimestamp(DateTime.Parse(Time));

                        await discord.SendMessageAsync(Channel_Craft, "", false, builder); //Get Messay

                        string Dilivered = "UPDATE [" + NAME + "].[dbo].[CraftingHistory] SET DELIVERED_YN = 'Y' WHERE Message = '" + Message + "'";
                        OleDbCommand DeliverMessage = new OleDbCommand(Dilivered, excelConnection);
                        DeliverMessage.ExecuteNonQuery();
                        DeliverMessage.Dispose();
                        Thread.Sleep(4000);
                    }
                    catch (Exception Error)
                    {
                        Console.WriteLine("Craft ERROR:" + Error.Message);
                        //break;
                    }


                }

                dTable_Chat.Dispose();
                dataAdapter_Chat.Dispose();
                dbCommand_Chat.Dispose();

                dTable_Crafting.Dispose();
                dataAdapter_Crafting.Dispose();
                dbCommand_Crafting.Dispose();

                Thread.Sleep(2000);
            }
        }
        static string thisDir(string dir)
        {
            return Path.Combine(Environment.CurrentDirectory, dir);
        }
        public static IniData Ini
        {
            get
            {
                if (ini == null)
                {
                    FileIniDataParser parser = new FileIniDataParser();
                    ini = parser.LoadFile(thisDir("Divine_Settings.ini"));
                }
                return ini;
            }
        }
    }
}
