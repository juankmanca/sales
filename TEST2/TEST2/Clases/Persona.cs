namespace TEST2.Clases
{
    public class Persona
    {
  
        public int Age { get; set; }
        public string Name;
        public Guid id;

        public Persona(int age, string name, Guid id)
        {
            Age = age;
            Name = name;
            this.id = id;
        }

    }
}
