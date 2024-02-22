namespace LabelChecker.Model;

public class EventModel<T> : EventModel
{
    public EventModel(string eventName, T data)
    {
        EventName = eventName;
        Data = data;
    }

    public T Data { get; }
}

public class EventModel
{
    public string EventName { get; init; } = default!;
}