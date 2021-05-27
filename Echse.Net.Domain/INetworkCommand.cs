namespace Echse.Net.Domain
{
    public interface INetworkCommand<TCommandIdentifier, TCommandArgument, TData>
    {
        
        TCommandIdentifier CommandName { get; set; }
        TCommandArgument CommandArgument { get; set; }
        TData Data { get; set; }
    }
}