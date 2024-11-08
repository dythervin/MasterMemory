namespace MasterMemory
{
    public delegate TKey KeySelector<TValue, out TKey>(in TValue item);
}