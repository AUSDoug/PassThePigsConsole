using System;

//File:   DecisionTrees.cs
//Author: Douglas John Spangenberg
//Date:   22nd September 2019

//The AI decision trees. Each 'rollDecision*' method returns true to roll, false to
//pass, and narrates its reasoning through GameLog.Ai (the "AI" logging channel).

namespace PassThePigsConsole
{
    //Tunable knob for rollDecisionExpert: the accumulated-turn-points target at which
    //to stop rolling. Gorman's expected-value analysis puts the break-even at ~23
    //("0.21 * turnScore < 4.7"). Held as data so variants can be benchmarked without
    //recompiling (see Benchmark.cs).
    internal sealed class ExpertParams
    {
        public int BaseTarget = 23;

        //The "go for the win" / "gamble" threshold. Originally derived as 100-BaseTarget
        //(=77); benchmarking found that coupling was suboptimal - decoupling it and
        //sweeping independently found a real, ~0.7pp win-rate gain around 68, flat across
        //roughly 58-74 and falling off outside that. BaseTarget re-checked against the new
        //value and 23 is still best. See tools/hillclimb.py / memory for the sweep data.
        public int EndgameZone = 68;

        //Accepts "23" or "23,68" (BaseTarget,EndgameZone); a legacy "23,20,20,35,16" form
        //is tolerated (fields beyond the second are ignored).
        public static ExpertParams Parse(string spec)
        {
            string[] f = spec.Split(',');
            ExpertParams p = new ExpertParams { BaseTarget = int.Parse(f[0]) };
            if (f.Length > 1)
            {
                int endgameZone;
                if (int.TryParse(f[1], out endgameZone)) p.EndgameZone = endgameZone;
            }
            return p;
        }

        public override string ToString()
        {
            return BaseTarget + "," + EndgameZone;
        }
    }

    class AIDecisionTree
    {
        //Human readable, descriptive string
        public string desc;
        //Integer assigned to the ruleset; used for referencing in the ruleset storage
        public int id;

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

        //Records the AI's reasoning on the "AI" log channel and returns the decision,
        //so a rule can end a branch with `return ai(true, "why");`.
        private static bool ai(bool roll, string reason)
        {
            GameLog.Ai.Debug("{Player} {Decision} — {Reason}",
                Program.current.name, roll ? "rolls" : "holds", reason);
            return roll;
        }

        //'Basic' method; Well-rounded, and the most consistent.
        //Falls back on the 'stop at 23 rule', as per Gorman's paper 'Analytics, Pedagogy and the Pass the Pigs Game'.
        public static Boolean rollDecisionBasic()
        {
            int cpuTotal = Program.current.totalScore;
            int cpuTurn = Program.current.turnScore;
            int opponentTotal = (Program.current == Program.player1)
                                    ? Program.player2.totalScore
                                    : Program.player1.totalScore;

            if (cpuTurn < 1)
                return ai(true, "haven't rolled yet this turn");

            if (cpuTurn + cpuTotal > 90 && cpuTurn <= 30)
                return ai(true, "about to win and not pushing my luck");

            if (opponentTotal >= 90 && (cpuTotal + cpuTurn) <= 90)
                return ai(true, "opponent is closing in on a win");

            //Nearly at the win with a buffer - don't be greedy.
            if (cpuTurn > 0 && cpuTotal >= 90 && opponentTotal <= 50)
                return ai(false, "not greedy; opponent far behind and I'm nearly home");

            //Never get greedy.
            if (cpuTurn >= 60)
                return ai(false, "not pushing my luck after 60+ this turn");

            //On 30+ this turn, be content unless the opponent is at 76+ and I'm below 50.
            if (cpuTurn > 29 && opponentTotal < 76 && cpuTotal < 50)
                return ai(false, "had a good run, opponent isn't too far ahead");

            //On zero overall - bank a small turn to get off the mark.
            if (cpuTotal == 0 && cpuTurn > 14)
                return ai(false, "want to get off the mark");

            //If passing now would leave the opponent ahead by 25+, roll.
            if ((opponentTotal - cpuTotal) >= (cpuTurn + 25))
                return ai(true, "passing now leaves opponent 25+ ahead");

            if ((cpuTotal + cpuTurn) > opponentTotal && cpuTotal > 0 && opponentTotal > 0 && cpuTurn > 0)
                return ai(false, "other reasons exhausted; passing leaves me ahead");

            return cpuTurn > 23
                ? ai(false, "other reasons exhausted; reached 23")
                : ai(true, "other reasons exhausted; not yet at 23");
        }

