using Autofac;
using Autofac.Extensions.DependencyInjection;

namespace DTAClient
{
    public class ServiceProvider : AutofacServiceProvider
    {
        public ServiceProvider(ILifetimeScope lifetimeScope) : base(lifetimeScope)
        {
        }

        public T GetService<T>() => (T) base.GetService(typeof(T));
        public T GetRequiredService<T>() => (T) base.GetRequiredService(typeof(T));
    }
}