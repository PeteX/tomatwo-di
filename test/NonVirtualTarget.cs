namespace DependencyInjectionTest
{
    public class NonVirtualTarget
    {
        [SimpleIntercept] public int Simple() => 1;
    }
}
