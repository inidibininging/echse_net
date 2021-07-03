using Echse.Net.Serialization;
using Lidgren.Network;


namespace Echse.Net.Infrastructure.Lidgren
{
    public static class NetworkExtensions
    {
        /// <summary>
        /// Serializes an object of type T to a client message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stuff"></param>
        /// <returns></returns>
        public static NetOutgoingMessage ToClientOutgoingMessage<T>(this T stuff, NetPeer client, IStringSerializationAdapter serializationAdapter)
        {
            return client.CreateMessage(serializationAdapter.SerializeObject(stuff));
        }

        /// <summary>
        /// Serializes an object of type T to a client message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stuff"></param>
        /// <returns></returns>
        public static NetOutgoingMessage ToClientOutgoingMessage<T>(this T stuff, NetPeer client, IByteArraySerializationAdapter serializationAdapter)
        {
            //not sure if this works
            var msg = client.CreateMessage();
            msg.Write(serializationAdapter.SerializeObject(stuff));
            return msg;
        }
    }
}