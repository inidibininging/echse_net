using Echse.Net.Domain;
using Echse.Net.Serialization;

namespace Echse.Net.Infrastructure
{
    /// <summary>
    /// Deserializes a string to a network command object
    /// </summary>
    public sealed class NetworkCommandTranslator : IInputTranslator<byte[], NetworkCommand>
    {
        public IByteArraySerializationAdapter SerializationAdapter { get; }

        public NetworkCommandTranslator(IByteArraySerializationAdapter serializationAdapter)
        {
            SerializationAdapter = serializationAdapter;
        }
        /// <summary>
        /// Translates the string input into a serialized Object
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public NetworkCommand Translate(byte[] input)
        {
            if (input.Length == 0)
                throw new NoValidNetworkCommandException();
            return SerializationAdapter.DeserializeObject<NetworkCommand>(input);
        }
    }
}