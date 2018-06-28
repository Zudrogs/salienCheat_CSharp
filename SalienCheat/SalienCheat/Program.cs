using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using System.Globalization;

namespace SalienCheat
{
    class Program
    {
        const int WaitTime = 100;
        static int oldScore = 0;
        static string Token;
        //static List<string> ZonePaces = new List<string>();

        //TODO Add suport for external token file
        static void Main(string[] args)
        {
            if(args.Length >= 1)
                Token = args[0];

            StartUpMessage();

            Result BestPlanetAndZone;
            do
            {
                BestPlanetAndZone = GetBestPlanetAndZone();
                if(BestPlanetAndZone == null)
                    Thread.Sleep(5000);
            } while(BestPlanetAndZone == null);

            do
            {
                Console.WriteLine();

                // Leave current game and planet and joins the best planet to be on
                string steamThinksPlanet;
                do
                {
                    steamThinksPlanet = LeaveCurrentGame(BestPlanetAndZone.BestPlanet);
                    if(BestPlanetAndZone.BestPlanet.id != steamThinksPlanet)
                    {
                        ExecuteRequest("ITerritoryControlMinigameService/JoinPlanet", GetOrPost.Post, $"id={BestPlanetAndZone.BestPlanet.id}&access_token={Token}");
                        steamThinksPlanet = LeaveCurrentGame();
                    }
                } while(steamThinksPlanet != BestPlanetAndZone.BestPlanet.id);

                // Join best zone
                Response tmpRes = ExecuteRequest("ITerritoryControlMinigameService/JoinZone", GetOrPost.Post, $"zone_position={BestPlanetAndZone.BestZone.zone_position}&access_token={Token}");
                // TODO Rescan planets if joining failed
                if(tmpRes == null)
                {
                    PrintWithColor("|]r!! Failed to join a zone, rescanning and restarting...");
                    Thread.Sleep(1000);

                    do
                    {
                        BestPlanetAndZone = GetBestPlanetAndZone();
                        if(BestPlanetAndZone == null)
                            Thread.Sleep(5000);
                    } while(BestPlanetAndZone == null);

                    // Restart the loop
                    continue;
                }
                Zone_Info joinedZone = tmpRes.zone_info;

                

                PrintWithColor($"++ Joined Zone |]y{joinedZone.zone_position}" +
                    $"|]n on Planet |]g{BestPlanetAndZone.BestPlanet.id}" +
                    $"|]n - Captured: |]y{joinedZone.capture_progress:p}" +
                    $"|]n - Difficulty: |]y{joinedZone.difficulty}");

                // TODO Check for scritp update

                // Waits 100 seconds before rescanning
                PrintWithColor($"   |]cWaiting {WaitTime} seconds before rescanning planets...");
                Thread.Sleep(100000);
                Console.WriteLine();

                // Scan for best planet and zone
                do
                {
                    BestPlanetAndZone = GetBestPlanetAndZone();
                    if(BestPlanetAndZone == null)
                        Thread.Sleep(5000);
                } while(BestPlanetAndZone == null);

                // Wait 10 seconds more before submitting score
                PrintWithColor("   |]cWaiting 10 remaining seconds before submitting score");
                Thread.Sleep(10000);

                // Report score
                Response score = ExecuteRequest("ITerritoryControlMinigameService/ReportScore", GetOrPost.Post, $"access_token={Token}&score={GetScoreForZone(joinedZone.difficulty)}&language=english");
                if(score == null)
                {
                    PrintWithColor("   |]rTrying again in 10 seconds...");
                    Thread.Sleep(10000);
                    score = ExecuteRequest("ITerritoryControlMinigameService/ReportScore", GetOrPost.Post, $"access_token={Token}&score={GetScoreForZone(joinedZone.difficulty)}&language=english");
                }
                
                if(score != null)
                {
                    Console.WriteLine();

                    // Store our own old score because the API may increment score while giving an error (e.g. a timeout)
                    if(oldScore == 0)
                        oldScore = score.old_score;

                    PrintWithColor($"++ Your Score: |]r{score.new_score:n0}" +
                        $"|]y (+{score.new_score - oldScore:n})" +
                        $"|]n - Current Level: |]g{score.new_level}" +
                        $"|]n ({score.new_score / int.Parse(score.next_level_score):p})");

                    oldScore = score.new_score;
                    int remainingScore = int.Parse(score.next_level_score) - oldScore;
                    int time = (remainingScore / GetScoreForZone(joinedZone.difficulty)) * (WaitTime / 60);

                    PrintWithColor($">> Next Level: |]y{score.next_level_score}" +
                        $"|]n XP - Remaining: |]y{remainingScore}" +
                        $"|]n XP - ETA: |]g{Math.Floor(time / 60.0)}h {time % 60}m");
                }

            } while(true);
        }

        static int GetScoreForZone(Difficulty difficulty)
        {
            int score = 0;
            switch(difficulty)
            {
                case Difficulty.High: score = 20; break;
                case Difficulty.Medium: score=  10; break;
                case Difficulty.Low: score = 5; break;
                default: score = 20; break;
            }
            return score * 120;
        }