        //lolSoRandom
        public static Boolean rollDecisionRandom()
        {
            int cpuTotal = Program.current.totalScore;
            int cpuTurn = Program.current.turnScore;

            if (cpuTotal + cpuTurn >= 100)
                return ai(false, "already won");

            int x = Program.randomNumber(0, 1000);
            return Program.isOdd(x)
                ? ai(false, "random draw " + x + " is odd")
                : ai(true, "random draw " + x + " is even");
        }

        //Aggressive thought process that loves to roll.
        public static Boolean rollAggressive()
        {
            int cpuTotal = Program.current.totalScore;
            int cpuTurn = Program.current.turnScore;
            int opponentTotal = (Program.current == Program.player1)
                                    ? Program.player2.totalScore
                                    : Program.player1.totalScore;

            if (cpuTotal + cpuTurn >= 100)
                return ai(false, "already won");

            //Aggressive but not stupid; unless the opponent is winning big, 50 is enough.
            if (cpuTurn >= 50 && opponentTotal <= 85)
                return ai(false, "50 points is enough in one turn");

            return ai(true, "I'm aggressive");
        }

        //'Expert' method; Gorman's "stop at 23" expected-value rule plus the endgame
        //awareness he sketches at the end of 'Analytics, Pedagogy and the Pass the Pigs Game'.
        //
        //Marginal analysis from the paper: each roll has a ~21% chance of a pig out and a
        //constant expected benefit of ~4.7 points, so rolling is worth it while
        //        0.21 * turnScore  <  4.7      ->   turnScore < ~22.4
        //hence "roll at 22, stop at 23". That maximises the expected score of a turn but is
        //not by itself optimal for WINNING, so near the end of the game this tree switches
        //to playing the probability of winning: once either player is within EndgameZone
        //points of 100, it presses on for the win (or gambles for it, if the opponent is the
        //one in range) instead of applying the stop-at-23 rule. That threshold was originally
        //100-BaseTarget (=77, "one good turn away"); benchmarking found opening the window
        //earlier, to ~68, wins ~0.7pp more often - see ExpertParams.EndgameZone.
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
            int endgameZone = p.EndgameZone;                   // independently tuned, see ExpertParams

            int myTotal = Program.current.totalScore;
            int myTurn = Program.current.turnScore;
            int oppTotal = (Program.current == Program.player1)
                                ? Program.player2.totalScore
                                : Program.player1.totalScore;

            //First roll of the turn: nothing banked, nothing to lose - always roll.
            if (myTurn < 1)
                return ai(true, "first roll of the turn, nothing at stake");

            //--- Endgame: play the probability of winning, not the expected value ---

            //This turn already wins the game - stop and take it.
            if (myTotal + myTurn >= WinScore)
                return ai(false, "this turn wins the game");

            //Close enough that a normal turn carries us home - just roll until it does.
            if (myTotal >= endgameZone)
                return ai(true, "within one turn of victory, rolling for the win");

            //Opponent is poised to win next turn and we can't win this turn:
            //a safe target loses the game, so gamble for the win now.
            if (oppTotal >= endgameZone)
                return ai(true, "opponent is one turn from winning, gambling for the win");

            //--- Otherwise: Gorman's expected-value stopping rule. ---
            return myTurn >= p.BaseTarget
                ? ai(false, "reached the stop-at-" + p.BaseTarget + " threshold")
                : ai(true, "turn score " + myTurn + " below " + p.BaseTarget);
        }

        //'EV' method; the pure expected-value heuristic from Gorman's paper, with nothing
        //bolted on. Each roll risks a ~21% pig out for a constant ~4.7 expected points, so
        //rolling is worth it while 0.21 * turnScore < 4.7, i.e. below ~23 accumulated points.
        //This tree ignores the opponent entirely - it is the "stop at 23" baseline that the
        //'expert' tree builds on. In benchmarking it does about as well as Expert against the
        //other bots, but Expert beats it head-to-head via the endgame play.
        public static Boolean rollDecisionEV()
        {
            const int Target = 23;      // 0.21 * 23 > 4.7  ->  stop
            const int WinScore = 100;

            int myTotal = Program.current.totalScore;
            int myTurn = Program.current.turnScore;

            //Free roll: nothing banked this turn, nothing to lose.
            if (myTurn < 1)
                return ai(true, "first roll of the turn, nothing at stake");

            //Already enough to win - take it.
            if (myTotal + myTurn >= WinScore)
                return ai(false, "this turn wins the game");

            return myTurn >= Target
                ? ai(false, "reached the stop-at-" + Target + " threshold")
                : ai(true, "turn score " + myTurn + " still below " + Target);
        }
    }
}
