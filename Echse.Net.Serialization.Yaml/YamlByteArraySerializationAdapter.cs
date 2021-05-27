using System;
using System.Text;

namespace Echse.Net.Serialization.Yaml
{
    public class YamlByteArraySerializationAdapter
    {
        private YamlSerializerAdapter YamlSerializerAdapter { get; set; } = new YamlSerializerAdapter();

        public T DeserializeObject<T>(byte[] content)
        {
            return YamlSerializerAdapter.DeserializeObject<T>(Encoding.UTF8.GetString(content));
        }

        public object DeserializeObject(byte[] content, Type type)
        {
            return YamlSerializerAdapter.DeserializeObject(Encoding.UTF8.GetString(content), type);
        }

        public byte[] SerializeObject<T>(T instance)
        {
            return Encoding.UTF8.GetBytes(YamlSerializerAdapter.SerializeObject<T>(instance));
        }

        public byte[] SerializeObject(object instance, Type type)
        {
            return Encoding.UTF8.GetBytes(YamlSerializerAdapter.SerializeObject(instance, type));
        }
    }
}
