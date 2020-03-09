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

            fileCSharpContent = SortCSharp(fileCSharpContent);
            fileResxContent = SortResx(fileResxContent);

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

    private enum Mode
    {
        Header,
        Body,
        Footer
    }

    private static string[] SortCSharp(string[] lines)
    {
        var header = new List<string>();
        var body = new List<List<string>>(); 
        var footer = new List<string>();

        var mode = Mode.Header;
        var i = 0;

        foreach (var line in lines)
        {
            switch (mode)
            {
                case Mode.Header:
                    if (!Regex.IsMatch(line, @"public\s*static\s*string\s*[^ ]*\s*{"))
                    {
                        header.Add(line);
                    }
                    else
                    {
                        body.Add(new List<string>());
                        
                        while (header[header.Count - 1].Trim() != "}")
                        {
                            body[body.Count - 1].Insert(0, header[header.Count - 1]);
                            header.RemoveAt(header.Count - 1);
                        }

                        mode = Mode.Body;
                        goto case Mode.Body;
                    }
                    
                    break;
                case Mode.Body:
                    if (line.Trim() == "}") i++;
                    else i = 0;

                    if (i <= 2)
                    {
                        body[body.Count - 1].Add(line);
                    }
                    if (i == 2)
                    {
                        body.Add(new List<string>());
                    }
                    
                    if (i > 2)
                    {
                        body.RemoveAt(body.Count - 1);
                        mode = Mode.Footer;
                        goto case Mode.Footer;
                    }
                    
                    break;
                case Mode.Footer:
                    footer.Add(line);
                    break;
            }
        }

        return Join(header, body, footer);
    }

    private static string[] SortResx(string[] lines)
    {
        var header = new List<string>();
        var body = new List<List<string>>(); 
        var footer = new List<string>();

        var mode = Mode.Header;
        var i = 0;

        foreach (var line in lines)
        {
            switch (mode)
            {
                case Mode.Header:
                    if (!Regex.IsMatch(line, @"<data[^>]*name=""[^""]*""[^>]*>"))
                    {
                        header.Add(line);
                    }
                    else
                    {
                        body.Add(new List<string>());
                        
                        while (!Regex.IsMatch(header[header.Count - 1], @"<\s*/\s*[^> ]+\s*>"))
                        {
                            body[body.Count - 1].Insert(0, header[header.Count - 1]);
                            header.RemoveAt(header.Count - 1);
                        }

                        mode = Mode.Body;
                        goto case Mode.Body;
                    }
                    
                    break;
                case Mode.Body:
                    if (Regex.IsMatch(line, @"<\s*/\s*(data|root)\s*>")) i++;
                    else i = 0;

                    if (i <= 1)
                    {
                        body[body.Count - 1].Add(line);
                    }
                    if (i == 1)
                    {
                        body.Add(new List<string>());
                    }
                    
                    if (i > 1)
                    {
                        body.RemoveAt(body.Count - 1);
                        mode = Mode.Footer;
                        goto case Mode.Footer;
                    }
                    
                    break;
                case Mode.Footer:
                    footer.Add(line);
                    break;
            }
        }

        return Join(header, body, footer);
    }

    private static string[] Join(List<string> header, List<List<string>> body, List<string> footer)
    {
        body.Sort((list1, list2) =>
        {
            const string regex = @"((?<=public\s*static\s*string\s*)[^ ]*(?=\s*{)|(?<=name="")[^""]*)";
            
            var text1 = string.Join(Environment.NewLine, list1.ToArray());
            var key1 = Regex.Match(text1, regex, RegexOptions.Singleline).Value;
            
            var text2 = string.Join(Environment.NewLine, list2.ToArray());
            var key2 = Regex.Match(text2, regex, RegexOptions.Singleline).Value;

            return string.Compare(key1, key2, StringComparison.Ordinal);
        });
        foreach (var block in body) header.AddRange(block);
        header.AddRange(footer);
        return header.ToArray();
    }
}