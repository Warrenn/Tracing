namespace Target
{
    public class BarClass
    {
        public T DoSomething<T>(T v, int a) where T : class
        {
            return v;
        }
    }
}
