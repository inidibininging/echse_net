using Echse.Net.Serialization;
using MsgPack;
using MsgPack.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Echse.Net.Serialization.MsgPack
{
    public class MsgPackSerializerAdapter : IStringSerializationAdapter
    {
        public T DeserializeObject<T>(string content)
        {
            var context = new SerializationContext { SerializationMethod = SerializationMethod.Map };
            context.DictionarySerlaizationOptions.OmitNullEntry = true;
            
            var serializer = SerializationContext.Default.GetSerializer<T>(context);

            T subject;
            using (var ms = new MemoryStream(Convert.FromBase64String(content)))
            {
                ms.Position = 0;
                subject = serializer.Unpack(ms);
            }
            return subject;
        }

        public object DeserializeObject(string content, Type type)
        {
            var serializer = MessagePackSerializer.Get(type);
            object subject;


            using (var ms = new MemoryStream(Convert.FromBase64String(content)))
            {
                ms.Position = 0;                
                subject = serializer.Unpack(ms);
            }
            return subject;
        }

        public string SerializeObject<T>(T instance)
        {
            var finalString = new StringBuilder();

            var context = new SerializationContext { SerializationMethod = SerializationMethod.Map };
            context.DictionarySerlaizationOptions.OmitNullEntry = true;
            var serializer = SerializationContext.Default.GetSerializer<T>(context);
           

            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, instance);
                ms.Position = 0;
                finalString.Append(Convert.ToBase64String(ms.ToArray()));
            }
            return finalString.ToString();
        }

        public string SerializeObject(object instance, Type type)
        {
            var serializer = MessagePackSerializer.Get(type);
            var finalString = new StringBuilder();
            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, instance);
                ms.Position = 0;
                finalString.Append(Convert.ToBase64String(ms.ToArray()));
            }
            return finalString.ToString();
        }
    }
}
