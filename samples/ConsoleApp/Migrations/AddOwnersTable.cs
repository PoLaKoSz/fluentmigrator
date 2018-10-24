using FluentMigrator;

namespace ConsoleApp.Migrations
{
    [Migration(1)]
    public class AddOwnersTable : Migration
    {
        public override void Up()
        {
            Create.Table("owners")
                .WithColumn("id").AsInt64().PrimaryKey().Identity()
                .WithColumn("first_name").AsString()
                .WithColumn("last_name").AsString();
        }

        public override void Down()
        {
            Delete.Table("owners");
        }
    }
}
