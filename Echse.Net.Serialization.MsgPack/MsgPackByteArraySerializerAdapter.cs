using MsgPack;
using MsgPack.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Echse.Net.Serialization.MsgPack
{
    public class MsgPackByteArraySerializerAdapter : IByteArraySerializationAdapter
    {
        public T DeserializeObject<T>(byte[] content)
        {
            var context = new SerializationContext { SerializationMethod = SerializationMethod.Map };
            context.DictionarySerlaizationOptions.OmitNullEntry = true;
            
            var serializer = SerializationContext.Default.GetSerializer<T>(context);

            T subject;
            using (var ms = new MemoryStream(content))
            {
                ms.Position = 0;
                subject = serializer.Unpack(ms);
            }
            return subject;
        }

        public object DeserializeObject(byte[] content, Type type)
        {
            var serializer = MessagePackSerializer.Get(type);
            object subject;


            using (var ms = new MemoryStream(content))
            {
                ms.Position = 0;                
                subject = serializer.Unpack(ms);
            }
            return subject;
        }

        public byte[] SerializeObject<T>(T instance)
        {
            // var context = new SerializationContext { SerializationMethod = SerializationMethod.Map };
            // context.DictionarySerlaizationOptions.OmitNullEntry = true;
            var serializer = MessagePackSerializer.Get<T>();

            byte[] output;
            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, instance);
                ms.Position = 0;
                output = ms.ToArray();
            }
            return output;
        }

        public byte[] SerializeObject(object instance, Type type)
        {
            var serializer = MessagePackSerializer.Get(type);
            var finalString = new StringBuilder();
            byte[] output;
            using (var ms = new MemoryStream())
            {
                serializer.Pack(ms, instance);
                ms.Position = 0;
                output = ms.ToArray();
            }
            return output;
        }
    }
}
