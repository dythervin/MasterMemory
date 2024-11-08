namespace MasterMemory
{
    public delegate TVal ValueGetter<TKey, out TVal>(in TKey item);
}