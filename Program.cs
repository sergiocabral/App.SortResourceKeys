using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        foreach (var fileResx in new DirectoryInfo(Environment.CurrentDirectory).GetFiles())
        {
            if (!string.Equals(fileResx.Extension, ".resx", StringComparison.InvariantCultureIgnoreCase)) 
                continue;

            var fileCSharp = new FileInfo(Regex.Replace(fileResx.FullName, @".resx$", ".Designer.cs", RegexOptions.IgnoreCase));
            
            var fileResxContent = File.ReadAllLines(fileResx.FullName, Encoding.UTF8);
            var fileCSharpContent = fileCSharp.Exists ? File.ReadAllLines(fileCSharp.FullName, Encoding.UTF8) : new string[0];

            fileResxContent = SortResx(fileResxContent);
            fileCSharpContent = SortCSharp(fileCSharpContent);

            try
            {
                File.WriteAllLines(fileResx.FullName, fileResxContent, Encoding.UTF8);
                File.WriteAllLines(fileCSharp.FullName, fileCSharpContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private static string[] SortCSharp(string[] lines)
    {
        var header = new List<string>();
        var body = new List<string>();
        var footer = new List<string>();
        
        
        
        return Join(header, body, footer);
    }

    private static string[] SortResx(string[] lines)
    {
        var header = new List<string>();
        var body = new List<string>();
        var footer = new List<string>();
        
        return Join(header, body, footer);
    }

    private static string[] Join(List<string> header, List<string> body, List<string> footer)
    {
        header.AddRange(body);
        header.AddRange(footer);
        return header.ToArray();
    }
}