namespace Macroscop_FaceRecReport.Entities
{
    public class Person
    {
        public string Id { get; private set; }
        public string Module { get; private set; }

        public Person (string id, string module)
        {
            Id = id;
            Module = module;
        }
    }
}
