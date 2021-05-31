using FluentMigrator;

namespace Product.API.FluentMigrations
{
    [Migration(202105111141000)]
    public class AddTable_Product_Category : Migration
    {
        public override void Down()
        {
            this.Delete.Table("product_categories");
        }

        public override void Up()
        {
            this.Create.Table("product_categories")
                .WithColumn("id").AsInt32().PrimaryKey().Identity()
                .WithColumn("name").AsString().NotNullable()
                .WithColumn("description").AsString().Nullable()
                .WithColumn("created_at").AsInt64().NotNullable()
                .WithColumn("created_by").AsString().NotNullable()
                .WithColumn("updated_at").AsInt64().Nullable()
                .WithColumn("updated_by").AsString().Nullable()
                .WithColumn("is_deleted").AsBoolean().WithDefaultValue(0);
        }
    }
}