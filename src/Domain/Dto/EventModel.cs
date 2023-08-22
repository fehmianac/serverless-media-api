namespace Domain.Dto;

public class EventModel<T>
{
    public EventModel(string eventName, T data)
    {
        EventName = eventName;
        Data = data;
    }

    public string EventName { get; }
    public T Data { get; }
}