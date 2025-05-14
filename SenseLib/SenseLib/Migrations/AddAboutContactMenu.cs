using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SenseLib.Migrations
{
    public partial class AddAboutContactMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Thêm menu Giới thiệu
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Menu WHERE ControllerName = 'About' AND ActionName = 'Index')
                BEGIN
                    INSERT INTO Menu (MenuName, IsActive, ControllerName, ActionName, Levels, ParentID, Link, MenuOrder, Position)
                    VALUES (N'Giới thiệu', 1, 'About', 'Index', 1, 0, '/About', 2, 1)
                END
                ELSE
                BEGIN
                    UPDATE Menu 
                    SET ControllerName = 'About', ActionName = 'Index', Link = '/About'
                    WHERE ControllerName = 'Home' AND ActionName = 'About'
                END
            ");

            // Thêm menu Liên hệ
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM Menu WHERE ControllerName = 'Contact' AND ActionName = 'Index')
                BEGIN
                    INSERT INTO Menu (MenuName, IsActive, ControllerName, ActionName, Levels, ParentID, Link, MenuOrder, Position)
                    VALUES (N'Liên hệ', 1, 'Contact', 'Index', 1, 0, '/Contact', 3, 1)
                END
                ELSE
                BEGIN
                    UPDATE Menu 
                    SET ControllerName = 'Contact', ActionName = 'Index', Link = '/Contact'
                    WHERE ControllerName = 'Home' AND ActionName = 'Contact'
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE Menu SET ControllerName = 'Home', ActionName = 'About', Link = '/Home/About' 
                WHERE ControllerName = 'About' AND ActionName = 'Index';
                
                UPDATE Menu SET ControllerName = 'Home', ActionName = 'Contact', Link = '/Home/Contact' 
                WHERE ControllerName = 'Contact' AND ActionName = 'Index';
            ");
        }
    }
} 