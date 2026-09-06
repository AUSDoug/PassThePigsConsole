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
    //Tunable knob for rollDecisionExpert: the accumulated-turn-points target at which
    //to stop rolling. Gorman's expected-value analysis puts the break-even at ~23
    //("0.21 * turnScore < 4.7"). Held as data so variants can be benchmarked without
    //recompiling (see Benchmark.cs).
    internal sealed class ExpertParams
    {
        public int BaseTarget = 23;

        //Accepts "23"; a legacy "23,20,20,35,16" form is tolerated (extra fields ignored).
        public static ExpertParams Parse(string spec)
        {
            return new ExpertParams { BaseTarget = int.Parse(spec.Split(',')[0]) };
        }

        public override string ToString()
        {
            return BaseTarget.ToString();
        }
    }

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
            AIDecisionTree expert = new AIDecisionTree("expert", 3);
            AIDecisionTree ev = new AIDecisionTree("ev", 4);
        }

        //Runs the chosen AI Decision Tree
        //Returns the result of that tree as a Bool.
        public static Boolean decision(AIDecisionTree ruleIn)
        {
            return false;
        }

        //'Basic' method; Well-rounded, and the most consistent.
        //Falls back on the 'stop at 23 rule', as per Gorman's paper 'Analytics, Pedagogy and the Pass the Pigs Game'.
        public static Boolean rollDecisionBasic()
        {
            //My total, my turn total, my opponent's total
            int cpuTotal, cpuTurn, opponentTotal;

            //Depending on which CPU is in control
            if (Program.current == Program.player1)
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
                opponentTotal = Program.player2.totalScore;
            }
            else
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
                opponentTotal = Program.player1.totalScore;
            }


            if (cpuTurn<1){
                Trace.WriteLine(Program.current.name + ": Rolling because haven't rolled yet this turn. \n");
                return true;
            }
         

            if (cpuTurn + cpuTotal > 90 && cpuTurn <= 30)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Rolling, because I'm about to win and it isn't pushing my luck.\n");
                }
                return true;
            }

            //If we think the human is about to win.
            if (opponentTotal >= 90 && ((cpuTotal + cpuTurn) <= 90))
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Rolling because Opponenent is closing in on a win.\n");
                }
                return true;
            }
            //If we're nearly at the win, and have a buffer, don't be greedy.
            if (cpuTurn > 0 && cpuTotal >= 90 && opponentTotal <= 50)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling because I'm not greedy; Opponent is a long way behind, and I am close to winning.\n");
                }
                return false;
            }
            //Never get greedy
            if (cpuTurn >= 60)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not pushing my luck after scoring 60+ on this turn.\n");
                }
                return false;
            }
            //If the CPU is on Thirty (30) or greater for this, be content with that UNLESS the human is at Seventy-Six (76) or above AND the CPU is below 50.
            if ((cpuTurn > 29) && (opponentTotal < 76) && (cpuTotal < 50))
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling; Had a good run here, opponent isn't too far ahead.\n");
                }
                return false;
            }
            //If the CPU total is Zero (0) and, on this turn, they have amassed at least fiteen (15) points, don't roll.
            if ((cpuTotal == 0) && (cpuTurn > 14))
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling, because I want to get off the mark.\n");
                }
                return false;
            }
            //Easy decision; If opponent is ahead by cpuTurn+25, we will roll.
            if ((opponentTotal - cpuTotal) >= (cpuTurn + 25))
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Rolling because, if I Pass now, Opponent will be ahead by at least Twenty Five.\n");
                }
                return true;
            }
            if (((cpuTotal + cpuTurn) > opponentTotal) && (cpuTotal > 0 && opponentTotal > 0) && cpuTurn > 0)
            {
                if (Program.logMode ==  true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling after exhausting the other options, because I'll be ahead.\n");
                }
                return false;
            }
            if (cpuTurn > 23)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Exhausted other reasons, am not rolling because I've reached 23.\n");
                }
                return false;
            }
            else
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Exhausted other reasons, am rolling because I've not reached 23.\n");
                }
                return true;
            }
            
            
        }

        //lolSoRandom
        public static Boolean rollDecisionRandom()
        {
            //My total, my turn total
            int cpuTotal, cpuTurn;

            //Depending on which CPU is in control
            if (Program.current == Program.player1)
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
            }
            else
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
            }

            //If I have reached or exceeded 100, no need to roll.
            if (cpuTotal + cpuTurn >= 100)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling because I've won.\n");
                }
                return false;
            }
            int x = Program.randomNumber(0, 1000);
            if (Program.isOdd(x))
            {
                Trace.WriteLine(Program.current.name + ": Not rolling because " + x + "(Random)\n");
                return false;
            }
            else
            {
                Trace.WriteLine(Program.current.name + ": Am rolling because " + x + "(Random)\n");
                return true;
            }
        }

        //Agressive thought process that loves to roll
        public static Boolean rollAggressive()
        {
            //My total, my turn total
            int cpuTotal, cpuTurn, opponentTotal;

            //Depending on which CPU is in control
            if (Program.current == Program.player1)
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
                opponentTotal = Program.player2.totalScore;
            }
            else
            {
                cpuTotal = Program.current.totalScore;
                cpuTurn = Program.current.turnScore;
                opponentTotal = Program.player1.totalScore;
            }

            //If I have reached or exceeded 100, no need to roll.
            if (cpuTotal + cpuTurn >= 100)
            {
                if (Program.logMode == true)
                {
                    Trace.WriteLine(Program.current.name + ": Not rolling because I've won.\n");
                }
                return false;
            }
            //Aggressive but not stupid; unless the opponent is winning big, be cool with fifty.
            else if (cpuTurn >= 50 && opponentTotal <= 85)
            {
                Trace.WriteLine(Program.current.name + ": Not rolling because 50 points is enough in one turn\n");
                return false;
            }
            //Otherwise roll.
            else
            {
                Trace.WriteLine(Program.current.name + ": Rolling because I'm aggressive\n");
                return true;
            }
        }

        //'Expert' method; Gorman's "stop at 23" expected-value rule plus the endgame
        //awareness he sketches at the end of 'Analytics, Pedagogy and the Pass the Pigs Game'.
        //
        //Marginal analysis from the paper: each roll has a ~21% chance of a pig out and a
        //constant expected benefit of ~4.7 points, so rolling is worth it while
        //        0.21 * turnScore  <  4.7      ->   turnScore < ~22.4
        //hence "roll at 22, stop at 23". That maximises the expected score of a turn but is
        //not by itself optimal for WINNING, so near the end of the game this tree switches
        //to playing the probability of winning: it presses on for the win when victory is in
        //range, and gambles for the win when the opponent is about to close the game out.
        //
        //(An earlier version also slid the target up/down by relative score - "chase when
        //behind, coast when ahead" - but benchmarking showed that layer was a net liability
        //against the other rulesets, so it was removed. The endgame overrides carried the
        //whole benefit. See tools/hillclimb.py and Benchmark.cs.)
        //
        //Per-CPU parameterisation. Normal play leaves both at the default; the benchmark
        //harness sets them so two BaseTarget values can play each other.
        public static ExpertParams ExpertConfigP1 = new ExpertParams();
        public static ExpertParams ExpertConfigP2 = new ExpertParams();

        public static Boolean rollDecisionExpert()
        {
            ExpertParams p = (Program.current == Program.player1) ? ExpertConfigP1 : ExpertConfigP2;
            return rollDecisionExpert(p);
        }

        public static Boolean rollDecisionExpert(ExpertParams p)
        {
            const int WinScore = 100;                          // total needed to win
            int endgameZone = WinScore - p.BaseTarget;         // ~one good turn from home

            int myTotal = Program.current.totalScore;
            int myTurn = Program.current.turnScore;
            int oppTotal = (Program.current == Program.player1)
                                ? Program.player2.totalScore
                                : Program.player1.totalScore;

            //First roll of the turn: nothing banked, nothing to lose - always roll.
            if (myTurn < 1)
                return expertTrace(true, "first roll of the turn, nothing at stake");

            //--- Endgame: play the probability of winning, not the expected value ---

            //This turn already wins the game - stop and take it.
            if (myTotal + myTurn >= WinScore)
                return expertTrace(false, "this turn wins the game");

            //Close enough that a normal turn carries us home - just roll until it does.
            if (myTotal >= endgameZone)
                return expertTrace(true, "within one turn of victory, rolling for the win");

            //Opponent is poised to win next turn and we can't win this turn:
            //a safe target loses the game, so gamble for the win now.
            if (oppTotal >= endgameZone)
                return expertTrace(true, "opponent is one turn from winning, gambling for the win");

            //--- Otherwise: Gorman's expected-value stopping rule. ---
            return myTurn >= p.BaseTarget
                ? expertTrace(false, "reached the stop-at-" + p.BaseTarget + " threshold")
                : expertTrace(true, "turn score " + myTurn + " below " + p.BaseTarget);
        }

        //Small logging helper so rollDecisionExpert doesn't repeat the logMode / Trace.WriteLine dance.
        private static Boolean expertTrace(Boolean roll, string reason)
        {
            if (Program.logMode)
                Trace.WriteLine(Program.current.name + (roll ? ": Rolling - " : ": Not rolling - ") + reason + "\n");
            return roll;
        }

        //'EV' method; the pure expected-value heuristic from Gorman's paper, with nothing
        //bolted on. Each roll risks a ~21% pig out for a constant ~4.7 expected points, so
        //rolling is worth it while 0.21 * turnScore < 4.7, i.e. below ~23 accumulated points.
        //This tree ignores the opponent entirely - it is the "stop at 23" baseline that the
        //'expert' tree builds on. In benchmarking it is actually the strongest of the trees
        //against the other bots, because defensive play only pays against an equal opponent.
        public static Boolean rollDecisionEV()
        {
            const int Target = 23;      // 0.21 * 23 > 4.7  ->  stop
            const int WinScore = 100;

            int myTotal = Program.current.totalScore;
            int myTurn = Program.current.turnScore;

            //Free roll: nothing banked this turn, nothing to lose.
            if (myTurn < 1)
                return evTrace(true, "first roll of the turn, nothing at stake");

            //Already enough to win - take it.
            if (myTotal + myTurn >= WinScore)
                return evTrace(false, "this turn wins the game");

            return myTurn >= Target
                ? evTrace(false, "reached the stop-at-" + Target + " threshold")
                : evTrace(true, "turn score " + myTurn + " still below " + Target);
        }

        private static Boolean evTrace(Boolean roll, string reason)
        {
            if (Program.logMode)
                Trace.WriteLine(Program.current.name + (roll ? ": Rolling - " : ": Not rolling - ") + reason + "\n");
            return roll;
        }
    }
}