        static string LeaveCurrentGame(Planet bestPlanet = null)
        {
            Response playerData = null;
            do
            {
                playerData = ExecuteRequest("ITerritoryControlMinigameService/GetPlayerInfo", GetOrPost.Post, "access_token=" + Token);
                if(!string.IsNullOrEmpty(playerData.active_zone_game))
                    ExecuteRequest("IMiniGameService/LeaveGame", GetOrPost.Post, $"access_token={Token}&gameid={playerData.active_zone_game}");
            } while(playerData == null);

            string activePlanet = playerData.active_planet;

            if(bestPlanet != null && bestPlanet.id != activePlanet)
            {
                PrintWithColor($"   Leaving planet |]g{activePlanet}|]n because we want to be on |]g{bestPlanet.id} ({bestPlanet.state.name})");
                PrintWithColor($"   Time accumulated on planet |]g{activePlanet}|]n: |]y{DateTime.ParseExact(playerData.time_on_planet.ToString(), "HHmmss", CultureInfo.CurrentCulture):T}");
                Console.WriteLine();

                ExecuteRequest("IMiniGameService/LeaveGame", GetOrPost.Post, $"access_token={Token}&gameid={activePlanet}");
            }

            return playerData.active_planet;
        }

        static Result GetBestPlanetAndZone()
        {
            Planet[] planets = ExecuteRequest("ITerritoryControlMinigameService/GetPlanets", GetOrPost.Get, "active_only=1&language=english").planets;

            if(planets.Length == 0 || planets == null)
                return null;

            Result planetResult = null;
            int sortKey = 0;

            foreach(Planet p in planets)
            {
                Result zoneResult = null;
                do
                {
                    zoneResult = GetPlanetState(p.id);
                } while(zoneResult == null);

                //Set the planet result to the same as zone result

                PrintWithColor(string.Format(">> Planet |]g{0}|]n - Captured: |]g{1:p}|]n - High: |]y{2}|]n - Medium: |]y{3}|]n - Low: |]y{4}|]n - Players: |]y{5:n0} |]g({6})",
                                            p.id, p.state.capture_progress, zoneResult.HighZones, zoneResult.MediumZones, zoneResult.LowZones, p.state.current_players, p.state.name));

                int localSortKey = 0;

                if(zoneResult.LowZones > 0)
                    localSortKey =+ 99 - zoneResult.LowZones;
                if(zoneResult.MediumZones > 0)
                    localSortKey =+ (int)Math.Pow(10, 2) * (99 - zoneResult.MediumZones);
                if(zoneResult.HighZones > 0)
                    localSortKey =+ (int)Math.Pow(10, 4) * (99 - zoneResult.HighZones);

                if(localSortKey >= sortKey)
                {
                    sortKey = localSortKey;
                    planetResult = zoneResult;
                    planetResult.BestPlanet = p;
                }
            }

            PrintWithColor($">> Next Zone is |]y{planetResult.BestZone.zone_position}" +
                $"|]n (Captured: |]y{planetResult.BestZone.capture_progress:p2}" +
                $"|]n - Difficulty: |]y{planetResult.BestZone.difficulty}" +
                $"|]n) on Planet |]g{planetResult.BestPlanet.id} ({planetResult.BestPlanet.state.name})");


            return planetResult;
        }
        static Result GetPlanetState(string planetId)
        {
            Zone[] zones = ExecuteRequest("ITerritoryControlMinigameService/GetPlanet", GetOrPost.Get, $"id={planetId}&language=english").planets[0].zones;

            if(zones.Length == 0 || zones == null)
            {
                return null;
            }

            Result zoneResult = new Result();
            zoneResult.BestZone = new Zone();
            //int zonePace;
            
            foreach(Zone z in zones)
            {
                if(z.captured)
                    continue;

                float cutoff = 0.98F;

                //TODO calculate/check zone pace HERE
                // '     Zone {yellow}%3d{normal} - Captured: {yellow}%5s%%{normal} - Cutoff: {yellow}%5s%%{normal} - Pace: {yellow}%6s%%{normal} - ETA: {yellow}%2dm %2ds{normal}',

                if(z.capture_progress >= cutoff)
                    continue;

                //Check for grater or equal: makes it so it always sets the "last and best" zone
                if(z.difficulty >= zoneResult.BestZone.difficulty)
                    zoneResult.BestZone = z;

                //Count number of each zone difficulty still active
                switch(z.difficulty)
                {
                    case Difficulty.High: zoneResult.HighZones++; break;
                    case Difficulty.Medium: zoneResult.MediumZones++; break;
                    case Difficulty.Low: zoneResult.LowZones++; break; 
                }
            }
            

            return zoneResult;
        }

