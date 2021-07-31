using System;
using System.Collections.Generic;
using Echse.Domain;
using States.Core.Infrastructure.Services;

namespace Echse.Net.Lidgren
{
    public class InMemoryDataBankApi :
        IStateGetService<string, IEchseContext>,
        IStateSetService<string, IEchseContext>,
        IStateNewService<string, IEchseContext>
    {
        private Dictionary<string, IState<string, IEchseContext>> _banks = new();

        public IEnumerable<string> States => _banks.Keys;

        public IState<string, IEchseContext> Get(string identifier) => _banks[identifier];


        public bool HasState(string identifier) => _banks.ContainsKey(identifier);

        public bool Set(string identifier, IState<string, IEchseContext> state)
        {
            if (state == null || string.IsNullOrWhiteSpace(identifier))
                return false;
            _banks[identifier] = state;
            return true;
        }

        public string New(IState<string, IEchseContext> state)
        {
            var newId = Guid.NewGuid().ToString();
            _banks.Add(newId, state);
            return newId;
        }

        public string New(string identifier, IState<string, IEchseContext> state)
        {
            _banks.Add(identifier, state);
            return identifier;
        }
    }
}