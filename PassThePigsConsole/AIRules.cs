using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

//File:   DecisionTrees.cs
//Author: Douglas John Spangenberg
//Date:   22nd September 2019

//A class for the AI decision trees. Initialises with three standard sets of rules,
//with an option to load user defined rules.
//All rules have a descriptive string associated with them (for logging), and an ID Number
//for storage and retrieval.


namespace PassThePigsConsole
{
    class AIRules
    {
        //'Basic' method; Well-rounded, and the most consistent.
        //Falls back on the 'stop at 23 rule', as per Gorman's paper 'Analytics, Pedagogy and the Pass the Pigs Game'.
        //Takes four variables; Logging mode, and the three scored by which it makes it's 'decisions'
        public static Boolean basic(int logMode, string name, int myTotal, int myTurn, int opponentTotal)
        {
			if (myTurn < 1){
				Trace.WriteLine(name + ": Rolling because haven't rolled yet this turn. \n");
				return true;
			}	 
			if (myTurn + myTotal > 90 && myTurn <= 30)
			{
                if (logMode == 1)Trace.WriteLine(name + ": Rolling, because I'm about to win and it isn't pushing my luck.\n");
				return true;
			}
			//If we think the human is about to win.
			if (opponentTotal >= 90 && ((myTotal + myTurn) <= 90))
			{
                if (logMode == 1)Trace.WriteLine(name + ": Rolling because Opponenent is closing in on a win.\n");
				return true;
			}
			//If we're nearly at the win, and have a buffer, don't be greedy.
			if (myTurn > 0 && myTotal >= 90 && opponentTotal <= 50)
			{
				if(logMode==1)Trace.WriteLine(name + ": Not rolling because I'm not greedy; Opponent is a long way behind, and I am close to winning.\n");
				return false;
			}
			//Never get greedy; Scored more than 60 this turn, play safe and Pass the Pigs
			if (myTurn >= 60)
			{
				if(logMode==1)Trace.WriteLine(name + ": Not pushing my luck after scoring 60+ on this turn.\n");
				return false;
			}
            //If the CPU is on Thirty (30) or greater for this, be content with that UNLESS the human is at Seventy-Six (76) or above AND the CPU is below 50.
            if ((myTurn > 29) && (opponentTotal < 76) && (myTotal < 50))
            {
                if(logMode==1)Trace.WriteLine(name + ": Not rolling; Had a good run here, opponent isn't too far ahead.\n");
                return false;
            }
            //If the CPU total is Zero (0) and, on this turn, they have amassed at least fiteen (15) points, don't roll.
            if ((myTotal == 0) && (myTurn > 14))
            {
                if (logMode==1)Trace.WriteLine(name + ": Not rolling, because I want to get off the mark.\n");
                return false;
            }
            //Easy decision; If opponent is ahead by cpuTurn+25, we will roll.
            if ((opponentTotal - myTotal) >= (myTurn + 25))
            {

                if(logMode==1)Trace.WriteLine(name + ": Rolling because, if I Pass now, Opponent will be ahead by at least Twenty Five.\n");
                return true;
            }
            if (((myTotal + myTurn) > opponentTotal) && (myTotal > 0 && opponentTotal > 0) && myTurn > 0)
            {

                if(logMode==1)Trace.WriteLine(name + ": Not rolling after exhausting the other options, because I'll be ahead.\n");
                return false;
            }
            if (myTurn > 23)
            {
                if(logMode==1)Trace.WriteLine(name + ": Exhausted other reasons, am not rolling because I've reached 23.\n");
                return false;
            }
            else
            {
                if(logMode==1)Trace.WriteLine(name + ": Exhausted other reasons, am rolling because I've not reached 23.\n");
                return true;
            }

        }

    }
}
