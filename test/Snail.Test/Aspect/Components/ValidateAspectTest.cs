using Snail.Abstractions.Common.Interfaces;
using Snail.Utilities.Common.Interfaces;

namespace Snail.Test.Aspect.Components;

[ValidateAspect]
public class ValidateAspectTest
{
    public virtual void TestRequired([Required] int? iv, [Required] Nullable<int> inv, [Required("dddddddddd")] string name)
    {
    }
    public virtual void TestAny([Required] int[] v)
    {
    }

    public virtual void TestValidatable(Validatable tv, [Required] Validatable tv1)
    {
    }
    public virtual void TestComplex([Required] int? iv, [Required] string str, [Required] string[] array, [Required] Validatable tv)
    {

    }
}


public class Validatable : IValidatable
{
    bool IValidatable.Validate(out string message)
    {
        message = "测试验证逻辑";
        return false;
    }
}