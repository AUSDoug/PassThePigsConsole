using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//File:   DecisionTrees.cs
//Author: Douglas John Spangenberg
//Date:   22nd September 2019

//A class for the AI decision trees. Initialises with three standard sets of rules,
//with an option to load user defined rules.
//All rules have a descriptive string associated with them (for logging), and an ID Number
//for storage and retrieval.


namespace PassThePigsConsole
{
    class AIDecisionTree
    {
        //Human readable, descriptive string
        public string desc;
        //Integer assigned to the ruleset; used for referencing in the ruleset storage
        public int id;

        //Decisions to roll or pass are likely to be made based off of
        //AI current score, current score for turn, and Opponent total score
        public int cpuTotal, cpuTurn, opponentTotal;

        public AIDecisionTree(String descIn, int idIn)
        {
            this.desc = descIn;
            this.id = idIn;
        }

        public static void initialiser()
        {
            AIDecisionTree basic = new AIDecisionTree("basic", 0);
            AIDecisionTree random = new AIDecisionTree("random", 1);
            AIDecisionTree aggressive = new AIDecisionTree("aggressive", 2);
        }

        //Runs the chosen AI Decision Tree
        //Returns the result of that tree as a Bool.
        public static Boolean decision(AIDecisionTree ruleIn)
        {
            return false;
        }
    }
}
