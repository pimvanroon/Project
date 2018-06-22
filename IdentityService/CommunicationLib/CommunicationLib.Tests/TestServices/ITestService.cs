using Communication.Attributes;
using Communication.Tests.TestModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Communication.Tests
{
    public interface ITestService
    {
        void AddPerson(Person person);

        [Query]
        IEnumerable<Person> FindPersons(string name);

        [Query]
        Task<IEnumerable<Person>> FindPersons2Async(string name);
    }
}
