using SimpleInjector;

namespace DTAClient
{
    public class ServiceProvider
    {
        private readonly Container container;

        public ServiceProvider(
            Container container
        )
        {
            this.container = container;
        }

        public T GetService<T>() => (T) container.GetInstance(typeof(T));
    }
}
