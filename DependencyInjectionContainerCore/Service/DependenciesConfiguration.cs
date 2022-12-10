using System;
using System.Collections.Generic;
using DependencyInjectionContainerCore.Model;

namespace DependencyInjectionContainerCore.Service
{
    public class DependenciesConfiguration
    {
        public readonly Dictionary<Type, List<ImplementationInfo>> RegisteredDependencies;

        public DependenciesConfiguration()
        {
            RegisteredDependencies = new Dictionary<Type, List<ImplementationInfo>>();
        }

        public void Register<TDependency, TImplementation>(LifeTime lt = LifeTime.InstancePerDependency)
        {
            Register(typeof(TDependency), typeof(TImplementation), lt);
        }

        public void Register(Type interfaceType, Type classType, LifeTime lt = LifeTime.InstancePerDependency)
        {
            if ((!interfaceType.IsInterface && interfaceType != classType) || classType.IsAbstract || !interfaceType.IsAssignableFrom(classType) && !interfaceType.IsGenericTypeDefinition)
                throw new Exception("Registration exception");
            if (!RegisteredDependencies.ContainsKey(interfaceType))
            {
                List<ImplementationInfo> impl = new List<ImplementationInfo> {new ImplementationInfo(lt, classType)};
                RegisteredDependencies.Add(interfaceType, impl);
            }
            else
            {
                RegisteredDependencies[interfaceType].Add(new ImplementationInfo(lt, classType));
            }
        }
    }
}