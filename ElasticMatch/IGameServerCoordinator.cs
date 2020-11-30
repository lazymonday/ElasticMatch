namespace ElasticMatch
{
    public interface IGameServerCoordinator
    {
        IGameServer GetAppropriateOne();

        void MarkAlive(IGameServer gameServer);

        void Remove(IGameServer gameServer);
    }
}