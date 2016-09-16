namespace TwitterBot.DB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLogTables : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BlackLists",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ErrorLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        Message = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.StatusLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.DateTime(nullable: false),
                        Message = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.StatusLogs");
            DropTable("dbo.ErrorLogs");
            DropTable("dbo.BlackLists");
        }
    }
}
