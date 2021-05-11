namespace Product.API.Domains.Entities
{
    public abstract class BaseEntity
    {
        public long CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public long? UpdatedAt { get; set; }
        public string? UpdateaBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}