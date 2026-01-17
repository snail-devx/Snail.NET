using Snail.Aspect.Distribution.Attributes;

namespace Snail.Test.Aspect.Components
{
    [LockAspect(Workspace = "Test", Code = "Default", Analyzer = "dddddd")]
    class LockAspectTest
    {
        [LockMethod(Key = "dddddd", Value = "12312")]
        public virtual async Task LockTest()
        {
            await Task.Yield();
        }
        [LockMethod(Key = "dddddd", Value = "12312"), Expire(Seconds = 1000)]
        public virtual async Task<string> LockTestString(string lockKey = "1")
        {
            await Task.Yield();
            return string.Empty;
        }
    }
}
