# PersonManager

Project Description: Thread-Safe Person Collection

The goal of this project is to design and implement a thread-safe collection for managing Person objects.

The main component of the project is a class named PersonCollection. This class is responsible for storing Person objects, supporting efficient retrieval/removal of the person with the maximum calculated value, and notifying subscribers about collection changes.

The collection should support the following functionality:

1. Adding persons

The collection must allow adding a Person object provided by the caller.

The Add operation may run in O(n) worst-case time complexity.

2. Removing the person with the maximum value

The collection must allow removing and returning the Person object that currently has the maximum value.

The Remove operation must run in O(1) worst-case time complexity.

3. Flexible maximum-value calculation

The collection should not depend on specific properties of the Person object.

The logic used to determine which person has the maximum value should be external to the collection and configurable when the collection instance is created.

This allows the collection to support different comparison algorithms without changing its internal implementation.

For a single instance of PersonCollection, only one maximum-value calculation algorithm is required. The algorithm does not need to change during the lifetime of that collection instance.

4. Subscriber notifications

The collection must support publishing notifications to subscriber objects whenever a person is added or removed.

Subscribers should be notified after each successful Add or Remove operation.

5. Thread safety

The collection must be safe to use from multiple threads.

Concurrent calls to Add, Remove, and Publish-related logic should not corrupt the internal state of the collection or produce inconsistent results.

TBD in next steps:

1. Periodic size notification

Every 60 seconds, the collection should notify all subscribers about the current number of persons stored in the collection.

2. Internal data structure analysis

The project should include a suggestion of two different internal data structures that can be used to implement the required behavior, including their trade-offs regarding Add, Remove, maximum lookup, and thread safety.