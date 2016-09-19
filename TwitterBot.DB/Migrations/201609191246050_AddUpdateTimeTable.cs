namespace TwitterBot.DB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUpdateTimeTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UpdateTimes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Time = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.UpdateTimes");
        }
    }
}
