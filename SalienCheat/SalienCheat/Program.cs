using System;
using System.Collections.Generic;
using System.Net.Http;

namespace SalienCheat
{
    class Program
    {
        const int WaitTime = 110;
        static string Token;
        static List<string> KnownPlanets = new List<string>();
        static List<string> SkippedPlanets = new List<string>();
        static List<string> ZonePaces = new List<string>();

        //TODO Add suport for external token file
        static void Main(string[] args)
        {
            StartUpMessage();

            bool BestPlanetAndZone;
            



#if DEBUG
            Console.ReadKey();
#endif
        }




        static bool GetBestPlanetAndZone()
        {
            string[] Planets = SendGet("ITerritoryControlMinigameService/GetPlanets", "active_only=1&language=english");


            return true;
        }

        static void SendPost(string method, string data)
        {
            ExecuteRequest(method, "https://community.steam-api.com/" + method + "/v0001/?", data);
        }
        static string[] SendGet(string method, string data)
        {
            ExecuteRequest(method, "https://community.steam-api.com/" + method + "/v0001/", data);
            return null;
        }

        static HttpClient GetHttpClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://steamcommunity.com/saliengame/play");
            client.Timeout = new TimeSpan(3000);
            return client;
        }

        static void ExecuteRequest(string method, string url, string data)
        {

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

            PrintWithColor("|]g-- If you want to support us, join our group");
            PrintWithColor("|]g--|]y https://steamcommunity.com/groups/steamdb");
            PrintWithColor("|]g-- and set us as your clan on");
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
                if(String.IsNullOrWhiteSpace(s))
                    continue;
                if(s.StartsWith("]n")) 
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
}
