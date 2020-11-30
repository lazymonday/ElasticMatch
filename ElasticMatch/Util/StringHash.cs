namespace ElasticMatch.Util
{
    public class StringHash
    {
        public static int GetHashCode(string s)
        {
            int num1 = 0;
            for (int index = 0; index < s.Length; ++index)
            {
                int num2 = num1 + (int) s[index];
                int num3 = num2 + (num2 << 10);
                num1 = num3 ^ num3 >> 6;
            }

            int num4 = num1 + (num1 << 3);
            int num5 = num4 ^ num4 >> 11;
            return num5 + (num5 << 15);
        }
    }
}