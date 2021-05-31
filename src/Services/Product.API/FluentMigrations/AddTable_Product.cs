using FluentMigrator;

namespace Product.API.FluentMigrations
{
    [Migration(202105111135000)]
    public class AddTable_Product : Migration
    {
        public override void Down()
        {
            this.Delete.Table("products");
        }

        public override void Up()
        {
            this.Create.Table("products")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("product_category_id").AsInt32().NotNullable()
                .WithColumn("name").AsString().NotNullable()
                .WithColumn("created_at").AsInt64().NotNullable()
                .WithColumn("created_by").AsString().NotNullable()
                .WithColumn("updated_at").AsInt64().Nullable()
                .WithColumn("updated_by").AsString().Nullable()
                .WithColumn("is_deleted").AsBoolean().WithDefaultValue(0);
        }
    }
}