using Communication.Tests.TestModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Communication.Tests
{

    public class TestService : ITestService
    {
        private readonly ConcurrentBag<Person> persons = new ConcurrentBag<Person>();

        public void AddPerson(Person person)
        {
            persons.Add(person);
        }

        public IEnumerable<Person> FindPersons(string name)
        {
            return persons.Where(it => it.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public Task<IEnumerable<Person>> FindPersons2Async(string name)
        {
            return Task.FromResult(persons.Where(it => it.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
