# Tomatwo.DependencyInjection

[![Nuget](https://img.shields.io/nuget/v/Tomatwo.DependencyInjection.svg)](https://www.nuget.org/packages/Tomatwo.DependencyInjection/)

There are a lot of dependency injection frameworks that can be used with ASP.NET Core, but the built in injector is fast and adequate for most purposes.  A notable feature that is not provided, though, is injection into properties or fields.  Property injection is controversial and probably it was left out for that reason.

Tomatwo-DI provides injection into properties and fields (you choose which) without replacing the normal built in injector.  It scans the registered services looking for classes which use property or field injection, and creating a wrapper which implements the necessary logic.  This is done in the `Startup` class's `ConfigureServices` method:

```
using Tomatwo.DependencyInjection;

...

public void ConfigureServices(IServiceCollection services) {
    services.AddScoped<MyComponentClass>();

    services.AddControllers()
        .AddControllersAsServices();

    services.AddEnhancedServiceProvider();
}
```

The additional configuration option `AddControllersAsServices` means that all the controllers identified by MVC will be added to the DI container, which allows them to be found and wrapped by Tomatwo-DI.  You can of course add your own services too, as illustrated for `MyComponentClass`.

Finally a call is made to `AddEnhancedServiceProvider`.  This carries out the scan and so should not be called until all the classes using property or field injection have been added to the container.  Probably this means that the call should go at the end of `ConfigureServices`.  Do not call `AddEnhancedServiceProvider` more than once.

Properties and fields which are injection targets must be identified with an attribute:

```
using Tomatwo.DependencyInjection;

...

public class MyController: ControllerBase {
    [Inject] protected readonly MyType myType;
    [Inject] protected MyOtherType myOtherType { private get; set; }

...
```

The access to the properties and fields can be made quite restrictive, but the restrictions are slightly different in each case.  The field has to be at least `protected readonly`, which means derived classes can read it but not write it.  The property has to be settable by derived classes, but need not be readable.  Ideally only the class itself would have access to the value (as can be arranged with Java CDI for example) but this is difficult in .NET.

Most .NET DI frameworks require a lot more access to the properties and fields than this; usually at least the setter must be public.  This is because Tomatwo-DI uses a different trade-off.  Most DI frameworks call the class constructor and then do property and field assignments.  Tomatwo-DI creates a derived class, and the derived class constructor does the assignments instead.

This technique provides two benefits.  Firstly, as already noted, the property and field access can be made much more restrictive.  Secondly, the injected values are available in the constructor, because the derived class constructor gets called first.  There is one disadvantage, which is that the object's identity may not be what you expect.

Tomatwo-DI still needs development and one issue at the moment is that it doesn't provide good support for unit testing with restricted accessibility.  Your test framework will be unable to assign to the injection targets because of the `protected` accessibility level, and Tomatwo-DI won't provide an alternative.  You can, though, use reflection to set values you otherwise don't have access to.
