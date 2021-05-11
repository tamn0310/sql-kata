using Dapper.FluentMap.Mapping;

namespace Product.API.Domains.Entities
{
    public class ProductCategory : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ProductCategoryMap : EntityMap<ProductCategory>
    {
        public ProductCategoryMap()
        {
            this.Map(x => x.Id).ToColumn("id");
            this.Map(x => x.Name).ToColumn("name");
            this.Map(x => x.Description).ToColumn("description");
            this.Map(x => x.CreatedAt).ToColumn("created_at");
            this.Map(x => x.CreatedBy).ToColumn("created_by");
            this.Map(x => x.UpdatedAt).ToColumn("updated_at");
            this.Map(x => x.UpdateaBy).ToColumn("updated_by");
            this.Map(x => x.IsDeleted).ToColumn("is_deleted");
        }
    }
}