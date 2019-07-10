namespace DependencyInjectionTest
{
    public class InterceptionService
    {
        public string ExclamationMark = "!";

        [SimpleIntercept] public virtual int Simple() => 1;
        [Intercept] public virtual string MakeMessage(string word, string who, int i) =>
            $"Hello {word} {who} {i}{ExclamationMark}";
    }
}
