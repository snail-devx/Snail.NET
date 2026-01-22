namespace Snail.Test.Aspect.Components
{

    [MethodAspect(Interceptor = "111")]
    public abstract class AspectTest
    {
        public virtual async Task<string> String()
        {
            await Task.Yield();
            return string.Empty;
        }

        public virtual string XXX()
        {
            return string.Empty;
        }

        public void Test()
        {

        }

        public static void Test2()
        {

        }
    }
}