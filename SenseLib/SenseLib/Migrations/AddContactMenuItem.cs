using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SenseLib.Migrations
{
    public partial class AddContactMenuItem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AdminMenu WHERE ControllerName = 'Contact' AND ActionName = 'Index')
                BEGIN
                    INSERT INTO AdminMenu (MenuName, ItemLevel, IsActive, ParentLevel, ItemOrder, AreaName, ControllerName, ActionName, Icon)
                    VALUES (N'Quản lý liên hệ', 1, 1, 0, 8, 'Admin', 'Contact', 'Index', 'bi bi-chat-dots-fill')
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM AdminMenu WHERE ControllerName = 'Contact' AND ActionName = 'Index'
            ");
        }
    }
} 