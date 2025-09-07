using Match3Tray.Model;

namespace Match3Tray.Core
{
    public static class TaskCodec
    {
        public const int SHIFT = 12;
        public const int COUNT_MASK = (1 << SHIFT) - 1; // 4095

        public static int Encode(Enums.FruitType type, int count)
        {
            return ((int)type << SHIFT) | (count & COUNT_MASK);
        }

        public static Enums.FruitType DecodeType(int code)
        {
            return (Enums.FruitType)(code >> SHIFT);
        }

        public static int DecodeCount(int code)
        {
            return code & COUNT_MASK;
        }
    }
}