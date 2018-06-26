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
            do
            {
                BestPlanetAndZone = GetBestPlanetAndZone();
            } while(!BestPlanetAndZone);



#if DEBUG
            Console.ReadKey();
#endif
        }

        static void CurrentTimeWrite()
        {
            ConsoleColor tmpFColor = Console.ForegroundColor;
            ConsoleColor tmpBColor = Console.BackgroundColor;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[{0:T}] ", DateTime.Now);
            Console.ForegroundColor = tmpFColor;
            Console.BackgroundColor = tmpBColor;
        }

        static void StartUpMessage()
        {
            Console.WriteLine("Please get a token from here: https://steamcommunity.com/saliengame/gettoken and enter it:");
            Token = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("The script can be terminated at any time by pressing Ctrl-C");

            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.Gray;
            CurrentTimeWrite();
            Console.WriteLine("Welcome to SalienCheat for SteamDB");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            CurrentTimeWrite();
            Console.WriteLine("-- If you want to support us, join our group");
            CurrentTimeWrite();
            Console.Write("--");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" https://steamcommunity.com/groups/steamdb");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            CurrentTimeWrite();
            Console.WriteLine("-- and set us as your clan on");
            CurrentTimeWrite();
            Console.Write("--");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(" https://steamcommunity.com/saliengame/play");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            CurrentTimeWrite();
            Console.WriteLine("-- Happy farming!");
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
    }
}
