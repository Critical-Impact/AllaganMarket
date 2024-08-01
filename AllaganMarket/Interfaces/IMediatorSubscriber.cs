namespace AllaganMarket.Interfaces;

using AllaganMarket.Services;

public interface IMediatorSubscriber
{
    MediatorService MediatorService { get; }
}