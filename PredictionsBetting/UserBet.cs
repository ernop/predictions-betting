namespace PredictionsBetting
{
    public class UserBet
    {
        public User User { get; set; }
        public double Estimate { get; set; }
        public override string ToString()
        {
            return $"{User}:{Estimate}";
        }
    }
}
