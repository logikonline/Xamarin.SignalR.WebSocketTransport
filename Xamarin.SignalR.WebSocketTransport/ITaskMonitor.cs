namespace Xamarin.SignalR.Transport
{
    internal interface ITaskMonitor
    {
        void TaskStarted();
        void TaskCompleted();
    }
}
