namespace Match3Tray.Model
{
    public static class Enums
    {
        public enum FruitType
        {
            None = 0,
            Apple = 1,
            Banana = 2,
            Blueberry = 3,
            Pineapple = 4,
            Lemon = 5,
            Pear = 6,
            Watermelon = 7,
            Strawberry = 8
        }
        
        public enum GameState
        {
            Idle = 0,
            Playing = 1,
            NextLevelPrompt = 2,
            FailedPrompt = 3,
            GameFinishedPrompt = 4,
        }
    }
}