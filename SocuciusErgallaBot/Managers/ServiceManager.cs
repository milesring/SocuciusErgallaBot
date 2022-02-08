using Microsoft.Extensions.DependencyInjection;

namespace SocuciusErgallaBot.Managers
{
    internal static class ServiceManager
    {
        public static IServiceProvider Provider { get; private set; }
        public static void SetProvider(ServiceCollection collection)
            => Provider = collection.BuildServiceProvider();

        public static T GetService<T>() where T : new() 
            => Provider.GetRequiredService<T>();
    }
}
