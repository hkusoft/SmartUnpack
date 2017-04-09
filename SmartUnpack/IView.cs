namespace SmartUnpack
{
    public interface IView
    {
        object DataContext { get; set; }
        void Close();
    }
}
