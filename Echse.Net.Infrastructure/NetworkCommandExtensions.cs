using Echse.Net.Domain;
using Echse.Net.Serialization;

namespace Echse.Net.Infrastructure
{
    public static class NetworkCommandExtensions
    {
        public static NetworkCommand ToNetworkCommand<T>(this T instance, byte commandName,
            IByteArraySerializationAdapter serializationAdapter) => new()
        {
            CommandName = commandName,
            CommandArgument = typeof(T).FullName,
            Data = serializationAdapter.SerializeObject(instance)
        };
    }
}