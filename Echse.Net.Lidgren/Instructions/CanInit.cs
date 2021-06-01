using System.Linq;
using Echse.Domain;
using Echse.Language;
using States.Core.Infrastructure.Services;

namespace Echse.Net.Lidgren.Instructions
{
    public class CanInit : ICustomReturnInstruction
    {
        public void Handle(IStateMachine<string, IEchseContext> machine)
        {
            ReturnTagValue = "ok";
        }

        public string ReturnTagValue { get; private set; }
    }
}