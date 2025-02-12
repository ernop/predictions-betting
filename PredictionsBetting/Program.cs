using System;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PredictionsBetting
{

    public class Program
    {
        public static string path = @"d:\\proj\\predictions-betting\\data-2023.csv";
        public static List<string> usernames = new List<string>() { "ivan", "jason", "ernie","daffy" };
        public static IEnumerable<User> users = usernames.Select(el => new User(el));

        static void Main(string[] args)
        {
            var cutout = false;
            if (cutout)
            {
                var ub = new List<UserBet>();
                var a = new User("A");
                var b = new User("B");
                var aa = new UserBet(a, 50);
                for (var ii = 0; ii < 100; ii++)
                {
                    var bb = new UserBet(b, ii);
                    var p = new Predicate("ABC", new List<UserBet>() { aa,bb}, false, DateTime.Now, true, "Yes", "A");
                    var methods = new Dictionary<PayoutMethod, Func<bool, UserBet, UserBet, Payout>>() { };
                    //methods[PayoutMethod.StraightBet] = EvaluateStraight;
                    //methods[PayoutMethod.DiffBet] = EvaluateDiffBets;
                    methods[PayoutMethod.FullContract] = EvaluateFullContract;
                    var payouts = EvaluateAccordingToFunction(p, methods[PayoutMethod.FullContract], PayoutMethod.FullContract);
                    if (payouts.Count() == 0)
                    {
                        Console.WriteLine($"FullContract at: A 50% vs B {ii}%, resolved 0%, No payment will be made.");
                        continue;
                    }
                    Console.WriteLine($"FullContract at: A 50% vs B {ii}%, resolved 0%, payout is: {payouts.First()}");
                    var payoutResults = SumPayouts(payouts);
                }
                for (var ii = 0; ii < 100; ii++)
                {
                    var bb = new UserBet(b, ii);
                    var p = new Predicate("ABC", new List<UserBet>() { aa, bb }, false, DateTime.Now, true, "Yes", "A");
                    var methods = new Dictionary<PayoutMethod, Func<bool, UserBet, UserBet, Payout>>() { };
                    //methods[PayoutMethod.StraightBet] = EvaluateStraight;
                    //methods[PayoutMethod.DiffBet] = EvaluateDiffBets;
                    methods[PayoutMethod.Multiplicative] = EvaluateMultiplicative;
                    var payouts = EvaluateAccordingToFunction(p, methods[PayoutMethod.Multiplicative], PayoutMethod.FullContract);
                    if (payouts.Count() == 0)
                    {
                        Console.WriteLine($"Multiplicative at: A 50% vs B {ii}%, resolved 0%, No payment will be made.");
                        continue;
                    }
                    Console.WriteLine($"Multiplicative at: A 50% vs B {ii}%, resolved 0%, payout is: {payouts.First()}");
                    var payoutResults = SumPayouts(payouts);
                }
                for (var ii = 0; ii < 100; ii++)
                {
                    var bb = new UserBet(b, ii);
                    var p = new Predicate("ABC", new List<UserBet>() { aa, bb }, false, DateTime.Now, true, "Yes", "A");
                    var methods = new Dictionary<PayoutMethod, Func<bool, UserBet, UserBet, Payout>>() { };
                    //methods[PayoutMethod.StraightBet] = EvaluateStraight;
                    methods[PayoutMethod.DiffBet] = EvaluateDiffBets;
                    //methods[PayoutMethod.FullContract] = EvaluateFullContract;
                    var payouts = EvaluateAccordingToFunction(p, methods[PayoutMethod.DiffBet], PayoutMethod.FullContract);
                    if (payouts.Count() == 0)
                    {
                        Console.WriteLine($"DiffBet at: A 50% vs B {ii}%, resolved 0%, No payment will be made.");
                        continue;
                    }
                    Console.WriteLine($"DiffBet at: A 50% vs B {ii}%, resolved 0%, payout is: {payouts.First()}");
                    var payoutResults = SumPayouts(payouts);
                }

                Console.ReadLine();
                return;
            }

            var rawPredicates = LoadPredicates();
            Evaluate(rawPredicates);

            foreach (var user in users)
            {
                var bs = CalculateBrier(rawPredicates, user);
                Console.WriteLine($"BrierScore: {user}: {bs}");
            }

            Console.ReadLine();
        }

        private static double CalculateBrier(IEnumerable<Predicate> preds, User user)
        {
            var tot = 0.0;
            foreach (var pred in preds)
            {
                var ub = pred.UserBets.Where(el => el.User.Name == user.Name).Single();
                var sub = pred.ResolvedTrue ? 1 : 0;
                var gap = Math.Pow((ub.Estimate/100.0) - sub, 2);
                tot += gap;
            }

            return tot;
        }

        internal static Dictionary<string, double> SumPayouts(IEnumerable<Payout> payouts)
        {
            var su = new Dictionary<string, double>();

            foreach (var p in payouts)
            {
                if (!su.ContainsKey(p.From.Name))
                {
                    su[p.From.Name] = 0.0;
                }
                if (!su.ContainsKey(p.To.Name))
                {
                    su[p.To.Name] = 0.0;
                }
                su[p.To.Name] += p.Amount;
                su[p.From.Name] -= p.Amount;
            }

            return su;
        }

        public static void Evaluate(IEnumerable<Predicate> preds)
        {
            var methods = new Dictionary<PayoutMethod, Func<bool, UserBet, UserBet, Payout>>() { };
            methods[PayoutMethod.StraightBet] = EvaluateStraight;
            methods[PayoutMethod.DiffBet] = EvaluateDiffBets;
            methods[PayoutMethod.FullContract] = EvaluateFullContract;
            methods[PayoutMethod.Multiplicative] = EvaluateMultiplicative;
            var totals = new Dictionary<PayoutMethod, Dictionary<string, double>>();
            var lines = new List<List<string>>();
            foreach (var pred in preds)
            {
                var line = new List<string>();
                //Console.WriteLine(pred.Text);
                line.Add(pred.Domain);
                line.Add(pred.Text);
                line.Add(pred.ResolvedTrue.ToString());
                //Console.WriteLine($"\tResolution: {pred.ResolvedTrue}");
                var userBets = string.Join("\t", pred.UserBets.OrderBy(el=>el.Estimate).Select(el => $"{el.User.Name}:{el.Estimate}%"));
                foreach (var l in pred.UserBets.OrderBy(el => el.User.Name).Select(el => $"{el.Estimate}"))
                {
                    line.Add(l);
                }
                Console.WriteLine("\tEstimates:\t" + userBets);
                Console.Write("\tScoring by method:\t");
                foreach (var methodEnum in methods.Keys)
                {
                    var method = methods[methodEnum];
                    if (!totals.ContainsKey(methodEnum)) { totals[methodEnum] = new Dictionary<string, double>(); }
                    var payouts = EvaluateAccordingToFunction(pred, method, methodEnum);
                    var payoutResults = SumPayouts(payouts);
                    var orderMult = 1;
                    if (!pred.ResolvedTrue)
                    {
                        orderMult = -1;
                    }
                    var moneyDescription = new List<string>();
                    if (payoutResults.Count() > 0)
                    {
                        moneyDescription = users
                            .OrderBy(user1 => payoutResults[user1.Name] * orderMult)
                            .Select(user2 => $"{user2}: {(payoutResults[user2.Name] >= 0 ? "+" : "")}{Math.Round(payoutResults[user2.Name], 3),-8}")
                            .ToList();
                    }
    
                    Console.WriteLine("");
                    Console.Write($"\t\t{methodEnum,-30}");
                    Console.Write("\t"+string.Join("", moneyDescription));
                    foreach (var pr in payoutResults)
                    {
                        if (!totals[methodEnum].ContainsKey(pr.Key)) { totals[methodEnum][pr.Key] = 0; }
                        totals[methodEnum][pr.Key] += pr.Value;
                    }
                    foreach (var user in users)
                    {
                        if (payoutResults.ContainsKey(user.Name))
                        {
                            line.Add(payoutResults[user.Name].ToString());
                        }
                        else
                        {
                            line.Add("no payout");
                        }
                    }
                }

                Console.WriteLine("\n\n\tRunning totals:");
                foreach (var methodName in totals.Keys)
                {
                    Console.WriteLine("\t\t"+methodName);
                    foreach (var user in users)
                    {
                        var val = Math.Round(totals[methodName][user.Name], 1);
                        var sign = "";
                        if (val > 0)
                        {
                            sign = "+";
                        }
                        Console.WriteLine($"\t\t\t{methodName} {user.Name} {sign}{val}");
                    }
                }
                lines.Add(line);
            }

            var joinedLines = new List<string>();
            foreach (var line in lines)
            {
                var joined = string.Join("\t", line);
                joinedLines.Add(joined);
                

            }
            System.IO.File.WriteAllLines("outpath-2023.csv", joinedLines);

        }

        public static Payout EvaluateDiffBets(bool resolution, UserBet winner, UserBet loser)
        {
            var gap = Math.Abs(winner.Estimate - loser.Estimate);
            return new Payout(gap / 100.0, loser.User, winner.User, PayoutMethod.DiffBet);
        }

        /// <summary>
        /// This method has some serious problems.  if the bet resolves true, 0.9 vs 0.90001
        /// will pay out MUCH LESS than 0.1 vs 0.10001.
        /// This is a defect against the principle of "insensitivity to small variations".
        /// </summary>
        public static Payout EvaluateFullContract(bool resolution, UserBet winner, UserBet loser)
        {
            if (winner.Estimate == loser.Estimate)
            {
                return new Payout(0, loser.User, winner.User, PayoutMethod.FullContract);
            }

            //the correct location on the probability line.
            var correctLocation = resolution ? 1 : 0;

            //the average, a location you would both have accepted.
            var middle = (winner.Estimate + loser.Estimate) / 200;

            //examples
            //correct:1
            //winner:1
            //loser:0    //very different opinions, yet small rewards.
            // => winner makes 0.5

            //correct:1
            //winner:0.1
            //loser:0.0   //winner being WRONGER makes you make more money
            // => winner makes 0.95

            //correct:1  
            //winner:1
            //loser:0.9  //both roughly correct - this is okay
            // => winner makes 0.05

            var award = Math.Abs(correctLocation - middle);
            return new Payout(award, loser.User, winner.User, PayoutMethod.FullContract);
        }

        /// <summary>
        /// for bets resolving true, we want:
        /// 0.9999 vs 0.9: A wins a lot (since 1000x multiple of gap)
        /// 0.9 vs 0.8: A wins medium (since 2x gap)
        /// 0.1 vs 0.01: A wins a little (since they were both significantly wrong)
        /// 0.5 vs 0.4: A wins a little  (since they were about equal in wrongness)
        /// and vice versa for false.
        /// </summary>
        public static Payout EvaluateMultiplicative(bool resolution, UserBet winner, UserBet loser)
        {
            var correctLocation = resolution ? 1 : 0;
            var winnerGap = Math.Abs(winner.Estimate / 100.0 - correctLocation);
            var loserGap = Math.Abs(loser.Estimate / 100.0 - correctLocation);
            double winnerRightnessRatio;
            if (winnerGap == 0)
            {
                winnerRightnessRatio = 1;
            }
            else
            {
                winnerRightnessRatio = (loserGap-winnerGap)/loserGap;
            }
            return new Payout(winnerRightnessRatio, loser.User, winner.User, PayoutMethod.Multiplicative);
        }

        public static Payout EvaluateStraight(bool resolution, UserBet winner,  UserBet loser)
        {
            var p = new Payout(1, loser.User, winner.User, PayoutMethod.StraightBet);
            return p;
        }

        /// <summary>
        /// wrapper for various payout methods.
        /// provides them with the pred (for naming, resolution, etc. and the winner & loser);
        /// </summary>
        public static IEnumerable<Payout> EvaluateAccordingToFunction(Predicate pred, Func<bool, UserBet, UserBet, Payout> func, PayoutMethod method)
        {
            var res = new List<Payout>();
            if (!pred.Valid)
            {
                Console.WriteLine($"Invalid pred: {pred}");
                return res;
            }
            //Console.WriteLine("\t"+method);
            for (var ii = 0; ii < pred.UserBets.Count(); ii++)
            {
                for (var jj = ii + 1; jj < pred.UserBets.Count(); jj++)
                {
                    var ub1 = pred.UserBets.Skip(ii).First();
                    var ub2 = pred.UserBets.Skip(jj).First();
                    //Console.Write($"\t\t{ub1} vs {ub2}");

                    UserBet winner;
                    UserBet loser;
                    if (ub1.Estimate == ub2.Estimate)
                    {
                        Console.WriteLine();
                        continue;
                    }
                    if (pred.ResolvedTrue)
                    {
                        winner = ub1.Estimate > ub2.Estimate ? ub1 : ub2;
                    }
                    else
                    {
                        winner = ub1.Estimate < ub2.Estimate ? ub1 : ub2;
                    }

                    if (winner.User.Name == ub1.User.Name)
                    {
                        loser = ub2;
                    }
                    else
                    {
                        loser = ub1;
                    }
                    
                    var p = func(pred.ResolvedTrue, winner, loser);
                    //Console.WriteLine($"\tpayout: {p}");
                    res.Add(p);
                }
            }
            return res;
        }

        public static IEnumerable<Predicate> LoadPredicates()
        {
            var lines = System.IO.File.ReadAllLines(path).Where(el => !string.IsNullOrEmpty(el)).ToList();
            var header = lines.First();
            
            var preds = lines.Skip(1).Where(el=>!string.IsNullOrEmpty(el)).Select(el => new Predicate(el, users)).ToList();

            preds=preds.Where(el=>el.Valid==true).ToList();
            var lastDomain = "";
            foreach (var p in preds)
            {
                if (!string.IsNullOrEmpty(p.Domain))
                {
                    lastDomain = p.Domain;
                }
                else
                {
                    p.Domain = lastDomain;
                }
            }
            return preds;
        }
    }
}
