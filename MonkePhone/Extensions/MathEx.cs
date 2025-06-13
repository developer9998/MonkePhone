namespace MonkePhone.Extensions
{
    public static class MathEx
    {
        public static int Wrap(int integer, int min, int max)
        {
            int range = max - min;
            int result = (integer - min) % range;
            if (result < 0) result += range;

            return result + min;
        }

        public static bool Is01(this float integer)
        {
            return integer > 0 && integer < 1;
        }
    }
}
