using DalaMock.Core.Plugin;

namespace AllaganMarketMock
{
    using DalaMock.Core.DI;
    using DalaMock.Core.Mocks;

    class Program
    {
        static void Main(string[] args)
        {
            var dalamudConfiguration = new MockDalamudConfiguration()
            {
                GamePath = new DirectoryInfo("/var/home/blair/.xlcore/ffxiv/game/sqpack"),
                PluginSavePath = new DirectoryInfo("/var/home/blair/.xlcore/DalaMockConfigs"),
            };
            var mockContainer = new MockContainer(dalamudConfiguration);
            var mockDalamudUi = mockContainer.GetMockUi();
            var pluginLoader = mockContainer.GetPluginLoader();
            var mockPlugin = pluginLoader.AddPlugin(typeof(AllaganMarketPluginMock));
            pluginLoader.StartPlugin(mockPlugin);
            mockDalamudUi.Run();
        }
    }
}
