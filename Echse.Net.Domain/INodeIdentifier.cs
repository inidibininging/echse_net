namespace Echse.Net.Domain
{
    public interface INodeIdentifier<TIdentifier>
    {
        public TIdentifier Id { get; set; }
    }
}