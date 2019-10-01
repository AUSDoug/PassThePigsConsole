using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Player class.
//Holds information and getters/setters for information relevant to each Player.
namespace PassThePigsConsole
{
    public class Player
    {
        public String name { get; set; }
        public int turnScore { get; set; }
        public int totalScore { get; set; }
        public int AIIndex { get; set; }

        //Constructor for the Player
        public Player(String name)
        {
            this.name = name;

        }

        //Constructor for an AI player
        public Player(String name, int AI)
        {
            this.name = name;
            this.AIIndex = AI;
        }
    }
}
