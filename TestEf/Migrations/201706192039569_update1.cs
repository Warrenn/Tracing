namespace TestEf.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update1 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Lectures", "LectureName", c => c.String(maxLength: 255));
            AlterColumn("dbo.Students", "Name", c => c.String(maxLength: 255));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Students", "Name", c => c.String());
            AlterColumn("dbo.Lectures", "LectureName", c => c.String());
        }
    }
}
