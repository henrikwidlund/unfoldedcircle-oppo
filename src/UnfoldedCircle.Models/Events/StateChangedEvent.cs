namespace UnfoldedCircle.Models.Events;

public record StateChangedEvent<TAttributes> : CommonEventRequired<StateChangedEventMessageData<TAttributes>>
    where TAttributes : StateChangedEventMessageDataAttributes;