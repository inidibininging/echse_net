using System;
using Echse.Domain;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;

namespace Echse.Net.Lidgren
{
    public class TagSyncConsumer : IObserver<NetworkCommandConnection<long>>, IDisposable
    {
        private NetworkCommandDataConverterService _dataConverterService;
        private InMemoryDataBankMachine _languageContext;
        public TagSyncConsumer(
            NetworkCommandDataConverterService dataConverterService,
            InMemoryDataBankMachine languageContext)
        {
            _dataConverterService =
                dataConverterService ?? throw new ArgumentNullException(nameof(dataConverterService));
            _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
            
        }
        
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(NetworkCommandConnection<long> value)
        {
            if (Disposed)
                return;

            var tagVariableUntyped = _dataConverterService.ConvertToObject(value);
            if (value.CommandArgument == typeof(TagVariable).FullName && tagVariableUntyped is TagVariable tagVariable)
            {
                Console.WriteLine($"Tag variable found for syncing {tagVariable.Name} {tagVariable.Value}");
                Console.WriteLine(tagVariable);
                _languageContext.SharedContext.AddVariable(tagVariable);
            }
        }
        
        private bool Disposed { get; set; }
        
        public void Dispose()
        {
            if (Disposed)
                return;
            _languageContext = null;
            _dataConverterService = null;
            Disposed = true;
        }
    }
}