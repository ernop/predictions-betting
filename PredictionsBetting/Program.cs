﻿using System;

using System.Collections.Generic;
using System.Linq;

namespace PredictionsBetting
{

    public class Program
    {
        public static string path = @"d:\\proj\\predictions-betting\\data.csv";
        public static List<string> usernames = new List<string>() { "ivan", "jason", "daffy", "ernie" };
        public static IEnumerable<User> users = usernames.Select(el => new User(el));

        static void Main(string[] args)
        {
            var rawPredicates = LoadPredicates();
            Evaluate(rawPredicates);

            //var allDomains = rawPredicates.Select(el => el.Domain).ToHashSet();
            
            foreach (var user in users)
            {
                var bs = CalculateBrier(rawPredicates, user);
                Console.WriteLine($"BrierScore: {user}: {bs}");
            }

            //foreach (var domain in allDomains)
            //{
            //    foreach (var user in users)
            //    {
            //        var usePreds = rawPredicates.Where(el => el.Domain == domain);
            //        var bs = CalculateBrier(usePreds, user);
            //        Console.WriteLine($"{domain} ({usePreds.Count()}) {user}: {bs}");
            //    }
            //}

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
            var methods = new Dictionary<PayoutMethod, Func<UserBet, UserBet, Payout>>() { };
            methods[PayoutMethod.StraightBet] = EvaluateStraight;
            methods[PayoutMethod.DiffBet] = EvaluateDiffBets;
            methods[PayoutMethod.FullContract] = EvaluateFullContract;
            var results = new Dictionary<PayoutMethod, Dictionary<string, double>>();
            foreach (var pred in preds)
            {
                Console.WriteLine(pred.Text);
                Console.WriteLine($"\tResolution: {pred.ResolvedTrue}");
                var userBets = string.Join(" ", pred.UserBets.Select(el => $"{el.User.Name}:{el.Estimate}"));
                Console.WriteLine("\t" + userBets);
                foreach (var methodEnum in methods.Keys)
                {
                    var method = methods[methodEnum];
                    if (!results.ContainsKey(methodEnum)) { results[methodEnum] = new Dictionary<string, double>(); }
                    var payouts = EvaluateAccordingToFunction(pred, method, methodEnum);
                    var payoutResults = SumPayouts(payouts);
                    var moneyDescription = users.Select(el => $"{el}: {payoutResults[el.Name]}").ToList();
                    Console.WriteLine("\t\t"+string.Join(" ", moneyDescription));
                    foreach (var pr in payoutResults)
                    {
                        if (!results[methodEnum].ContainsKey(pr.Key)) { results[methodEnum][pr.Key] = 0; }
                        results[methodEnum][pr.Key] += pr.Value;
                    }
                }

                Console.WriteLine("\n\tRunning totals:");
                foreach (var methodName in results.Keys)
                {
                    Console.WriteLine("\t\t"+methodName);
                    foreach (var user in users)
                    {
                        Console.WriteLine($"\t\t\t{methodName} {user.Name} {results[methodName][user.Name]}");
                    }
                }
            }
        }

        public static Payout EvaluateDiffBets(UserBet winner, UserBet loser)
        {
            var middle = (winner.Estimate + loser.Estimate)/2;
            return new Payout(middle/100.0, loser.User, winner.User, PayoutMethod.DiffBet);
        }

        public static Payout EvaluateFullContract(UserBet winner, UserBet loser)
        {
            var gap = Math.Abs(winner.Estimate - loser.Estimate);
            return new Payout(gap/100.0, loser.User, winner.User, PayoutMethod.FullContract);
        }

        public static Payout EvaluateStraight(UserBet winner,  UserBet loser)
        {
            var p = new Payout(1, loser.User, winner.User, PayoutMethod.StraightBet);
            return p;
        }

        /// <summary>
        /// wrapper for various payout methods.
        /// provides them with the pred (for naming, resolution, etc. and the winner & loser);
        /// </summary>
        public static IEnumerable<Payout> EvaluateAccordingToFunction(Predicate pred, Func<UserBet, UserBet, Payout> func, PayoutMethod method)
        {
            var res = new List<Payout>();
            if (!pred.Valid)
            {
                Console.WriteLine($"Invalid pred: {pred}");
                return res;
            }
            Console.WriteLine("\t"+method);
            for (var ii = 0; ii < 4; ii++)
            {
                for (var jj = ii + 1; jj < 4; jj++)
                {
                    var ub1 = pred.UserBets.Skip(ii).First();
                    var ub2 = pred.UserBets.Skip(jj).First();
                    UserBet winner;
                    UserBet loser;
                    if (ub1.Estimate == ub2.Estimate)
                    {
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
                    Console.Write($"\t\t{ub1} vs {ub2}");
                    var p = func(winner, loser);
                    Console.WriteLine($"\tpayout: {p}");
                    res.Add(p);
                }
            }
            return res;
        }

        public static IEnumerable<Predicate> LoadPredicates( )
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
