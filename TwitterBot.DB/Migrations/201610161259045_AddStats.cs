namespace TwitterBot.DB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddStats : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Statistics",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        Followers = c.Int(nullable: false),
                        Followings = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Statistics");
        }
    }
}
