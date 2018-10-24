using FluentMigrator;

namespace ConsoleApp.Migrations
{
    [Migration(2)]
    public class AddCarsTable : Migration
    {
        public override void Up()
        {
            Create.Table("cars")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("owner_id").AsInt64();
        }

        public override void Down()
        {
            Delete.Table("cars");
        }
    }
}
