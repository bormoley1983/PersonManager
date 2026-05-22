using System.Collections.Concurrent;
using System.Timers;
using Timer = System.Timers.Timer;

namespace PersonManager
{
    //hardcoded IPubSub
    //concrete PubSubImplementation
    //no subscriber registration model
    //The task wording suggests an Observer / Publisher-Subscriber style collection with explicit subscribers, not a fixed internal publisher implementation.
    public interface IPubSub
    {
        Task<int> Publish(Message message);
        Task Subscribe();
        Task Unsubscribe();
    }

    public class PubSubImplementation : IPubSub
    {
        public Task<int> Publish(Message message)
        {
            // implementation of pubsub

            return Task.FromResult(0);
        }

        public async Task Subscribe()
        {
            // subscribe to count notifications
            // no clear contract for count notifications, so subscribing to all messages and filtering by type
            // also no clear contract for message size, so not utilizing it
            // await _pubsub.Subscribe(message =>
            // {
            //     if (message.ObjectType == ObjectType.Person &&
            //         message.OperationType == OperationType.Count)
            //     {
            //         var count = (long)message.Notification;
            //         Console.WriteLine($"Current person count: {count}");
            //     }
            //
            //     return Task.CompletedTask;
            // });
        }

        public async Task Unsubscribe()
        {
            // await _pubsub.Unsubscribe(...);
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
        private const int NOTIFICATION_TIMER = 5000;//60000;

        //Phase 2: max tracking - can be implemented with a sorted collection and a comparer
        //To meet the complexity target, the simplest practical design is:
        //•	a linked structure that keeps current max at the head
        //•	Add scans to insert in the right place → O(n)
        //•	Remove pops head → O(1)
        private readonly ConcurrentDictionary<int, IPerson> _inMemoryPersonCollection = new();

        //locking per personId does not protect global collection invariants
        //•	once max-tracking is introduced, a per-item lock will be insufficient
        //•	_lock also depends on Id, which is not allowed by the task
        //•	publish + state changes are not coordinated as a single collection-level operation
        //•	For this problem, a single collection - level lock is the simplest correct approach.
        private readonly ConcurrentDictionary<int, object> _lock = new();

        private readonly IPubSub _pubsub = new PubSubImplementation();

        //•	_timer is static
        //•	async void OnTimedEvent(...)
        //•	no disposal
        //•	uses LINQ-style Count() call instead of a maintained count field / property access
        //•	no clear subscriber contract for count notifications
        private static Timer _timer;

        public PersonCollection()
        {
            schedulerSetup();
        }

        public async Task Add(IPerson person)
        {
            //Phase 3 Sorted doubly linked list
            //•	Add: find position O(n)
            //•	Remove max: remove head O(1)
            //OR Sorted singly linked list
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

        //Should do Remove MaxValue
        public async Task<IPerson> Remove(int personId)
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

            return removedPerson;
            // WC time of O(1) - per record
        }

        //Phase 2: max tracking - can be implemented with a sorted collection and a comparer
        //or Func<IPerson, IPerson, int>
        private IComparer<IPerson> _comparer;

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
