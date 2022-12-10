using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DependencyInjectionContainerCore.Model;

namespace DependencyInjectionContainerCore.Service
{
    public class DependenciesProvider
    {
        private readonly DependenciesConfiguration _configuration;

        private readonly ConcurrentDictionary<Type, object> _singletonImplementations =
            new ConcurrentDictionary<Type, object>();

        private readonly Stack<Type> _recursionStackResolver = new Stack<Type>();

        public DependenciesProvider(DependenciesConfiguration configuration)
        {
            _configuration = configuration;
        }

        public TDependency Resolve<TDependency>()
        {
            return (TDependency) Resolve(typeof(TDependency));
        }

        private object Resolve(Type t)
        {
            Type dependencyType = t;
            List<ImplementationInfo> infos = GetImplementationsInfos(dependencyType);

            if (infos == null && t.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                throw new Exception("Unregistered dependency");
            if (_recursionStackResolver.Contains(t))
                return null;

            _recursionStackResolver.Push(t);
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                dependencyType = t.GetGenericArguments()[0];
                infos = GetImplementationsInfos(dependencyType);
                if (infos == null) throw new Exception("Unregistered dependency");
                List<object> implementations = new List<object>();
                foreach (ImplementationInfo info in infos)
                {
                    implementations.Add(GetImplementation(info, t));
                }

                return ConvertToIEnumerable(implementations, dependencyType);
            }

            object obj = GetImplementation(infos[0], t);
            _recursionStackResolver.Pop();
            return obj;
        }

        private List<ImplementationInfo> GetImplementationsInfos(Type dependencyType)
        {
            if (_configuration.RegisteredDependencies.ContainsKey(dependencyType))
                return _configuration.RegisteredDependencies[dependencyType];
            if (!dependencyType.IsGenericType) return null;
            Type definition = dependencyType.GetGenericTypeDefinition();
            return _configuration.RegisteredDependencies.ContainsKey(definition)
                ? _configuration.RegisteredDependencies[definition]
                : null;
        }

        private object GetImplementation(ImplementationInfo implInfo, Type resolvingDependency)
        {
            Type innerTypeForOpenGeneric = null;
            if (implInfo.ImplClassType.IsGenericType && implInfo.ImplClassType.IsGenericTypeDefinition &&
                implInfo.ImplClassType.GetGenericArguments()[0].FullName == null)
                innerTypeForOpenGeneric = resolvingDependency.GetGenericArguments().FirstOrDefault();

            if (implInfo.LifeTime == LifeTime.Singleton)
            {
                if (!_singletonImplementations.ContainsKey(implInfo.ImplClassType))
                {
                    object singleton = CreateInstanceForDependency(implInfo.ImplClassType, innerTypeForOpenGeneric);
                    _singletonImplementations.TryAdd(implInfo.ImplClassType, singleton);
                }

                return _singletonImplementations[implInfo.ImplClassType];
            }

            return CreateInstanceForDependency(implInfo.ImplClassType, innerTypeForOpenGeneric);
        }

        private object CreateInstanceForDependency(Type implClassType, Type innerTypeForOpenGeneric)
        {
            ConstructorInfo[] constructors = implClassType.GetConstructors()
                .OrderByDescending(x => x.GetParameters().Length).ToArray();
            object implInstance = null;
            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                List<object> paramsValues = new List<object>();
                foreach (ParameterInfo parameter in parameters)
                {
                    if (IsDependency(parameter.ParameterType))
                    {
                        object obj = Resolve(parameter.ParameterType);
                        paramsValues.Add(obj);
                    }
                    else
                    {
                        object obj = null;
                        try
                        {
                            obj = Activator.CreateInstance(parameter.ParameterType, null);
                        }
                        catch
                        {
                            // ignored
                        }

                        paramsValues.Add(obj);
                    }
                }

                try
                {
                    if (innerTypeForOpenGeneric != null)
                        implClassType = implClassType.MakeGenericType(innerTypeForOpenGeneric);
                    implInstance = Activator.CreateInstance(implClassType, paramsValues.ToArray());
                    break;
                }
                catch
                {
                    // ignored
                }
            }

            return implInstance;
        }

        private object ConvertToIEnumerable(List<object> implementations, Type t)
        {
            var enumerableType = typeof(Enumerable);
            var castMethod = enumerableType.GetMethod(nameof(Enumerable.Cast))?.MakeGenericMethod(t);
            var toListMethod = enumerableType.GetMethod(nameof(Enumerable.ToList))?.MakeGenericMethod(t);

            IEnumerable<object> itemsToCast = implementations;

            var castedItems = castMethod?.Invoke(null, new[] {itemsToCast});

            return toListMethod?.Invoke(null, new[] {castedItems});
        }

        private bool IsDependency(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return IsDependency(t.GetGenericArguments()[0]);

            return _configuration.RegisteredDependencies.ContainsKey(t);
        }
    }
}