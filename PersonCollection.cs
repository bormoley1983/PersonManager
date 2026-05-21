using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PersonManager
{
    internal class PersonCollectionRuntimeTest
    {
        // Runtime tests
        // 1. run scheduler each n seconds and publish queue of consumed messages per period
    }

    public interface IPubSub
    {
        Task<int> Publish(Message message);
    }

    public class PubSubImplementation : IPubSub
    {
        public Task<int> Publish(Message message)
        {
            // implementation of pubsub

            return Task.FromResult(0);
        }
    }

    public enum ObjectType
    {
        Person
    }

    public enum OperationType
    {
        Add,
        Remove,
        Count
    }

    public record Message(
        Guid Id,
        ObjectType ObjectType,
        OperationType OperationType,
        object Notification)
    {
        public static Message AddPerson(Guid messageId, IPerson person) =>
            new(messageId, ObjectType.Person, OperationType.Add, person);

        public static Message RemovePerson(Guid messageId, int personId) =>
            new(messageId, ObjectType.Person, OperationType.Remove, personId);

        public static Message CountPersons(Guid messageId, long personsCount) =>
            new(messageId, ObjectType.Person, OperationType.Count, personsCount);
    }

    public class PersonCollection
    {
        private const int NOTIFICATION_TIMER = 60000;

        private readonly ConcurrentDictionary<int, IPerson> _inMemoryPersonCollection = new();
        private readonly ConcurrentDictionary<int, object> _lock = new();

        private readonly IPubSub _pubsub = new PubSubImplementation();

        private static Timer _timer;

        public PersonCollection()
        {
            schedulerSetup();
        }

        public async Task AddPerson(IPerson person)
        {
            // prechecks skipped: no single point of truth
            // Id can also be removed or updated -> can calculate record hash
            // also no 100% guarantee for uniqueness

            bool result;
            var personLock = _lock.GetOrAdd(person.Id, _ => new object());

            lock (personLock)
            {
                result = _inMemoryPersonCollection.TryAdd(person.Id, person);
            }

            if (!result)
                throw new InvalidOperationException("person could not be added");

            var addPersonMessage = Message.AddPerson(Guid.NewGuid(), person);
            await _pubsub.Publish(addPersonMessage);

            // WC time of O(n) per param?
        }

        public async Task RemovePerson(int personId)
        {
            // 1 person with maximum value? removed

            bool result;
            IPerson removedPerson;

            var personLock = _lock.GetOrAdd(personId, _ => new object());

            lock (personLock)
            {
                result = _inMemoryPersonCollection.TryRemove(personId, out removedPerson);
            }

            if (!result)
                throw new InvalidOperationException("person record is not available");

            var removePersonMessage = Message.RemovePerson(Guid.NewGuid(), personId);
            await _pubsub.Publish(removePersonMessage);

            // WC time of O(1) - per record
        }

        private Task schedulerSetup()
        {
            _timer = new Timer(NOTIFICATION_TIMER);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
            _timer.Enabled = true;

            return Task.CompletedTask;
        }

        private async void OnTimedEvent(object source, ElapsedEventArgs args)
        {
            // on timer

            // not mentioned to utilize message size - misunderstood task
            // sending all messages in one chunk - misunderstood task
            // Should return only the amount of messages

            var personCountMsg = Message.CountPersons(
                Guid.NewGuid(),
                _inMemoryPersonCollection.Count());

            await _pubsub.Publish(personCountMsg);
        }

        // ?
        // private Task AddToPublishQueue(
        //     ObjectType objectType,
        //     OperationType operationType,
        //     object[] notification)
        // {
        //     return Task.CompletedTask;
        // }
    }

    // You cannot rely on the properties in this interface,
    // they can be changed and/or removed.
    public interface IPerson
    {
        int Id { get; }
        string FirstName { get; }
        string LastName { get; }
        DateOnly BirthDate { get; }
        int Height { get; }

        // etc. there may be more, such as get property methods
        // You cannot rely on the properties in this interface,
        // they can be changed and removed.
    }
}
