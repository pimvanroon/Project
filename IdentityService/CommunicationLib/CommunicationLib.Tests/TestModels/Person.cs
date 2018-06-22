using System.ComponentModel.DataAnnotations;

namespace Communication.Tests.TestModels
{
    public enum Gender
    {
        Male = 0,
        Female = 1,
        SomethingInbetween = 42
    }

    public class Person
    {
        [Required]
        public string Name { get; set; }

        public Gender Gender { get; set; }

        public int Age { get; set; }
    }
}
