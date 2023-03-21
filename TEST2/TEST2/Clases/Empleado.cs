namespace TEST2.Clases
{
    public class Empleado : Persona
    {
        public Empleado(int age, string name, Guid id, string position) : base(age, name, id)
        {
            this.Position = position;
        }                                       

        public string Position { get; set; }
    }
}
