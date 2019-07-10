using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;

namespace Tomatwo.DependencyInjection
{
    public class EnhancedServiceProvider
    {
        public delegate object Interceptor(Interception interception);

        private Dictionary<Type, Interceptor> interceptors = new Dictionary<Type, Interceptor>();
        private Action initialisers = () => { };
        private int generatedField = 0;

        private void ldarg(ILGenerator ilGenerator, int argNum)
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

        private void wrapConstructor(Type cls, TypeBuilder typeBuilder, Type[] injectionTypes,
            List<FieldInfo> fieldInjects, List<PropertyInfo> propertyInjects)
        {
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
        }

        private FieldInfo makeInvoker(string name, TypeBuilder typeBuilder, MethodInfo method)
        {
            var dynamicMethod = new DynamicMethod(name, typeof(object), new[] { typeof(object), typeof(object[]) });
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Target object for the call
            ilGenerator.Emit(OpCodes.Ldarg_0);

            // Arguments
            var param = method.GetParameters();
            for (int i = 0; i < param.Length; i++)
            {
                // Get the parameter from the array
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);

                // Convert to appropriate value
                OpCode convert = typeof(ValueType).IsAssignableFrom(param[i].ParameterType) ?
                    OpCodes.Unbox_Any : OpCodes.Castclass;

                ilGenerator.Emit(convert, param[i].ParameterType);
            }

            // Call the base method, Call NOT Callvirt because we want the base class method
            ilGenerator.Emit(OpCodes.Call, method);
            ilGenerator.Emit(OpCodes.Ret);

            var fieldInfo = typeBuilder.DefineField(name, typeof(Func<object, object[], object>),
                FieldAttributes.Private | FieldAttributes.Static);

            initialisers += () =>
            {
                typeBuilder.GetField(name, BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, dynamicMethod.CreateDelegate(typeof(Func<object, object[], object>)));
            };

            return fieldInfo;
        }

        private void wrapMethod(Type cls, TypeBuilder typeBuilder, MethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.Virtual) == 0)
                throw new InvalidOperationException($"Interception target {method.Name} is not virtual.");

            var interceptorList = method.GetCustomAttributes()
                .Where(x => interceptors.ContainsKey(x.GetType()))
                .Select(x => interceptors[x.GetType()])
                .ToArray();

            if (interceptorList.Length > 1)
                throw new InvalidOperationException($"Method {method.Name} is intercepted more than once.");

            var interceptor = interceptorList.Single();

            string interceptorFieldName = $"_DI_Interceptor_{generatedField}";
            FieldBuilder interceptorField = typeBuilder.DefineField(interceptorFieldName, typeof(Interceptor),
                FieldAttributes.Private | FieldAttributes.Static);
            string interceptionFieldName = $"_DI_Interception_{generatedField}";
            FieldBuilder interceptionField = typeBuilder.DefineField(interceptionFieldName, typeof(Interception),
                FieldAttributes.Private | FieldAttributes.Static);

            var invoke = makeInvoker($"_DI_Invoke_{generatedField}", typeBuilder, method);

            generatedField++;

            initialisers += () =>
            {
                typeBuilder.GetField(interceptorFieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, interceptor);
                typeBuilder.GetField(interceptionFieldName, BindingFlags.NonPublic | BindingFlags.Static)
                    .SetValue(null, new Interception { Method = method });
            };

            var param = method.GetParameters();

            var methodBuilder = typeBuilder.DefineMethod(method.Name, method.Attributes, method.ReturnType,
                method.GetParameters().Select(x => x.ParameterType).ToArray());

            for (int i = 1; i < param.Length; i++)
                methodBuilder.DefineParameter(i, param[i].Attributes, param[i].Name);

            var ilGenerator = methodBuilder.GetILGenerator();

            // The interceptor takes this Interception struct as a parameter, but it needs some fields filling in.
            ilGenerator.Emit(OpCodes.Ldsfld, interceptionField);
            ilGenerator.DeclareLocal(typeof(Interception));
            ilGenerator.Emit(OpCodes.Stloc_0);

            // Fill in the target object
            ilGenerator.Emit(OpCodes.Ldloca_S, 0);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Stfld, typeof(Interception).GetField("Target"));

            // Create the argument array
            ilGenerator.Emit(OpCodes.Ldloca_S, 0);
            ilGenerator.Emit(OpCodes.Ldc_I4, param.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            for (int i = 0; i < param.Length; i++)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                ilGenerator.Emit(OpCodes.Ldarg, i + 1);
                ilGenerator.Emit(OpCodes.Box, param[i].ParameterType);
                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            ilGenerator.Emit(OpCodes.Stfld, typeof(Interception).GetField("Args"));
            // Finished creating the argument array

            // Fill in the Invoke delegate
            ilGenerator.Emit(OpCodes.Ldloca_S, 0);
            ilGenerator.Emit(OpCodes.Ldsfld, invoke);
            ilGenerator.Emit(OpCodes.Stfld, typeof(Interception).GetField("Invoke"));

            // Call the interceptor
            ilGenerator.Emit(OpCodes.Ldsfld, interceptorField);
            ilGenerator.Emit(OpCodes.Ldloc_0);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Interceptor).GetMethod("Invoke"));
            ilGenerator.Emit(OpCodes.Unbox_Any, method.ReturnType);
            ilGenerator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(methodBuilder, method);
        }

        private Type wrapClass(ModuleBuilder moduleBuilder, Type cls)
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

            var methods = cls.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var interceptions = methods
                .Where(method => method.GetCustomAttributes()
                    .Any(attrib => interceptors.ContainsKey(attrib.GetType())))
                .ToList();

            if (!injectionTypes.Any() && !interceptions.Any())
                return null;

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                $"DI_{cls.Name}", TypeAttributes.Public, cls);

            if (injectionTypes.Any())
                wrapConstructor(cls, typeBuilder, injectionTypes, fieldInjects, propertyInjects);

            foreach (var method in interceptions)
                wrapMethod(cls, typeBuilder, method);

            return typeBuilder.CreateTypeInfo();
        }

        public EnhancedServiceProvider AddInterceptor<T>(Interceptor interceptor)
        {
            interceptors[typeof(T)] = interceptor;
            return this;
        }

        public EnhancedServiceProvider(IServiceCollection services, Action<EnhancedServiceProvider> config)
        {
            if (config != null)
                config(this);

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

            initialisers();
        }
    }
}
