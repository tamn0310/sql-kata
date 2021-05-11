using Dapper.FluentMap.Mapping;

namespace Product.API.Domains.Entities
{
    public class Product : BaseEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ProductCategoryId { get; set; }
    }

    public class ProductMap : EntityMap<Product>
    {
        public ProductMap()
        {
            this.Map(x => x.Id).ToColumn("id");
            this.Map(x => x.ProductCategoryId).ToColumn("product_category_id");
            this.Map(x => x.Name).ToColumn("name");
            this.Map(x => x.CreatedAt).ToColumn("created_at");
            this.Map(x => x.CreatedBy).ToColumn("created_by");
            this.Map(x => x.UpdateaBy).ToColumn("updated_by");
            this.Map(x => x.UpdatedAt).ToColumn("updated_at");
            this.Map(x => x.IsDeleted).ToColumn("is_deleted");
        }
    }
}