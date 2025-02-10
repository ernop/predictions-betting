namespace PredictionsBetting
{
    public class UserBet
    {
        public User User { get; set; }
        public double Estimate { get; set; }
        public UserBet() { }

        public UserBet(User user, double estimate)
        {
            User = user;
            Estimate = estimate;
        }
        public override string ToString()
        {
            return $"{User}:{Estimate}";
        }
    }
}
