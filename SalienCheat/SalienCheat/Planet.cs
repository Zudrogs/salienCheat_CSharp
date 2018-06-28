using System;
using System.Collections.Generic;
using System.Text;

namespace SalienCheat
{
    public class Rootobject
    {
        public Response response { get; set; }
    }

    public class Response
    {
        public Zone_Info zone_info { get; set; }
        public string active_planet { get; set; }
        public int time_on_planet { get; set; }
        public string active_zone_game { get; set; }
        public string active_zone_position { get; set; }
        public int time_in_zone { get; set; }
        public string score { get; set; }
        public int level { get; set; }
        public string next_level_score { get; set; }

        public int old_score { get; set; }
        public int old_level { get; set; }
        public int new_score { get; set; }
        public int new_level { get; set; }

        public Planet[] planets { get; set; }
    }

    public class Planet :IComparable<Planet>
    {
        public string id { get; set; }
        public State state { get; set; }
        public Zone[] zones { get; set; }

        public int CompareTo(Planet other)
        {
            if(other == null)
                return 1;

            return id.CompareTo(other.id);
        }
    }

    public class State
    {
        public string name { get; set; }
        public Difficulty difficulty { get; set; }
        public bool active { get; set; }
        public int activation_time { get; set; }
        public int position { get; set; }
        public bool captured { get; set; }
        public float capture_progress { get; set; }
        public int total_joins { get; set; }
        public int current_players { get; set; }
        public int priority { get; set; }
        public string tag_ids { get; set; }
    }

    public class Zone
    {
        public int zone_position { get; set; }
        public int type { get; set; }
        public string gameid { get; set; }
        public Difficulty difficulty { get; set; }
        public bool captured { get; set; }
        public float capture_progress { get; set; }
    }

    public class Result
    {
        public int HighZones { get; set; }
        public int MediumZones { get; set; }
        public int LowZones { get; set; }
        public List<string> ZoneMessages { get; set; }
        public Planet BestPlanet { get; set; }
        public Zone BestZone { get; set; }
    }

    public class Zone_Info
    {
        public int zone_position { get; set; }
        public int type { get; set; }
        public string gameid { get; set; }
        public Difficulty difficulty { get; set; }
        public bool captured { get; set; }
        public float capture_progress { get; set; }
    }
}
