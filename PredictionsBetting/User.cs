namespace PredictionsBetting
{
    public class User
    {
        public User(string name)
        {
            Name = name;
        }
        public string Name { get; set; }
        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
