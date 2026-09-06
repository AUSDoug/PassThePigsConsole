using System;
using System.Diagnostics;

//File:   Benchmark.cs
//Headless match runner used to tune the AI rulesets.
//
//Invoked as:
//  PassThePigsConsole.exe bench games=<n> seed=<n> p1=<spec> p2=<spec>
//
//where <spec> is one of:
//  basic | random | aggressive | ev
//  expert                       (Expert with the default stop-at-23 threshold)
//  expert:<n>                   (Expert with an explicit BaseTarget threshold, e.g. expert:26)
//
//Games alternate which side takes the first turn, so first-mover advantage is
//split evenly. One result line is printed to stdout:
//
//  RESULT p1=<spec> p2=<spec> games=<n> p1wins=<n> p2wins=<n> p1rate=<f> se=<f>

namespace PassThePigsConsole
{
    internal static class Benchmark
    {
        private const int WinScore = 100;
        private const int TurnSafetyCap = 5000;   //guards against two 'random' AIs stalling

        //Cumulative-free weight table for a single pig (out of 10000), matching pigInitialiser().
        private static readonly string[] PigNames =
            { "Side (No Dot)", "Side (Dot)", "Razorback", "Trotter", "Snouter", "Leaning Jowler" };
        private static readonly int[] PigWeights = { 3490, 3020, 2240, 880, 300, 70 };

        public static void Run(string[] args)
        {
            //Silence the trees' Trace.WriteLine calls.
            Trace.Listeners.Clear();

            int games = 20000;
            int seed = 1;
            string p1Spec = "expert";
            string p2Spec = "expert";

            foreach (string a in args)
            {
                int eq = a.IndexOf('=');
                if (eq < 0) continue;
                string key = a.Substring(0, eq);
                string val = a.Substring(eq + 1);
                switch (key)
                {
                    case "games": games = int.Parse(val); break;
                    case "seed": seed = int.Parse(val); break;
                    case "p1": p1Spec = val; break;
                    case "p2": p2Spec = val; break;
                }
            }

            int p1AI = ParseSpec(p1Spec, out ExpertParams p1Expert);
            int p2AI = ParseSpec(p2Spec, out ExpertParams p2Expert);

            AIDecisionTree.ExpertConfigP1 = p1Expert ?? new ExpertParams();
            AIDecisionTree.ExpertConfigP2 = p2Expert ?? new ExpertParams();

            Program.player1 = new Player("P1");
            Program.player2 = new Player("P2");
            Program.logMode = false;

            Random rng = new Random(seed);
            int p1Wins = 0;

            for (int g = 0; g < games; g++)
            {
                bool p1Starts = (g % 2 == 0);
                if (PlayGame(p1AI, p2AI, p1Starts, rng))
                {
                    p1Wins++;
                }
            }

            int p2Wins = games - p1Wins;
            double rate = (double)p1Wins / games;
            double se = Math.Sqrt(rate * (1.0 - rate) / games);

            Console.WriteLine(
                "RESULT p1=" + p1Spec + " p2=" + p2Spec + " games=" + games
                + " p1wins=" + p1Wins + " p2wins=" + p2Wins
                + " p1rate=" + rate.ToString("F4") + " se=" + se.ToString("F4"));
        }

        //Returns the AI id and, for "expert[:...]", the parsed parameters (else null).
        private static int ParseSpec(string spec, out ExpertParams expert)
        {
            expert = null;
            string name = spec;
            int colon = spec.IndexOf(':');
            if (colon >= 0)
            {
                name = spec.Substring(0, colon);
                expert = ExpertParams.Parse(spec.Substring(colon + 1));
            }

            switch (name)
            {
                case "random": return 1;
                case "aggressive": return 2;
                case "expert":
                    if (expert == null) expert = new ExpertParams();
                    return 3;
                case "ev": return 4;
                default: return 0; //basic
            }
        }

        //Plays one game to WinScore. Returns true if player1 won.
        //Mirrors Program's semantics: the win is only resolved when a turn ends
        //(voluntary pass or pig out), so a greedy tree can still bust past 100.
        private static bool PlayGame(int p1AI, int p2AI, bool p1Starts, Random rng)
        {
            Program.player1.totalScore = 0;
            Program.player1.turnScore = 0;
            Program.player2.totalScore = 0;
            Program.player2.turnScore = 0;

            Player onTurn = p1Starts ? Program.player1 : Program.player2;

            for (int turn = 0; turn < TurnSafetyCap; turn++)
            {
                int aiId = (onTurn == Program.player1) ? p1AI : p2AI;
                onTurn.turnScore = 0;
                Program.current = onTurn;

                while (Decide(aiId))
                {
                    int s = Program.Score(SamplePig(rng), SamplePig(rng));
                    if (s == 0)
                    {
                        onTurn.turnScore = 0;   //pig out
                        break;
                    }
                    onTurn.turnScore += s;
                    Program.current = onTurn;
                }

                onTurn.totalScore += onTurn.turnScore;
                onTurn.turnScore = 0;
                if (onTurn.totalScore >= WinScore)
                {
                    return onTurn == Program.player1;
                }

                onTurn = (onTurn == Program.player1) ? Program.player2 : Program.player1;
            }

            //Safety fallback: whoever is ahead.
            return Program.player1.totalScore >= Program.player2.totalScore;
        }

        private static bool Decide(int aiId)
        {
            switch (aiId)
            {
                case 1: return AIDecisionTree.rollDecisionRandom();
                case 2: return AIDecisionTree.rollAggressive();
                case 3: return AIDecisionTree.rollDecisionExpert();
                case 4: return AIDecisionTree.rollDecisionEV();
                default: return AIDecisionTree.rollDecisionBasic();
            }
        }

        private static string SamplePig(Random rng)
        {
            int r = rng.Next(10000);
            int acc = 0;
            for (int i = 0; i < PigWeights.Length; i++)
            {
                acc += PigWeights[i];
                if (r < acc) return PigNames[i];
            }
            return PigNames[PigNames.Length - 1];
        }
    }
}
