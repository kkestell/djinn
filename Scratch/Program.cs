using Music.Utils;
using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create columns with proper alignment
        var columns = new List<ConsoleTableColumn>
        {
            new(ConsoleTableColumnAlignment.Right),  // ID
            new(ConsoleTableColumnAlignment.Left, ConsoleTableColumnSizing.Grow),   // Name
            new(ConsoleTableColumnAlignment.Center), // Category
            new(ConsoleTableColumnAlignment.Left)    // Status
        };

        // Create data with header row and content rows
        var data = new List<List<string>>
        {
            new() { "ID", "Name", "Category", "Status" },
            new() { "1", "Project Alpha", "Development", "Active" },
            new() { "2", "Project Beta", "Testing", "On Hold" },
            new() { "3", "Project Gamma", "Production", "Completed" },
            new() { "4", "Project Delta", "Planning", "Active" },
            new() { "5", "Project Epsilon", "Development", "Active" },
            new() { "6", "Project Zeta", "Testing", "On Hold" },
            new() { "7", "Project Eta", "Production", "Completed" },
            new() { "8", "Project Theta", "Planning", "Active" },
            new() { "9", "Project Iota", "Development", "Canceled" },
            new() { "10", "Project Kappa", "Testing", "Active" }
        };

        // Create the ConsoleTable with columns and data
        var table = new ConsoleTable(columns, data);
        
        // Render the table with appropriate width
        var tableWidth = Console.WindowWidth - 5;
        var renderedTable = table.Render(tableWidth);
        
        // Use the SelectList with the rendered table
        var selectList = new SelectList(renderedTable);
        
        // Show the selection UI
        var selectedIndex = selectList.Show("Select a project:");
        
        if (selectedIndex is null)
        {
            Console.WriteLine("Nothing selected");
            return;
        }
        
        // Display the selected line
        Console.WriteLine($"You selected line: {renderedTable[selectedIndex.Value]}");
        
        // If the selected index corresponds to a data row (skipping header row)
        if (selectedIndex.Value >= 1 && selectedIndex.Value - 1 < data.Count)
        {
            // Get the actual data row (offset by 1 for the header)
            var projectRow = data[selectedIndex.Value];
            Console.WriteLine($"\nProject details:");
            Console.WriteLine($"ID: {projectRow[0]}");
            Console.WriteLine($"Name: {projectRow[1]}");
            Console.WriteLine($"Category: {projectRow[2]}");
            Console.WriteLine($"Status: {projectRow[3]}");
        }
    }
}