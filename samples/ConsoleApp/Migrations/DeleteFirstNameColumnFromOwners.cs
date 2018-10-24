using FluentMigrator;

namespace ConsoleApp.Migrations
{
    [Migration(3)]
    public class DeleteFirstNameColumnFromOwners : Migration
    {
        public override void Up()
        {
            Delete.Column("first_name")
                .FromTable("owners");
        }

        public override void Down()
        {
            Alter.Table("owners")
                .AddColumn("first_name").AsString();
        }
    }
}
