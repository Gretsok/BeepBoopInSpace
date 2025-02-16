namespace Game.ArchitectureTools.Manager
{
    public class ItemsManager : AManager<ItemsManager>
    {
        private int nextItemInstanceId;
        
        public int GetNextItemInstanceId()
        {
            nextItemInstanceId++;
            return nextItemInstanceId;
        }
    }
}
