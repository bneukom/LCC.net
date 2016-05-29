namespace Selen.Wpf.DemoApplication
{
    public class FoodItem
    {
        public FoodItem(string name, int amount, int calories, bool reservedForCaptain)
        {
            Name = name;
            Amount = amount;
            Calories = calories;
            ReservedForCaptain = reservedForCaptain;
        }

        public string Name { get; set; }
        public int Amount { get; set; }
        public int Calories { get; set; }
        public bool ReservedForCaptain { get; set; }
    }
}