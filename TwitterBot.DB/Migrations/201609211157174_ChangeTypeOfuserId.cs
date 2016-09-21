namespace TwitterBot.DB.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeTypeOfuserId : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.BlackLists", "UserId", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.BlackLists", "UserId", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
