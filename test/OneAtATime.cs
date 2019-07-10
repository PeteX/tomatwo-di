namespace DependencyInjectionTest
{
    public class OneAtATime
    {
        [Intercept] [SimpleIntercept] public virtual int Simple() => 1;
    }
}
