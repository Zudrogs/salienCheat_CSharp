using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http.Formatting;

namespace SalienCheat
{
    class Program
    {
        const int WaitTime = 110;
        static string Token;
        static List<string> ZonePaces = new List<string>();

        //TODO Add suport for external token file
        static void Main(string[] args)
        {
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



                Console.WriteLine("Zone: {0} on Planet: {1}", BestPlanetAndZone.BestZone.zone_position, BestPlanetAndZone.BestPlanet.id);


                Thread.Sleep(30000);
            } while(true);
        }

        static string LeaveCurrentGame(Planet bestPlanet = null)
        {
            Response playerData = null;
            do
            {
                playerData = ExecuteRequest("ITerritoryControlMinigameService/GetPlayerInfo", GetOrPost.Post, "access_token=" + Token).response;
                if(playerData.active_zone_game != null)
                    ExecuteRequest("IMiniGameService/LeaveGame", GetOrPost.Post, $"access_token={Token}&gameid={playerData.active_zone_game}");
            } while(playerData != null);

            string activePlanet = playerData.active_planet;

            if(bestPlanet.id != activePlanet && bestPlanet != null)
            {
                PrintWithColor($"   Leaving planet |]g{activePlanet}|]n because we want to be on |]g{bestPlanet.id} ({bestPlanet.state.name})");
                PrintWithColor($"   Time accumulated on planet |]g{activePlanet}|]n: |]y{playerData.time_on_planet}");
                Console.WriteLine();

                ExecuteRequest("IMiniGameService/LeaveGame", GetOrPost.Post, $"access_token={Token}&gameid={activePlanet}");
            }

            return playerData.active_planet;
        }

        static Result GetBestPlanetAndZone()
        {
            Planet[] planets = ExecuteRequest("ITerritoryControlMinigameService/GetPlanets", GetOrPost.Get, "active_only=1&language=english").response.planets;

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
                planetResult = zoneResult;

                PrintWithColor(string.Format(">> Planet |]g{0}|]n - Captured: |]g{1:p}|]n - High: |]y{2}|]n - Medium: |]y{3}|]n - Low: |]y{4}|]n - Players: |]y{5:n0} |]g({6})",
                                            p.id, p.state.capture_progress, planetResult.HighZones, planetResult.MediumZones, planetResult.LowZones, p.state.current_players, p.state.name));

                int localSortKey = 0;

                if(planetResult.LowZones > 0)
                    localSortKey =+ 99 - planetResult.LowZones;
                if(planetResult.MediumZones > 0)
                    localSortKey =+ (int)Math.Pow(10, 2) * (99 - planetResult.MediumZones);
                if(planetResult.HighZones > 0)
                    localSortKey =+ (int)Math.Pow(10, 4) * (99 - planetResult.HighZones);

                if(localSortKey >= sortKey)
                    zoneResult.BestPlanet = p;
            }

            //PrintWithColor(string.Format(">> Next Zone is |]y{0}|]n (Captured: |]y{1:F2}|]n - Difficulty: |]y{2}|]n) on Planet |]g{3} ({4})"));


            return planetResult;
        }
        static Result GetPlanetState(string planetId)
        {
            string filter = "id=" + planetId + "&language=english";
            Zone[] zones = ExecuteRequest("ITerritoryControlMinigameService/GetPlanet", GetOrPost.Get, filter).response.planets[0].zones;

            if(zones.Length == 0 || zones == null)
            {
                return null;
            }

            Result zoneResult = new Result();
            //int zonePace;
            
            foreach(Zone z in zones)
            {
                if(z.captured)
                    continue;

                float cutoff = 0.99F;

                //TODO calculate/check zone pace HERE
                // '     Zone {yellow}%3d{normal} - Captured: {yellow}%5s%%{normal} - Cutoff: {yellow}%5s%%{normal} - Pace: {yellow}%6s%%{normal} - ETA: {yellow}%2dm %2ds{normal}',

                if(z.capture_progress >= cutoff)
                    continue;

                //Check for grater or equal: makes it so it always sets the "last and best" zone
                if(z >= zoneResult.BestZone)
                    zoneResult.BestZone = z;

                //Count number of each zone difficulty still active
                switch(z.difficulty)
                {
                    case 3: zoneResult.HighZones++; break;
                    case 2: zoneResult.MediumZones++; break;
                    case 1: zoneResult.LowZones++; break; 
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

        static Rootobject ExecuteRequest(string method, GetOrPost getOrPost, string filterOrContent = "")
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
                if(!result.IsSuccessStatusCode)
                {
                    

                    //PrintWithColor(string.Format("|[r!! {0} failed - Error:  - {1}", method, filter));
                }

                return result.Content.ReadAsAsync<Rootobject>().Result;

            } 
        }


        static void StartUpMessage()
        {
            Console.WriteLine("Please get a token from here: https://steamcommunity.com/saliengame/gettoken and enter it:");
            Token = Console.ReadLine();
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

                Console.Write(s.Remove(0, 2));
            }
            Console.WriteLine();
        }
    }
    enum GetOrPost { Get, Post }
    enum Difficulty { Low = 1, Medium = 2, High = 3 }
}
