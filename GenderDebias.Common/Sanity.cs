namespace GenderDebias.Common
{
    public static class Sanity
    {
        public static bool Requires(bool valid, string message)
        {
            if (!valid)
                throw new Exception(message);
            return true;
        }

        public static T RequiresNotNoll<T>(T? t, string message = "")
        {
            if (t is null)
                throw new Exception("Null reference: " + message);
            return t;
        }
    }
}
