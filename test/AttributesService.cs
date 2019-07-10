using Microsoft.AspNetCore.Mvc;

namespace DependencyInjectionTest
{
    // Test attributes are borrowed from MVC.  They are just attributes, for testing purposes; they don't have their
    // usual MVC interpretations.

    [ApiController]
    [Route("class-attr", Order=345)]
    public class AttributesService
    {
        [SimpleIntercept] [HttpGet] public virtual int MethodWithoutParameter() => 1;
        [SimpleIntercept] [HttpGet("httpget")] public virtual int MethodWithParameter() => 1;
        [SimpleIntercept] [HttpGet("param", Order=123)] public virtual int MethodWithNamedParameter() => 1;
        [SimpleIntercept] public virtual int ArgWithoutParameter([FromHeader] string arg) => 1;
        [SimpleIntercept] public virtual int ArgWithNamedParameter([FromHeader(Name="header")] string arg) => 1;
    }
}
