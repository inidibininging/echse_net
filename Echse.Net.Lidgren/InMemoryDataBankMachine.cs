using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Echse.Domain;
using States.Core.Common;
using States.Core.Common.Delegation;
using States.Core.Common.Storage;
using States.Core.Infrastructure.Services;

namespace Echse.Net.Lidgren
{
    public class InMemoryDataBankMachine :
        StateMachine<string, IEchseContext>
        
    {
        public InMemoryDataBankMachine(InMemoryDataBankApi api) : base(api, api, api)
        {
            
        }
    }
    
}