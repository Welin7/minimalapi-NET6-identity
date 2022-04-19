namespace MinimalApiNet6.Models
{
    public class Patient
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Document{ get; set; }

        public bool IsActive { get; set; }
    }
}
