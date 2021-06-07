using Echse.Domain;
using Echse.Language;
using States.Core.Infrastructure.Services;

namespace Echse.Net.Lidgren.Instructions
{
    public class NewId: ICustomReturnInstruction
    {
        public void Handle(IStateMachine<string, IEchseContext> machine)
        {
            ReturnTagValue = System.Guid.NewGuid().ToString().Replace("-", "");
        }

        public string ReturnTagValue { get; private set; }
    }
}