using System;
using System.Collections.Generic;
using System.Text;

namespace Echse.Net.Serialization
{
    public interface IGenericSerializationAdapter<TOutput>
    {
        TOutput SerializeObject<T>(T instance);
        TOutput SerializeObject(object instance, Type type);
        T DeserializeObject<T>(TOutput content);
        object DeserializeObject(TOutput content, Type type);
    }
}