        //Gets the HttpClient
        static HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://community.steam-api.com/");
            client.Timeout = new TimeSpan(0, 0, 30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3464.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("Application/json"));

            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Origin", "https://steamcommunity.com");
            client.DefaultRequestHeaders.Add("Referer", "https://steamcommunity.com/saliengame/play");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("Keep-Alive", "300");

            return client;
        }

        static Response ExecuteRequest(string method, GetOrPost getOrPost, string filterOrContent = "")
        {
            
            
            using(HttpClient client = GetHttpClient())
            {
                string url = "";
                HttpResponseMessage result = null;

                switch(getOrPost)
                {
                    case GetOrPost.Get:
                        url = method + "/v0001/?" + filterOrContent;
                        result = client.GetAsync(url).Result;
                        break;
                    case GetOrPost.Post:
                        url = method + "/v0001/";

                        List<KeyValuePair<string, string>> contentList = new List<KeyValuePair<string, string>>();
                        foreach(string s in filterOrContent.Split('&'))
                        {
                            string[] tmpSplit = s.Split('=');
                            contentList.Add(new KeyValuePair<string, string>(tmpSplit[0], tmpSplit[1]));
                        }

                        FormUrlEncodedContent content = new FormUrlEncodedContent(contentList);
                        result = client.PostAsync(url, content).Result;
                        break;
                }

                //TODO Error handling
                if(result.IsSuccessStatusCode)
                {
                    string eresult = result.Headers.GetValues("X-eresult").FirstOrDefault();

                    // If not successful display error message
                    if(eresult != "1")
                    {
                        string errorMessage = result.Headers.GetValues("X-error_message").FirstOrDefault();
                        PrintWithColor($"|]r!! {method} failed - EResult: {eresult} - {result.Content.ReadAsStringAsync().Result}");
                        PrintWithColor($"|]r!! API failed - {errorMessage}");

                        Console.WriteLine();

                        if(eresult == "15" && method == "ITerritoryControlMinigameService/RepresentClan")
                        {
                            PrintWithColor("|]gThis script was designed for SteamDB");
                            PrintWithColor("|]gIf you want to support it, join the group and represent it in game:");
                            PrintWithColor("|]yhttps://steamcommunity.com/groups/SteamDB");
                            Thread.Sleep(10000);
                        }
                        else if(eresult == "42" && method == "ITerritoryControlMinigameService/ReportScore")
                            PrintWithColor("|]r-- EResult 42 means zone has been captured while you were in it");
                        else if(eresult == "0" || eresult == "11")
                            PrintWithColor("|]r-- This problem should resolve itself, wait for a couple of minutes");
                        else if(eresult == "10")
                            PrintWithColor("|]r-- EResult 10 means Steam is busy");
                        else if(eresult == "93")
                            PrintWithColor("|]r-- EResult 93 means time is out of sync");

                        return null;
                    }

                    return result.Content.ReadAsAsync<Rootobject>().Result.response;
                }
                Console.WriteLine("!!!!ERROR!!!!");
                return null;
            } 
        }

        static void StartUpMessage()
        {
            if(string.IsNullOrEmpty(Token))
            {
                Console.WriteLine("Please get a token from here: https://steamcommunity.com/saliengame/gettoken and enter it:");
                Token = Console.ReadLine();
            }
            Console.Clear();
            Console.WriteLine("The script can be terminated at any time by pressing Ctrl-C");

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Welcome to SalienCheat for SteamDB");
            Console.BackgroundColor = ConsoleColor.Black;
            PrintWithColor("|]g-- This script is based on SteamDB's PhP script");
            PrintWithColor("|]g--|]y https://github.com/SteamDatabase/SalienCheat");
            PrintWithColor("|]g-- If you want to support them, join there group");
            PrintWithColor("|]g--|]y https://steamcommunity.com/groups/steamdb");
            PrintWithColor("|]g-- and set them as your clan on");
            PrintWithColor("|]g--|]y https://steamcommunity.com/saliengame/play");
            PrintWithColor("|]g-- Happy farming!");
        }
        static void PrintWithColor(string message)
        {
            // ]n == normal/gray
            // ]g == darkGreen
            // ]y == yellow
            // ]r == red
            // ]c == cyan

            string[] messageParts = message.Split('|');

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[{0:T}] ", DateTime.Now);

            foreach(string s in messageParts)
            {
                if(!s.StartsWith(']'))
                {
                    Console.Write(s);
                    continue;
                }
                else if(String.IsNullOrWhiteSpace(s))
                    continue;
                else if(s.StartsWith("]n"))
                    Console.ForegroundColor = ConsoleColor.Gray;
                else if(s.StartsWith("]g"))
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                else if(s.StartsWith("]y"))
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if(s.StartsWith("]r"))
                    Console.ForegroundColor = ConsoleColor.Red;
                else if(s.StartsWith("]c"))
                    Console.ForegroundColor = ConsoleColor.DarkCyan;

                Console.Write(s.Remove(0, 2));
            }
            Console.WriteLine();
        }
    }
    public enum GetOrPost { Get, Post }
    public enum Difficulty { Low = 1, Medium = 2, High = 3 }
}
