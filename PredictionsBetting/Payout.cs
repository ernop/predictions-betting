namespace PredictionsBetting
{
    public class Payout
    {
        public double Amount { get; set; }
        public User From { get; set; }
        public User To { get; set; }
        public PayoutMethod PayoutMethod { get; set; }

        public Payout(double amount, User from, User to, PayoutMethod method)
        {
            Amount = amount;
            From = from;
            To = to;
            PayoutMethod = method;
        }

        public override string ToString()
        {
            return $"{From} pays {Amount} to {To}";
        }
    }
}
