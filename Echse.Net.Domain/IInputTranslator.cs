namespace Echse.Net.Domain
{
    public interface IInputTranslator<in TKeyInputType, out TKeyOutputType>
    {
        TKeyOutputType Translate(TKeyInputType input);
    }

    public interface IInputTranslator<in TKeyInputType, out TKeyOutputType, TSharedBaseType>
        where TKeyInputType : TSharedBaseType
        where TKeyOutputType : TSharedBaseType
    {
        TKeyOutputType Translate(TKeyInputType input);
    }

    public interface IInputTranslator<in TKeyInputType, out TKeyOutputType, TLeftType, TRightType>
       where TKeyInputType : TLeftType
       where TKeyOutputType : TRightType
    {
        TKeyOutputType Translate(TKeyInputType input);
    }
}