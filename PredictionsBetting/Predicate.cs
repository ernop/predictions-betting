using System;

using System.Collections.Generic;
using System.Linq;

namespace PredictionsBetting
{
    public class Predicate
    {
        /// <summary>
        /// hardcode this... TODO
        /// </summary>
        public static DateTime targetDate = new DateTime(2025, 1, 1);
        public string Text { get; set; }
        public List<UserBet> UserBets { get; set; } = new List<UserBet>();
        public bool ResolvedTrue { get; set; }
        public DateTime DueDate { get; set; }
        public bool Valid { get; set; }
        public string ValidityReason { get; set; }
        public string Domain { get; set; }

        public Predicate(string text, List<UserBet> userBets, bool resolvedTrue, DateTime dueDate, bool valid, string validityReason, string domain)
        {
            Text = text;
            UserBets = userBets;
            ResolvedTrue = resolvedTrue;
            DueDate = dueDate;
            Valid = valid;
            ValidityReason = validityReason;
            Domain = domain;
        }

        /// <summary>
        /// parsing text
        /// </summary>
        public Predicate(string line, IEnumerable<User> users)
        {
            Valid = true;
            if (string.IsNullOrWhiteSpace(line))
            {
                Valid = false;
                ValidityReason = "empty";
                return;
            }
            var lsp = line.Split('\t').ToList();
            if (lsp.Count == 0)
            {
                Valid = false;
                ValidityReason = "none";
                return;
            }
            Domain = lsp[0].Trim();
            Text = lsp[1].TrimEnd();
            DueDate = DateTime.Parse(lsp[4]);
            if (DueDate != targetDate)
            {
                Valid = false;
                ValidityReason = "not due yet";
                return;
            }
            for (var ii = 5; ii < 9; ii++)
            {
                var ub = new UserBet();
                ub.User = users.Skip(ii-5).First();
                if (Double.TryParse(lsp[ii], out var result))
                {
                    ub.Estimate = result;
                }
                else
                {
                    Console.WriteLine($"Invalid. {line}");
                    Valid = false;
                    ValidityReason = "no meaningful numerical bet";
                    return;
                }
                UserBets.Add(ub);
            }
            if (TryGetResolution(lsp[9], out var finalResult)){
                ResolvedTrue = finalResult;
            }
            else
            {
                Valid = false;
                ValidityReason = "no result yet";
                return;
            }
        }

        private bool TryGetResolution(string val, out bool result)
        {
            val = val.ToLower();
            if (val == "1")
            {
                val = "t";
            }
            if (val == "0")
            {
                val = "f";
            }
            if (val != "t" && val != "f")
            {
                result = false;
                return false;
            }
            result = val == "t";
            return true;
        }

        public override string ToString()
        {
            //var ub = string.Join(",", UserBets.Select(el => el.ToString()));
            var ub = "";
            return $"{Text}{ub}, Resolution:{ResolvedTrue}";
        }       
    }
}
