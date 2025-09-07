namespace Match3Tray.Interface
{
    public interface IBindingContext
    {
        public void SetBindingData();
        public void RegisterBindingContext();
        public void UnregisterBindingContext();
    }
}