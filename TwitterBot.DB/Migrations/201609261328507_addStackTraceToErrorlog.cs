namespace TwitterBot.DB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addStackTraceToErrorlog : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ErrorLogs", "StackTrace", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ErrorLogs", "StackTrace");
        }
    }
}
