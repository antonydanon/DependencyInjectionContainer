using System.Collections.Generic;
using DependencyInjectionContainerCore.Model;
using DependencyInjectionContainerCore.Service;
using NUnit.Framework;

namespace DependencyInjectionContainerTests
{
    [TestFixture]
    public class ContainerTests
    {
        [Test]
        public void SimpleDependencyTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<ISmth, ISmthImpl>(LifeTime.Singleton);
            DependenciesProvider provider = new DependenciesProvider(configuration);

            ISmthImpl cl = (ISmthImpl) provider.Resolve<ISmth>();
            Assert.IsNotNull(cl);
        }

        [Test]
        public void DifferentLifeTimeTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<ISmth, ISmthImpl>(LifeTime.Singleton);
            configuration.Register<IService, FirstIServiceImpl>();
            DependenciesProvider provider = new DependenciesProvider(configuration);

            ISmthImpl cl1 = (ISmthImpl) provider.Resolve<ISmth>();
            ISmthImpl cl2 = (ISmthImpl) provider.Resolve<ISmth>();
            Assert.AreEqual(cl1, cl2);
            IService s1 = provider.Resolve<IService>();
            IService s2 = provider.Resolve<IService>();
            Assert.AreNotEqual(s1, s2);
        }

        [Test]
        public void ManyImplementationsTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<IService, FirstIServiceImpl>();
            configuration.Register<IService, SecondIServiceImpl>();
            DependenciesProvider provider = new DependenciesProvider(configuration);

            IEnumerable<IService> impls = provider.Resolve<IEnumerable<IService>>();
            Assert.IsNotNull(impls);
            Assert.AreEqual(2, ((List<IService>) impls).Count);
        }

        [Test]
        public void InnerDependencyTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<ISmth, ISmthImpl>();
            configuration.Register<IService, FirstIServiceImpl>();
            configuration.Register<IService, SecondIServiceImpl>();
            configuration.Register<IClient, SecondIClientImpl>();
            DependenciesProvider provider = new DependenciesProvider(configuration);

            FirstIServiceImpl cl1 = (FirstIServiceImpl) provider.Resolve<IService>();
            Assert.IsNotNull(cl1.Smth);

            SecondIClientImpl cl2 = (SecondIClientImpl) provider.Resolve<IClient>();
            Assert.IsNotNull(cl2.Serv);
            Assert.AreEqual(2, ((List<IService>) cl2.Serv).Count);
        }

        [Test]
        public void SimpleRecursionTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<IClient, FirstIClientImpl>();
            configuration.Register<IData, IDataImpl>();
            DependenciesProvider provider = new DependenciesProvider(configuration);

            FirstIClientImpl client = (FirstIClientImpl) provider.Resolve<IClient>();
            Assert.IsNull(((IDataImpl) client.Data).Cl);
        }

        [Test]
        public void SimpleOpenGenericTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<IAnother<ISmth>, First<ISmth>>();
            configuration.Register(typeof(IFoo<>), typeof(Second<>));
            DependenciesProvider provider = new DependenciesProvider(configuration);

            IAnother<ISmth> cl1 = provider.Resolve<IAnother<ISmth>>();
            Assert.IsNotNull(cl1);
            
            IFoo<IService> cl2 = provider.Resolve<IFoo<IService>>();
            Assert.IsNotNull(cl2);
        }

        [Test]
        public void OneClassTest()
        {
            DependenciesConfiguration configuration = new DependenciesConfiguration();
            configuration.Register<HumanImpl, HumanImpl>();
            DependenciesProvider provider = new DependenciesProvider(configuration);
            HumanImpl humanImpl = provider.Resolve<HumanImpl>();
            Assert.IsNotNull(humanImpl);
        }
    }

    interface IAnother<T>
        where T : ISmth
    {
    }

    class First<T> : IAnother<T>
        where T : ISmth
    {
    }

    interface IFoo<T>
        where T : IService
    {
    }

    class Second<T> : IFoo<T>
        where T : IService
    {
    }

    public interface IHuman
    {
    }

    public class HumanImpl : IHuman
    {
    }
}