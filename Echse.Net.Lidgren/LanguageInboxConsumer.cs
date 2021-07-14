using System;
using Echse.Language;
using Echse.Net.Domain;
using Echse.Net.Infrastructure;

namespace Echse.Net.Lidgren
{
    public class LanguageInboxConsumer : 
        IObserver<NetworkCommandConnection<long>>, IDisposable
    {
        private NetworkCommandDataConverterService _dataConverterService;
        private readonly Interpreter _echseInterpreter;

        public LanguageInboxConsumer(
            NetworkCommandDataConverterService dataConverterService,
            Interpreter echseInterpreter)
        {
            _dataConverterService =
                dataConverterService ?? throw new ArgumentNullException(nameof(dataConverterService));
            _echseInterpreter = echseInterpreter ?? throw new ArgumentNullException(nameof(echseInterpreter));
        }

        public void OnCompleted()
        {
            Console.WriteLine("Done");
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error?.Message);
        }

        public void OnNext(NetworkCommandConnection<long> value)
        {
            if (Disposed)
                return;
            
            Console.WriteLine($"Evaluating source");
            var sourceCode = _dataConverterService.ConvertToObject(value);
            if (value.CommandArgument == typeof(string).FullName && sourceCode is string code)
            {
                //Console.WriteLine(code);
                //_echseInterpreter.Instructions.Clear();
                _echseInterpreter.Run(code);
                
                _echseInterpreter.Context.Run("Main");
            }
        }

        private bool Disposed { get; set; }
        
        public void Dispose()
        {
            if (Disposed)
                return;
            _dataConverterService = null;
            Disposed = true;
        }
    }
}