using System;

namespace Echse.Net.Serialization.Yaml
{
    public class YamlSerializerAdapter : IStringSerializationAdapter
    {
        private SharpYaml.Serialization.Serializer Serializer { get; set; } = new SharpYaml.Serialization.Serializer(
                new SharpYaml.Serialization.SerializerSettings()
                {
                    EmitAlias = false,
                    EmitJsonComptible= true                      
                });
        
        public YamlSerializerAdapter()
        {

        }
        public T DeserializeObject<T>(string content)
        {
            return Serializer.Deserialize<T>(content);
        }

        public object DeserializeObject(string content, Type type)
        {
            return Serializer.Deserialize(content, type);
        }

        public string SerializeObject<T>(T instance)
        {
            return Serializer.Serialize(instance);
        }

        public string SerializeObject(object instance, Type type)
        {
            return Serializer.Serialize(instance, type);
        }
    }
}
