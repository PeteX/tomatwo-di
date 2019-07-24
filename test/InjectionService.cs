using Tomatwo.DependencyInjection;

namespace DependencyInjectionTest
{
    public class InjectionService : ITestInterface
    {
        [Inject] protected InjectThisService propertyService { private get; set; }
        [Inject] protected readonly InjectThisService fieldService;

        public string GetPropertyMessage() => propertyService.Message;
        public string GetFieldMessage() => fieldService.Message;
    }
}
