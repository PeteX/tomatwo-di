using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;

namespace Tomatwo.DependencyInjection
{
    public static class EnhancedServiceProvider
    {
        private static void ldarg(ILGenerator ilGenerator, int argNum)
        {
            switch (argNum)
            {
                case 0: ilGenerator.Emit(OpCodes.Ldarg_0); break;
                case 1: ilGenerator.Emit(OpCodes.Ldarg_1); break;
                case 2: ilGenerator.Emit(OpCodes.Ldarg_2); break;
                case 3: ilGenerator.Emit(OpCodes.Ldarg_3); break;
                default:
                    if (argNum <= 255)
                    {
                        ilGenerator.Emit(OpCodes.Ldarg_S, argNum);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldarg, argNum);
                    }
                    break;
            }
        }

        private static Type wrapClass(ModuleBuilder moduleBuilder, Type cls)
        {
            var fields = cls.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fieldInjects = fields
                .Where(x => x.GetCustomAttribute(typeof(InjectAttribute)) != null)
                .ToList();

            var properties = cls.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var propertyInjects = properties
                .Where(x => x.GetCustomAttribute(typeof(InjectAttribute)) != null)
                .ToList();

            Type[] injectionTypes = fieldInjects.Select(x => x.FieldType)
                .Concat(propertyInjects.Select(x => x.PropertyType))
                .ToArray();

            if (!injectionTypes.Any())
                return null;

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"{cls.Name}_PropertyInjection", TypeAttributes.Public, cls);

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard, injectionTypes);

            ILGenerator ilGenerator = ctor.GetILGenerator();

            int argNum;
            for (argNum = 0; argNum < fieldInjects.Count; argNum++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);                          // object instance
                ldarg(ilGenerator, argNum + 1);                             // argument with the appropriate type
                ilGenerator.Emit(OpCodes.Stfld, fieldInjects[argNum]);      // store it in the field
            }

            for (int propNum = 0; propNum < propertyInjects.Count; argNum++, propNum++)
            {
                ilGenerator.Emit(OpCodes.Ldarg_0);                          // object instance
                ldarg(ilGenerator, argNum + 1);                             // argument with the appropriate type
                ilGenerator.Emit(OpCodes.Call, propertyInjects[propNum].SetMethod);     // store it in the property
            }

            // Call base class constructor:
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, cls.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                Type.EmptyTypes,
                null));

            ilGenerator.Emit(OpCodes.Ret);

            return typeBuilder.CreateTypeInfo();
        }

        /// <summary>
        /// Scans the registered services, and creates subclasses where necessary to implement property or field
        /// injection.
        /// </summary>
        public static IServiceCollection AddEnhancedServiceProvider(this IServiceCollection services)
        {
            AssemblyName aName = new AssemblyName("_PropertyInjection");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(aName.Name);

            for (int i = 0; i < services.Count; i++)
            {
                ServiceDescriptor service = services[i];
                if (service.ImplementationType != null)
                {
                    Type impl = wrapClass(moduleBuilder, service.ImplementationType);
                    if (impl != null)
                        services[i] = new ServiceDescriptor(service.ServiceType, impl, service.Lifetime);
                }
            }

            return services;
        }
    }
}
