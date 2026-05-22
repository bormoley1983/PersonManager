using PersonManager;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Init");
        PersonCollection personCollection = new PersonCollection();

        var person1 = new Person(1, "John", "Doe", new DateOnly(1990, 1, 1), 180);
        var person2 = new Person(2, "Jane", "Smith", new DateOnly(1992, 2, 2), 165);

        await personCollection.Add(person1);
        await personCollection.Add(person2);
    }
}

internal class Person : IPerson
{
    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public DateOnly BirthDate { get; }
    public int Height { get; }
    public Person(int id, string firstName, string lastName, DateOnly birthDate, int height)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        BirthDate = birthDate;
        Height = height;
    }
}